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
    public sealed class RunGameFlowController
    {
        private const float ReviveWindowSeconds = 5f;
        private const string TutorialEnemyAttackPrompt =
            "방금 몬스터 공격을 맞았습니다. 플레이어 HP가 줄어드는지 확인하세요. 방어와 회복은 이런 적 턴을 버티기 위한 선택입니다.";
        private const string TutorialIntentPrompt =
            "몬스터 머리 위 아이콘은 다음 행동입니다.\n왼쪽 몬스터는 공격하고, 오른쪽 몬스터는 방어합니다.\n먼저 인벤토리에서 훈련용 배터리 효과를 확인하세요.";
        private const string TutorialInventoryOpenedPrompt =
            "훈련용 배터리는 체리/레몬 족보에 반응합니다.\n체리 3개 이상: 추가 피해 +3\n레몬 3개 이상: 보호막 +4";
        private const string TutorialFirstSpinPrompt =
            "인벤토리를 닫았습니다. 이제 SPIN으로 체리와 레몬 족보가 어떻게 발동하는지 확인하세요.";
        private const string TutorialFirstResultPrompt =
            "체리 족보가 피해를 만들고, 훈련용 배터리가 추가 피해 +3을 더했습니다.\n레몬 족보에는 보호막 +4로 반응했습니다.";
        private const string TutorialUpdatedEnemyTurnPrompt =
            "몬스터는 예고한 행동을 실행합니다.\n왼쪽 몬스터는 쓰러져 행동하지 않고, 오른쪽 몬스터는 방어 후 다음 행동이 공격으로 바뀌었습니다.\n강한 체리 족보로 먼저 처치하세요.";
        private const string TutorialUpdatedCompletedPrompt =
            "튜토리얼 완료! 로비로 돌아갑니다.";

        private readonly RunGameNavigator _navigator;
        private readonly StartRelicSelectViewModel _startRelicSelectVM;
        private readonly RunRewardViewModel _rewardVM;
        private readonly RunHUDViewModel _hudVM;
        private readonly RunInventoryViewModel _inventoryVM;
        private readonly RunDefeatViewModel _defeatVM;
        private readonly LeaderboardViewModel _leaderboardVM;
        private readonly BattleView _battleView;
        private readonly BattleSceneCompositionRoot _battleSceneCompositionRoot;
        private readonly RunTutorialOverlayView _tutorialOverlayView;
        private readonly RunDefeatView _defeatView;
        private readonly LeaderboardView _leaderboardView;
        private readonly CancellationToken _ownerDestroyToken;

        private CancellationTokenSource _defeatCountdownCts;
        private bool _reviveAdPending;
        private bool _resumeBattleOnEnter;
        private bool _tutorialPatternMessageShown;
        private bool _tutorialEnemyAttackMessageShown;
        private bool _tutorialEnemyTurnMessageShown;
        private bool _tutorialInventoryOpened;
        private TutorialRunStep _tutorialRunStep;

        private enum TutorialRunStep
        {
            None = 0,
            InspectIntent = 1,
            InspectInventory = 2,
            FirstSpin = 3,
            EnemyTurn = 4,
            SecondSpin = 5,
            Completed = 6,
        }

        public RunGameFlowController(
            RunGameNavigator navigator,
            StartRelicSelectViewModel startRelicSelectVM,
            RunRewardViewModel rewardVM,
            RunHUDViewModel hudVM,
            RunInventoryViewModel inventoryVM,
            RunDefeatViewModel defeatVM,
            LeaderboardViewModel leaderboardVM,
            BattleView battleView,
            BattleSceneCompositionRoot battleSceneCompositionRoot,
            RunTutorialOverlayView tutorialOverlayView,
            RunDefeatView defeatView,
            LeaderboardView leaderboardView,
            CancellationToken ownerDestroyToken)
        {
            _navigator = navigator;
            _startRelicSelectVM = startRelicSelectVM;
            _rewardVM = rewardVM;
            _hudVM = hudVM;
            _inventoryVM = inventoryVM;
            _defeatVM = defeatVM;
            _leaderboardVM = leaderboardVM;
            _battleView = battleView;
            _battleSceneCompositionRoot = battleSceneCompositionRoot;
            _tutorialOverlayView = tutorialOverlayView;
            _defeatView = defeatView;
            _leaderboardView = leaderboardView;
            _ownerDestroyToken = ownerDestroyToken;
        }

        public static RunGameState GetInitialRunState()
        {
            return GameFlowSession.IsTutorialRun
                ? RunGameState.Battle
                : RunGameState.StartRelicSelect;
        }

        public void Dispose()
        {
            CancelDefeatCountdown();
        }

        // ── 시작 유물 ────────────────────────────────────────────────────

        public void HandleStartRelicEntered()
        {
            _startRelicSelectVM.Refresh();
        }

        public void HandleStarterRelicSelectionRequested(string relicId)
        {
            _startRelicSelectVM.SelectRelic(relicId);
        }

        public void HandleStarterRelicSelected()
        {
            _tutorialOverlayView?.Hide();
            RefreshHud();
            _navigator.GoTo(RunGameState.Battle);
        }

        // ── 보상 ─────────────────────────────────────────────────────────

        public void HandleRewardEntered()
        {
            _rewardVM.Refresh();
            RefreshRewardedAvailability();
        }

        public void HandleRewardSelectionRequested(int optionIndex)
        {
            _rewardVM.ClaimReward(optionIndex);
        }

        public void HandleRewardRerollRequested()
        {
            RunRewardedOrSkip(
                RewardedAdPurpose.RewardReroll,
                HandleRewardRerollRewarded);
        }

        private void HandleRewardRerollRewarded()
        {
            Debug.Log("[RunGameFlowController] Applying rewarded reroll.");
            _rewardVM.ApplyRewardedReroll();
        }

        public void HandleExtraRewardRequested()
        {
            RunRewardedOrSkip(
                RewardedAdPurpose.ExtraReward,
                _rewardVM.ApplyRewardedExtraReward);
        }

        public void HandleRewardDoubleRequested()
        {
            RunRewardedOrSkip(
                RewardedAdPurpose.RewardDouble,
                _rewardVM.ApplyRewardedDouble);
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
            GameFlowSession.AdvanceToNextBattle();
            RefreshHud();
            _inventoryVM?.Refresh();
            StartNextBattle();
        }

        // ── 인벤토리 ─────────────────────────────────────────────────────

        public void HandleInventoryOpenRequested()
        {
            _inventoryVM.Open();

            if (!GameFlowSession.IsTutorialRun)
            {
                return;
            }

            _tutorialInventoryOpened = true;
            _tutorialRunStep = TutorialRunStep.InspectInventory;
            _inventoryVM.SelectTab(RunInventoryTab.Relics);
            _tutorialOverlayView?.ShowMessage(TutorialInventoryOpenedPrompt);
        }

        public void HandleInventoryCloseRequested()
        {
            _inventoryVM.Close();

            if (!GameFlowSession.IsTutorialRun ||
                !_tutorialInventoryOpened ||
                _tutorialRunStep != TutorialRunStep.InspectInventory)
            {
                return;
            }

            _tutorialRunStep = TutorialRunStep.FirstSpin;
            _battleSceneCompositionRoot?.SetTutorialSpinBlocked(false);
            _tutorialOverlayView?.ShowMessage(TutorialFirstSpinPrompt);
        }

        public void HandleInventorySymbolTabRequested()
        {
            _inventoryVM.SelectTab(RunInventoryTab.SymbolPool);
        }

        public void HandleInventoryRelicTabRequested()
        {
            _inventoryVM.SelectTab(RunInventoryTab.Relics);
        }

        // ── 전투 ─────────────────────────────────────────────────────────

        public void HandleBattleEntered()
        {
            if (_resumeBattleOnEnter)
            {
                _resumeBattleOnEnter = false;
                return;
            }

            if (_battleSceneCompositionRoot != null)
            {
                _battleSceneCompositionRoot.BeginBattle();
            }
        }

        public void HandleBattleTutorialSignalRaised(BattleTutorialSignal signal)
        {
            if (!GameFlowSession.IsTutorialRun || _tutorialOverlayView == null)
            {
                return;
            }

            switch (signal)
            {
                case BattleTutorialSignal.BattleStarted:
                    _tutorialRunStep = TutorialRunStep.InspectIntent;
                    _tutorialInventoryOpened = false;
                    _battleSceneCompositionRoot?.SetTutorialSpinBlocked(true);
                    _battleSceneCompositionRoot?.SetTutorialTargetSelectionBlocked(true);
                    _tutorialOverlayView.ShowMessage(TutorialIntentPrompt);
                    break;
                case BattleTutorialSignal.SlotPresentationCompleted:
                    if (!_tutorialPatternMessageShown)
                    {
                        _tutorialPatternMessageShown = true;
                        _tutorialRunStep = TutorialRunStep.EnemyTurn;
                        _tutorialOverlayView.ShowMessage(TutorialFirstResultPrompt);
                    }
                    break;
                case BattleTutorialSignal.EnemyTurnCompleted:
                    if (!_tutorialEnemyTurnMessageShown)
                    {
                        _tutorialEnemyTurnMessageShown = true;
                        _tutorialRunStep = TutorialRunStep.SecondSpin;
                        _battleSceneCompositionRoot?.SetTutorialTargetSelectionBlocked(false);
                        _tutorialOverlayView.ShowMessage(TutorialUpdatedEnemyTurnPrompt);
                    }
                    break;
                case BattleTutorialSignal.EnemyAttackReceived:
                    if (!_tutorialEnemyAttackMessageShown)
                    {
                        _tutorialEnemyAttackMessageShown = true;
                        _tutorialOverlayView.ShowMessage(TutorialEnemyAttackPrompt);
                    }
                    break;
                default:
                    break;
            }
        }

        // ── 전투 결과 처리 ───────────────────────────────────────────────

        public void OnBattleVictory()
        {
            if (GameFlowSession.IsTutorialRun)
            {
                _inventoryVM?.Close();
                CompleteFirstRunTutorial();
                ReturnToLobbyAfterTutorialAsync().Forget();
                return;
            }

            // 매 wave 승리 시 보상 화면(3택)으로 이동.
            _inventoryVM?.Close();
            RefreshHud();
            _navigator.GoTo(RunGameState.Reward);
        }

        private async UniTaskVoid ReturnToLobbyAfterTutorialAsync()
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(1.2f),
                    cancellationToken: _ownerDestroyToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            GameFlowSession.EndRun();
            GameSceneLoader.LoadGameStart();
        }

        public void OnBattleDefeat()
        {
            _inventoryVM?.Close();
            CancelDefeatCountdown();
            _reviveAdPending = false;
            _defeatView?.SetMonsterPortrait(
                _battleSceneCompositionRoot?.GetDefeatingMonsterPortrait());

            if (GameFlowSession.CanRevive)
            {
                _defeatVM.ShowReviveOffer(
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
            _inventoryVM?.Close();
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
            if (_leaderboardView == null)
            {
                GameStartSceneRoot.RequestOpenLeaderboardOnNextLoad();
                GameSceneLoader.LoadGameStart();
                return;
            }

            _leaderboardVM.OpenAsync().Forget();
        }

        public void HandleHomeRequested()
        {
            FinalizePendingDefeat();
            GameSceneLoader.LoadGameStart();
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
            _defeatVM.SetRevivePending();
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

            _defeatVM.SetCanRevive(false);
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
            // TODO: UI_PopupCanvas의 PausePopup 활성화
            Debug.Log("[RunGameFlowController] Pause requested.");
        }

        public void RefreshHud()
        {
            _hudVM.Refresh();
        }

        private void CompleteFirstRunTutorial()
        {
            FirstRunTutorialState.MarkCompleted();
            ResetTutorialMessages();
            _tutorialRunStep = TutorialRunStep.Completed;
            _tutorialOverlayView?.ShowMessage(TutorialUpdatedCompletedPrompt);
        }

        private void ResetTutorialMessages()
        {
            _tutorialPatternMessageShown = false;
            _tutorialEnemyAttackMessageShown = false;
            _tutorialEnemyTurnMessageShown = false;
            _tutorialInventoryOpened = false;
            _tutorialRunStep = TutorialRunStep.None;
            _battleSceneCompositionRoot?.SetTutorialSpinBlocked(false);
            _battleSceneCompositionRoot?.SetTutorialTargetSelectionBlocked(false);
        }

        public void RefreshRewardedAvailability()
        {
            AdsManager adsManager = AdsManager.Instance;
            bool adsRemoved = AdsRemoveState.IsRemoved;

            _rewardVM?.SetRewardedAvailability(
                CanShowRewarded(adsManager, RewardedAdPurpose.RewardReroll),
                CanShowRewarded(adsManager, RewardedAdPurpose.ExtraReward),
                CanShowRewarded(adsManager, RewardedAdPurpose.RewardDouble),
                adsRemoved);
            _defeatVM?.SetRewardedAvailability(
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
                        _defeatVM.UpdateReviveCountdown(displayedSeconds);
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
            _defeatVM.ShowResult(
                GameFlowSession.CurrentBattleNumber,
                GameFlowSession.Victories,
                GameFlowSession.RewardsClaimed,
                GameFlowSession.HasRevivedThisRun,
                GameFlowSession.GetSlotSymbolContributionSummary());
            RefreshRewardedAvailability();
        }

        private void CancelDefeatCountdown()
        {
            _defeatCountdownCts?.Cancel();
            _defeatCountdownCts?.Dispose();
            _defeatCountdownCts = null;
        }
    }
}
