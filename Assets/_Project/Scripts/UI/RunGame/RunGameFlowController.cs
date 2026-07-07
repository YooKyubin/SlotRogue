using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.UI.Ads;
using SlotRogue.UI.App;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.Iap;
using SlotRogue.UI.Leaderboard;
using SlotRogue.UI.RunGame.ViewModels;
using UnityEngine;

namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// RunGame의 화면 흐름 제어를 담당합니다.
    /// 전투 진입/결과, 보상 선택, 광고 리롤, 패배/부활, 튜토리얼, 홈/랭킹 이동 같은
    /// '언제·어디로 전환하는가'를 결정하고 ViewModel/Session/System을 호출합니다.
    ///
    /// 순수 C# 클래스입니다. ViewModel/View 참조는 RunGameSceneRoot가 생성·주입하며,
    /// 이 컨트롤러는 Unity 생명주기(Awake/OnDestroy) 대신 SceneRoot의 destroy 토큰으로
    /// 비동기 작업 수명을 관리합니다.
    /// </summary>
    public sealed class RunGameFlowController : IRunGameFlow
    {
        private const float ReviveWindowSeconds = 5f;

        private readonly IRunGameNavigator _navigator;
        private readonly RunGamePresenter _presenter;
        private readonly IRunGameView _battleView;
        private readonly IBattleSceneController _battleSceneCompositionRoot;
        private readonly ITutorialOverlay _tutorialOverlayView;
        private readonly RunBattleTutorialSequenceDefinition _tutorialSequenceDefinition;
        private readonly IDefeatPortraitView _defeatView;
        private readonly bool _hasInSceneLeaderboard;
        private readonly CancellationToken _ownerDestroyToken;

        private CancellationTokenSource _defeatCountdownCts;
        private bool _reviveAdPending;
        private bool _resumeBattleOnEnter;
        private bool _battleEntryPending;
        private int _tutorialStepIndex;
        private bool _preBattleTutorialCompleted;
        private bool _tutorialBattlePrepared;

        public RunGameFlowController(
            IRunGameNavigator navigator,
            RunGamePresenter presenter,
            IRunGameView battleView,
            IBattleSceneController battleSceneCompositionRoot,
            ITutorialOverlay tutorialOverlayView,
            RunBattleTutorialSequenceDefinition tutorialSequenceDefinition,
            IDefeatPortraitView defeatView,
            bool hasInSceneLeaderboard,
            CancellationToken ownerDestroyToken)
        {
            _navigator = navigator;
            _presenter = presenter;
            _battleView = battleView;
            _battleSceneCompositionRoot = battleSceneCompositionRoot;
            _tutorialOverlayView = tutorialOverlayView;
            _tutorialSequenceDefinition = tutorialSequenceDefinition;
            _defeatView = defeatView;
            _hasInSceneLeaderboard = hasInSceneLeaderboard;
            _ownerDestroyToken = ownerDestroyToken;
        }

        public static RunGameState GetInitialRunState()
        {
            return RunGameState.Battle;
        }

        public void Dispose()
        {
            CancelDefeatCountdown();
        }

        // ── 보상 ─────────────────────────────────────────────────────────

        public void HandleRewardEntered()
        {
            _presenter.RefreshReward();
            RefreshRewardedAvailability();
        }

        public void HandleRewardSelectionRequested(int optionIndex)
        {
            _presenter.ClaimReward(optionIndex);
        }

        public void HandleRewardRerollRequested()
        {
            RunRewardedOrSkip(
                RewardedAdPurpose.RewardReroll,
                HandleRewardRerollRewarded);
        }

        private void HandleRewardRerollRewarded()
        {
            GameLog.Info("[RunGameFlowController] Applying rewarded reroll.");
            _presenter.ApplyRewardedReroll();
        }

        public void HandleExtraRewardRequested()
        {
            RunRewardedOrSkip(
                RewardedAdPurpose.ExtraReward,
                _presenter.ApplyRewardedExtraReward);
        }

        public void HandleRewardDoubleRequested()
        {
            RunRewardedOrSkip(
                RewardedAdPurpose.RewardDouble,
                _presenter.ApplyRewardedDouble);
        }

        private void RunRewardedOrSkip(
            RewardedAdPurpose purpose,
            Action rewardCallback)
        {
            if (AdsRemoveState.IsRemoved)
            {
                rewardCallback?.Invoke();
                return;
            }

            AdsManager adsManager = AdsManager.Instance;
            if (adsManager == null ||
                !adsManager.CanShowRewarded(purpose))
            {
                Debug.LogWarning(
                    $"[RunGameFlowController] Rewarded is not ready for {purpose}.");
                RefreshRewardedAvailability();
                return;
            }

            adsManager.ShowRewarded(purpose, rewardCallback);
        }

        public void HandleRewardClaimed()
        {
            // 레거시 시작 유물 선택 화면은 전투 번호를 올리지 않고 첫 전투로 진입합니다.
            if (_presenter.IsStarterRewardSelection)
            {
                _presenter.RefreshInventory();
                EnterBattleAfterStarterRelicAsync().Forget();
                return;
            }

            GameFlowSession.AdvanceToNextBattle();
            RefreshHud();
            _presenter.RefreshInventory();
            StartNextBattle();
        }

        // ── 인벤토리 ─────────────────────────────────────────────────────

        public void HandleInventoryOpenRequested()
        {
            _presenter.OpenRelicInventory();
        }

        public void HandleInventoryCloseRequested()
        {
            _presenter.CloseRelicInventory();
        }

        public void HandleDescriptionOpenRequested()
        {
            _presenter.OpenDescription();
        }

        public void HandleDescriptionCloseRequested()
        {
            _presenter.CloseDescription();
        }

        public void HandleInventorySymbolTabRequested()
        {
            _presenter.SelectInventoryTab(RunInventoryTab.SymbolProbability);
        }

        public void HandleInventoryPatternTabRequested()
        {
            _presenter.SelectInventoryTab(RunInventoryTab.PatternDescription);
        }

        // ── 전투 ─────────────────────────────────────────────────────────

        public void HandleBattleEntered()
        {
            if (_resumeBattleOnEnter)
            {
                _resumeBattleOnEnter = false;
                return;
            }

            // 최초 튜토리얼 런은 전투를 시작하기 전에 설명형 나레이션을 먼저 재생한다.
            // 나레이션을 모두 본 뒤 일반 런으로 전환하고 실제 전투를 시작한다.
            if (TryBeginTutorialNarration())
            {
                return;
            }

            if (_battleSceneCompositionRoot != null)
            {
                _battleSceneCompositionRoot.BeginBattle();
            }
        }

        // 시그널 기반 스포트라이트 튜토리얼은 설명형 나레이션으로 대체되었다.
        // 전투 시그널 배선은 유지하되 여기서는 아무 것도 하지 않는다.
        public void HandleBattleTutorialSignalRaised(BattleTutorialSignal signal)
        {
        }

        // ── 설명형 나레이션 ──────────────────────────────────────────────

        private bool TryBeginTutorialNarration()
        {
            if (!GameFlowSession.IsTutorialRun || _preBattleTutorialCompleted)
            {
                return false;
            }

            if (_tutorialOverlayView == null ||
                _tutorialSequenceDefinition == null ||
                _tutorialSequenceDefinition.StepCount == 0)
            {
                // 안내할 문구가 없으면 튜토리얼을 즉시 마치고 일반 전투로 진행한다.
                FinishTutorialNarration();
                return true;
            }

            PrepareBattleBehindTutorial();
            _tutorialStepIndex = 0;
            ShowCurrentTutorialStep();
            return true;
        }

        private void ShowCurrentTutorialStep()
        {
            if (_tutorialSequenceDefinition == null ||
                !_tutorialSequenceDefinition.TryGetStep(
                    _tutorialStepIndex,
                    out RunBattleTutorialStep step))
            {
                FinishTutorialNarration();
                return;
            }

            _tutorialOverlayView.ShowStep(
                ResolveTutorialTarget(step.TargetKey),
                step,
                AdvanceTutorialNarration);
        }

        private void AdvanceTutorialNarration()
        {
            _tutorialStepIndex++;
            ShowCurrentTutorialStep();
        }

        private void FinishTutorialNarration()
        {
            _preBattleTutorialCompleted = true;
            FirstRunTutorialState.MarkCompleted();
            GameFlowSession.CompleteTutorialAndContinueAsNormalRun();
            _tutorialOverlayView?.Hide();
            RefreshHud();
            if (_tutorialBattlePrepared)
            {
                _battleSceneCompositionRoot?.SetTutorialSpinBlocked(false);
                _battleSceneCompositionRoot?.SetTutorialTargetSelectionBlocked(false);
                _tutorialBattlePrepared = false;
                return;
            }

            _battleSceneCompositionRoot?.BeginBattle();
        }

        // ── 전투 결과 처리 ───────────────────────────────────────────────

        private void PrepareBattleBehindTutorial()
        {
            if (_battleSceneCompositionRoot == null || _tutorialBattlePrepared)
            {
                return;
            }

            _battleSceneCompositionRoot.SetTutorialSpinBlocked(true);
            _battleSceneCompositionRoot.SetTutorialTargetSelectionBlocked(true);
            _battleSceneCompositionRoot.BeginBattle();
            _tutorialBattlePrepared = true;
        }

        private RectTransform ResolveTutorialTarget(RunBattleTutorialTargetKey targetKey)
        {
            RunBattleTutorialTargets targets =
                _battleSceneCompositionRoot?.TutorialTargets ?? RunBattleTutorialTargets.Empty;
            return targetKey switch
            {
                RunBattleTutorialTargetKey.Spin => targets.SpinTarget,
                RunBattleTutorialTargetKey.SwapDecision => targets.SwapDecisionTarget,
                RunBattleTutorialTargetKey.Shop => targets.ShopTarget,
                RunBattleTutorialTargetKey.Enemy => targets.EnemyTarget,
                _ => null,
            };
        }

        public void OnBattleVictory()
        {
            if (GameFlowSession.IsTutorialRun)
            {
                CompleteFirstRunTutorial();
                GameFlowSession.CompleteTutorialAndContinueAsNormalRun();
                _tutorialOverlayView?.Hide();
            }

            // 매 wave 승리 시 보상 화면(3택)으로 이동.
            _presenter.CloseInventory();
            RefreshHud();
            _navigator.GoTo(RunGameState.Reward);
        }

        public void OnBattleDefeat()
        {
            _presenter.CloseInventory();
            CancelDefeatCountdown();
            _reviveAdPending = false;
            _defeatView?.SetMonsterPortrait(
                _battleSceneCompositionRoot?.GetDefeatingMonsterPortrait());

            if (GameFlowSession.CanRevive)
            {
                _presenter.ShowReviveOffer(
                    GameFlowSession.CurrentBattleNumber,
                    GameFlowSession.Victories,
                    GameFlowSession.RewardsClaimed,
                    Mathf.CeilToInt(ReviveWindowSeconds));
            }
            else
            {
                ShowFinalDefeatResult();
            }

            RefreshHud();
            _navigator.GoTo(RunGameState.Defeat);

            if (GameFlowSession.CanRevive)
            {
                RefreshRewardedAvailability();
                StartDefeatCountdown();
            }
        }

        public void HandleRestartRequested()
        {
            _presenter.CloseInventory();
            bool restartTutorial =
                GameFlowSession.IsTutorialRun &&
                !FirstRunTutorialState.IsCompleted;
            FinalizePendingDefeat();
            if (restartTutorial)
            {
                GameFlowSession.StartTutorialRun();
                ResetTutorialMessages();
            }
            else
            {
                GameFlowSession.StartNewRun();
                _tutorialOverlayView?.Hide();
            }

            RefreshHud();
            _navigator.GoTo(GetInitialRunState());
        }

        public void HandleRankingRequested()
        {
            FinalizePendingDefeat();
            if (!_hasInSceneLeaderboard)
            {
                GameStartSceneRoot.RequestOpenLeaderboardOnNextLoad();
                GameSceneLoader.LoadGameStart();
                return;
            }

            _presenter.OpenLeaderboardAsync().Forget();
        }

        public void HandleHomeRequested()
        {
            FinalizePendingDefeat();
            GameSceneLoader.LoadGameStart();
        }

        public void HandleGiveUpRequested()
        {
            _presenter.CloseInventory();
            _tutorialOverlayView?.Hide();
            ShowAbandonedRunResult();
        }

        public void HandleReviveRequested()
        {
            if (!GameFlowSession.CanRevive)
            {
                return;
            }

            if (AdsRemoveState.IsRemoved)
            {
                HandleReviveRewarded();
                return;
            }

            AdsManager adsManager = AdsManager.Instance;
            if (adsManager == null ||
                !adsManager.CanShowRewarded(RewardedAdPurpose.Revive))
            {
                Debug.LogWarning("[RunGameFlowController] Rewarded revive is not ready.");
                RefreshRewardedAvailability();
                return;
            }

            CancelDefeatCountdown();
            _reviveAdPending = true;
            _presenter.SetRevivePending();
            adsManager.ShowRewarded(
                RewardedAdPurpose.Revive,
                HandleReviveRewarded);
        }

        private void HandleReviveRewarded()
        {
            _reviveAdPending = false;
            CancelDefeatCountdown();

            if (_navigator.CurrentState != RunGameState.Defeat ||
                _battleSceneCompositionRoot == null ||
                !_battleSceneCompositionRoot.TryRevive())
            {
                ShowFinalDefeatResult();
                return;
            }

            _presenter.SetCanRevive(false);
            RefreshHud();
            _resumeBattleOnEnter = true;
            _navigator.GoTo(RunGameState.Battle);
        }

        public void HandleRewardedSessionEnded(
            RewardedAdPurpose purpose,
            bool rewarded)
        {
            if (purpose != RewardedAdPurpose.Revive ||
                rewarded ||
                !_reviveAdPending)
            {
                return;
            }

            _reviveAdPending = false;
            ShowFinalDefeatResult();
        }

        public void HandleAdsRemoveChanged(bool isRemoved)
        {
            RefreshRewardedAvailability();
        }

        // 같은 Battle 상태에 머무를 땐 Navigator.GoTo가 no-op이므로 View를 직접 재진입시킨다.
        private void StartNextBattle()
        {
            CancelDefeatCountdown();
            if (_navigator.CurrentState == RunGameState.Battle && _battleView != null)
            {
                _battleView.OnEnter();
            }
            else
            {
                _navigator.GoTo(RunGameState.Battle);
            }
        }

        // ── 기타 ────────────────────────────────────────────────────────

        public void OnPauseRequested()
        {
            GameLog.Info("[RunGameFlowController] Pause requested.");
        }

        public void RefreshHud()
        {
            _presenter.RefreshHud();
        }

        private async UniTaskVoid EnterBattleAfterStarterRelicAsync()
        {
            if (_battleEntryPending)
            {
                return;
            }

            _battleEntryPending = true;
            try
            {
                _tutorialOverlayView?.Hide();
                RefreshHud();

                if (_battleSceneCompositionRoot != null)
                {
                    await _battleSceneCompositionRoot.PrepareBattleEntryAsync(_ownerDestroyToken);
                }

                if (_ownerDestroyToken.IsCancellationRequested)
                {
                    return;
                }

                _navigator.GoTo(RunGameState.Battle);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                _battleEntryPending = false;
            }
        }

        private void CompleteFirstRunTutorial()
        {
            FirstRunTutorialState.MarkCompleted();
            ResetTutorialMessages();
            if (!string.IsNullOrWhiteSpace(_tutorialSequenceDefinition?.CompletionMessage))
            {
                _tutorialOverlayView?.ShowMessage(_tutorialSequenceDefinition.CompletionMessage);
            }
        }

        private void ResetTutorialMessages()
        {
            _tutorialStepIndex = 0;
            _preBattleTutorialCompleted = false;
            _tutorialBattlePrepared = false;
        }

        public void RefreshRewardedAvailability()
        {
            AdsManager adsManager = AdsManager.Instance;
            bool adsRemoved = AdsRemoveState.IsRemoved;

            _presenter.SetRewardedAvailability(
                CanShowRewarded(adsManager, RewardedAdPurpose.RewardReroll),
                CanShowRewarded(adsManager, RewardedAdPurpose.ExtraReward),
                CanShowRewarded(adsManager, RewardedAdPurpose.RewardDouble),
                CanShowRewarded(adsManager, RewardedAdPurpose.Revive),
                adsRemoved);
        }

        private static bool CanShowRewarded(
            AdsManager adsManager,
            RewardedAdPurpose purpose)
        {
            return adsManager != null && adsManager.CanShowRewarded(purpose);
        }

        private void FinalizePendingDefeat()
        {
            CancelDefeatCountdown();
            _reviveAdPending = false;
            _battleSceneCompositionRoot?.FinalizePendingDefeat();
            RefreshRewardedAvailability();
        }

        private void StartDefeatCountdown()
        {
            CancelDefeatCountdown();
            _defeatCountdownCts =
                CancellationTokenSource.CreateLinkedTokenSource(_ownerDestroyToken);
            RunDefeatCountdownAsync(_defeatCountdownCts.Token).Forget();
        }

        private async UniTaskVoid RunDefeatCountdownAsync(
            CancellationToken cancellationToken)
        {
            float remainingSeconds = ReviveWindowSeconds;
            int displayedSeconds = Mathf.CeilToInt(remainingSeconds);

            try
            {
                while (remainingSeconds > 0f)
                {
                    await UniTask.Yield(
                        PlayerLoopTiming.Update,
                        cancellationToken);
                    remainingSeconds -= Time.unscaledDeltaTime;
                    int nextDisplayedSeconds =
                        Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
                    if (nextDisplayedSeconds != displayedSeconds)
                    {
                        displayedSeconds = nextDisplayedSeconds;
                        _presenter.UpdateReviveCountdown(displayedSeconds);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                ShowFinalDefeatResult();
            }
        }

        private void ShowFinalDefeatResult()
        {
            CancelDefeatCountdown();
            _reviveAdPending = false;
            _battleSceneCompositionRoot?.FinalizePendingDefeat();
            _presenter.ShowDefeatResult(
                GameFlowSession.CurrentBattleNumber,
                GameFlowSession.Victories,
                GameFlowSession.RewardsClaimed,
                GameFlowSession.HasRevivedThisRun,
                GameFlowSession.GetSlotSymbolContributionSummary());
            RefreshRewardedAvailability();
        }

        private void ShowAbandonedRunResult()
        {
            CancelDefeatCountdown();
            _reviveAdPending = false;

            int battleNumber = GameFlowSession.CurrentBattleNumber;
            int victories = GameFlowSession.Victories;
            int rewardsClaimed = GameFlowSession.RewardsClaimed;
            bool hasRevived = GameFlowSession.HasRevivedThisRun;
            var symbolContributions =
                GameFlowSession.GetSlotSymbolContributionSummary();

            if (GameFlowSession.IsDefeatPending)
            {
                _battleSceneCompositionRoot?.FinalizePendingDefeat();
            }
            else if (GameFlowSession.HasRun)
            {
                GameFlowSession.EndRun();
            }

            _presenter.ShowDefeatResult(
                battleNumber,
                victories,
                rewardsClaimed,
                hasRevived,
                symbolContributions);
            RefreshRewardedAvailability();
            _navigator.GoTo(RunGameState.Defeat);
        }

        private void CancelDefeatCountdown()
        {
            _defeatCountdownCts?.Cancel();
            _defeatCountdownCts?.Dispose();
            _defeatCountdownCts = null;
        }
    }
}
