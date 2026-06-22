using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Tooling;
using SlotRogue.UI.Ads;
using SlotRogue.UI.App;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.Iap;
using SlotRogue.UI.Leaderboard;
using SlotRogue.UI.RunGame.ViewModels;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// RunGameScene의 최상위 조립자입니다.
    /// ViewModel 생성 → View Bind → Navigator 등록 → 첫 화면 진입을 담당합니다.
    ///
    /// 규칙:
    ///  - 게임 규칙·보상 계산은 GameFlowSession 또는 각 ViewModel에 위임합니다.
    ///  - 화면 전환은 RunGameNavigator.GoTo() 만 사용합니다.
    ///  - 이 클래스는 '언제, 어디로 전환하는가'만 결정합니다.
    /// </summary>
    public class RunGameSceneRoot : MonoBehaviour
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

        public static RunGameSceneRoot Instance { get; private set; }

        // ── Inspector 연결 ───────────────────────────────────────────────

        [Header("Navigator")]
        [SerializeField, AutoWire("RunGameNavigator")]
        private RunGameNavigator _navigator;

        [Header("Game Views  (IRunGameView 구현체)")]
        [SerializeField, AutoWire("00_StartRelicSelectView")]
        private StartArtifactSelectionView _startRelicSelectView;
        [SerializeField, AutoWire("10_BattleView")]
        private BattleView _battleView;
        [SerializeField, AutoWire("20_RewardView")]
        private RunRewardView _rewardView;
        [SerializeField] private RunDefeatView _defeatView;
        [SerializeField] private LeaderboardView _leaderboardView;

        [Header("HUD  (항상 표시)")]
        [SerializeField] private RunHUDView _hudView;
        [SerializeField] private RunInventoryView _inventoryView;
        [SerializeField] private RunTutorialOverlayView _tutorialOverlayView;

        [Header("Battle Flow")]
        [FormerlySerializedAs("_battleFlowController")]
        [SerializeField, AutoWire("BattleSceneCompositionRoot")]
        private BattleSceneCompositionRoot _battleSceneCompositionRoot;

        private StartRelicSelectViewModel _startRelicSelectVM;
        private RunRewardViewModel        _rewardVM;
        private RunHUDViewModel           _hudVM;
        private RunInventoryViewModel     _inventoryVM;
        private RunDefeatViewModel        _defeatVM;
        private LeaderboardViewModel       _leaderboardVM;
        private RelicIconRenderer          _relicIconRenderer;
        private CancellationTokenSource     _defeatCountdownCts;
        private bool                        _reviveAdPending;
        private bool                        _resumeBattleOnEnter;
        private bool                        _tutorialPatternMessageShown;
        private bool                        _tutorialEnemyAttackMessageShown;
        private bool                        _tutorialEnemyTurnMessageShown;
        private bool                        _tutorialInventoryOpened;
        private TutorialRunStep             _tutorialRunStep;

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

        // ── 초기화 ──────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _relicIconRenderer = new RelicIconRenderer();
            AdsRemoveState.Initialize();

            // 런 보장은 ViewModel 생성·View 바인딩·Navigator.GoTo 이전에 실행되어야 합니다.
            // BootScene → GameStart → RunGame 경로에서는 GameStart에서 이미 새 런을 시작했으므로
            // 여기서는 그대로 이어가고, RunGame 단독 Play 경로에서는 여기서 새 런을 시작합니다.
            EnsureRunStarted();

            CreateViewModels();
            EnsureDefeatView();
            EnsureLeaderboardView();
            EnsureInventoryView();
            EnsureTutorialOverlayView();
            BindViews();
            RegisterViews();
            SubscribeEvents();
            RefreshRewardedAvailability();
            RefreshHud();
        }

        // RunGame 단독 실행 방어: 진행 중인 런이 없으면 새 런을 시작합니다.
        // 진행 중인 런이 있으면(=GameStart에서 시작) 덮어쓰지 않습니다.
        private static void EnsureRunStarted()
        {
            if (!GameFlowSession.HasRun)
            {
                GameFlowSession.StartNewRun();
            }
        }

        private static RunGameState GetInitialRunState()
        {
            return GameFlowSession.IsTutorialRun
                ? RunGameState.Battle
                : RunGameState.StartRelicSelect;
        }

        protected virtual void Start()
        {
            _navigator.GoTo(GetInitialRunState());
        }

        protected virtual void OnDestroy()
        {
            CancelDefeatCountdown();
            UnsubscribeEvents();
            _relicIconRenderer?.Dispose();
            _relicIconRenderer = null;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ── ViewModel 생성 ───────────────────────────────────────────────

        private void CreateViewModels()
        {
            _startRelicSelectVM = new StartRelicSelectViewModel();
            _rewardVM           = new RunRewardViewModel();
            _hudVM              = new RunHUDViewModel();
            _inventoryVM        = new RunInventoryViewModel();
            _defeatVM           = new RunDefeatViewModel();
            _leaderboardVM      = new LeaderboardViewModel();
        }

        // ── View Bind ────────────────────────────────────────────────────

        private void BindViews()
        {
            if (_startRelicSelectView != null)
            {
                RenderStartRelicState(_startRelicSelectVM.State);
            }

            if (_rewardView != null)
            {
                RenderRewardState(_rewardVM.State);
            }

            if (_hudView != null)
            {
                _hudView.Render(_hudVM.State);
            }

            if (_inventoryView != null)
            {
                _inventoryView.Render(_inventoryVM.State);
            }

            if (_defeatView != null)
            {
                _defeatView.Render(_defeatVM.State);
            }

            if (_leaderboardView != null)
            {
                _leaderboardView.Render(_leaderboardVM.State);
            }
        }

        // ── Navigator 등록 ───────────────────────────────────────────────

        private void RegisterViews()
        {
            RegisterIfPresent(RunGameState.StartRelicSelect, _startRelicSelectView);
            RegisterIfPresent(RunGameState.Battle,           _battleView);
            RegisterIfPresent(RunGameState.Reward,           _rewardView);
            RegisterIfPresent(RunGameState.Defeat,           _defeatView);
        }

        // v1에서 미사용인 View(예: Map/HUD)는 인스펙터 미연결이 정상이므로 등록을 건너뜁니다.
        private void RegisterIfPresent(RunGameState state, IRunGameView view)
        {
            if (view as UnityEngine.Object == null) return;
            _navigator.Register(state, view);
        }

        // ── 이벤트 구독 ──────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            _startRelicSelectVM.Changed += HandleStartRelicStateChanged;
            _startRelicSelectVM.RelicSelected += HandleStarterRelicSelected;
            _rewardVM.Changed += HandleRewardStateChanged;
            _rewardVM.RewardClaimed += HandleRewardClaimed;
            _hudVM.Changed += HandleHudStateChanged;
            _hudVM.PauseRequested += OnPauseRequested;
            _inventoryVM.Changed += HandleInventoryStateChanged;
            _defeatVM.Changed += HandleDefeatStateChanged;
            _defeatVM.RestartRequested += HandleRestartRequested;
            _defeatVM.RankingRequested += HandleRankingRequested;
            _defeatVM.HomeRequested += HandleHomeRequested;
            _defeatVM.ReviveRequested += HandleReviveRequested;

            if (_startRelicSelectView != null)
            {
                _startRelicSelectView.Entered += HandleStartRelicEntered;
                _startRelicSelectView.RelicSelectionRequested += HandleStarterRelicSelectionRequested;
            }

            if (_rewardView != null)
            {
                _rewardView.Entered += HandleRewardEntered;
                _rewardView.RewardSelectionRequested += HandleRewardSelectionRequested;
                _rewardView.RerollRequested += HandleRewardRerollRequested;
                _rewardView.ExtraRewardRequested += HandleExtraRewardRequested;
                _rewardView.RewardDoubleRequested += HandleRewardDoubleRequested;
            }

            if (_battleView != null)
            {
                _battleView.Entered += HandleBattleEntered;
            }

            if (_hudView != null)
            {
                _hudView.PauseRequested += _hudVM.RequestPause;
            }

            if (_inventoryView != null)
            {
                _inventoryView.OpenRequested += HandleInventoryOpenRequested;
                _inventoryView.CloseRequested += HandleInventoryCloseRequested;
                _inventoryView.SymbolTabRequested +=
                    HandleInventorySymbolTabRequested;
                _inventoryView.RelicTabRequested +=
                    HandleInventoryRelicTabRequested;
            }

            if (_defeatView != null)
            {
                _defeatView.RestartRequested += _defeatVM.RequestRestart;
                _defeatView.RankingRequested += _defeatVM.RequestRanking;
                _defeatView.HomeRequested += _defeatVM.RequestHome;
                _defeatView.ReviveRequested += _defeatVM.RequestRevive;
            }

            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.RewardedAvailabilityChanged +=
                    RefreshRewardedAvailability;
                AdsManager.Instance.RewardedSessionEnded +=
                    HandleRewardedSessionEnded;
            }

            AdsRemoveState.Changed += HandleAdsRemoveChanged;

            if (_leaderboardView != null)
            {
                _leaderboardView.CloseRequested += _leaderboardVM.Close;
                _leaderboardView.RefreshRequested += HandleLeaderboardRefreshRequested;
                _leaderboardVM.Changed += _leaderboardView.Render;
            }

            if (_battleSceneCompositionRoot != null)
            {
                _battleSceneCompositionRoot.TutorialSignalRaised +=
                    HandleBattleTutorialSignalRaised;
                _battleSceneCompositionRoot.BattleVictory += OnBattleVictory;
                _battleSceneCompositionRoot.BattleDefeat += OnBattleDefeat;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_startRelicSelectVM != null)
            {
                _startRelicSelectVM.Changed -= HandleStartRelicStateChanged;
                _startRelicSelectVM.RelicSelected -= HandleStarterRelicSelected;
            }

            if (_rewardVM != null)
            {
                _rewardVM.Changed -= HandleRewardStateChanged;
                _rewardVM.RewardClaimed -= HandleRewardClaimed;
            }

            if (_hudVM != null)
            {
                _hudVM.Changed -= HandleHudStateChanged;
                _hudVM.PauseRequested -= OnPauseRequested;
            }

            if (_inventoryVM != null)
            {
                _inventoryVM.Changed -= HandleInventoryStateChanged;
            }

            if (_defeatVM != null)
            {
                _defeatVM.Changed -= HandleDefeatStateChanged;
                _defeatVM.RestartRequested -= HandleRestartRequested;
                _defeatVM.RankingRequested -= HandleRankingRequested;
                _defeatVM.HomeRequested -= HandleHomeRequested;
                _defeatVM.ReviveRequested -= HandleReviveRequested;
            }

            if (_startRelicSelectView != null)
            {
                _startRelicSelectView.Entered -= HandleStartRelicEntered;
                _startRelicSelectView.RelicSelectionRequested -= HandleStarterRelicSelectionRequested;
            }

            if (_rewardView != null)
            {
                _rewardView.Entered -= HandleRewardEntered;
                _rewardView.RewardSelectionRequested -= HandleRewardSelectionRequested;
                _rewardView.RerollRequested -= HandleRewardRerollRequested;
                _rewardView.ExtraRewardRequested -= HandleExtraRewardRequested;
                _rewardView.RewardDoubleRequested -= HandleRewardDoubleRequested;
            }

            if (_battleView != null)
            {
                _battleView.Entered -= HandleBattleEntered;
            }

            if (_hudView != null)
            {
                _hudView.PauseRequested -= _hudVM.RequestPause;
            }

            if (_inventoryView != null && _inventoryVM != null)
            {
                _inventoryView.OpenRequested -= HandleInventoryOpenRequested;
                _inventoryView.CloseRequested -= HandleInventoryCloseRequested;
                _inventoryView.SymbolTabRequested -=
                    HandleInventorySymbolTabRequested;
                _inventoryView.RelicTabRequested -=
                    HandleInventoryRelicTabRequested;
            }

            if (_defeatView != null && _defeatVM != null)
            {
                _defeatView.RestartRequested -= _defeatVM.RequestRestart;
                _defeatView.RankingRequested -= _defeatVM.RequestRanking;
                _defeatView.HomeRequested -= _defeatVM.RequestHome;
                _defeatView.ReviveRequested -= _defeatVM.RequestRevive;
            }

            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.RewardedAvailabilityChanged -=
                    RefreshRewardedAvailability;
                AdsManager.Instance.RewardedSessionEnded -=
                    HandleRewardedSessionEnded;
            }

            AdsRemoveState.Changed -= HandleAdsRemoveChanged;

            if (_leaderboardView != null && _leaderboardVM != null)
            {
                _leaderboardView.CloseRequested -= _leaderboardVM.Close;
                _leaderboardView.RefreshRequested -= HandleLeaderboardRefreshRequested;
                _leaderboardVM.Changed -= _leaderboardView.Render;
            }

            if (_battleSceneCompositionRoot != null)
            {
                _battleSceneCompositionRoot.TutorialSignalRaised -=
                    HandleBattleTutorialSignalRaised;
                _battleSceneCompositionRoot.BattleVictory -= OnBattleVictory;
                _battleSceneCompositionRoot.BattleDefeat -= OnBattleDefeat;
            }
        }

        private void HandleStartRelicEntered()
        {
            _startRelicSelectVM.Refresh();
        }

        private void HandleStarterRelicSelectionRequested(string relicId)
        {
            _startRelicSelectVM.SelectRelic(relicId);
        }

        private void HandleStartRelicStateChanged(StartRelicSelectViewState state)
        {
            if (_startRelicSelectView != null)
            {
                RenderStartRelicState(state);
            }
        }

        private void HandleStarterRelicSelected()
        {
            _tutorialOverlayView?.Hide();
            RefreshHud();
            _navigator.GoTo(RunGameState.Battle);
        }

        private void HandleRewardEntered()
        {
            _rewardVM.Refresh();
            RefreshRewardedAvailability();
        }

        private void HandleRewardSelectionRequested(int optionIndex)
        {
            _rewardVM.ClaimReward(optionIndex);
        }

        private void HandleRewardRerollRequested()
        {
            RunRewardedOrSkip(
                RewardedAdPurpose.RewardReroll,
                HandleRewardRerollRewarded);
        }

        private void HandleRewardRerollRewarded()
        {
            Debug.Log("[RunGameSceneRoot] Applying rewarded reroll.");
            _rewardVM.ApplyRewardedReroll();
        }

        private void HandleExtraRewardRequested()
        {
            RunRewardedOrSkip(
                RewardedAdPurpose.ExtraReward,
                _rewardVM.ApplyRewardedExtraReward);
        }

        private void HandleRewardDoubleRequested()
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
                    $"[RunGameSceneRoot] Rewarded is not ready for {purpose}.");
                RefreshRewardedAvailability();
                return;
            }

            adsManager.ShowRewarded(purpose, rewardCallback);
        }

        private void HandleRewardStateChanged(RunRewardViewState state)
        {
            if (_rewardView != null)
            {
                RenderRewardState(state);
            }
        }

        private void HandleRewardClaimed()
        {
            GameFlowSession.AdvanceToNextBattle();
            RefreshHud();
            _inventoryVM?.Refresh();
            StartNextBattle();
        }

        private void HandleHudStateChanged(RunHUDViewState state)
        {
            if (_hudView != null)
            {
                _hudView.Render(state);
            }
        }

        private void HandleInventoryStateChanged(RunInventoryViewState state)
        {
            if (_inventoryView != null)
            {
                _inventoryView.Render(state);
            }
        }

        private void HandleInventoryOpenRequested()
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

        private void HandleInventoryCloseRequested()
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

        private void HandleInventorySymbolTabRequested()
        {
            _inventoryVM.SelectTab(RunInventoryTab.SymbolPool);
        }

        private void HandleInventoryRelicTabRequested()
        {
            _inventoryVM.SelectTab(RunInventoryTab.Relics);
        }

        private void HandleBattleEntered()
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

        private void HandleBattleTutorialSignalRaised(BattleTutorialSignal signal)
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

        private void OnBattleVictory()
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
                    cancellationToken: this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
                return;
            }

            GameFlowSession.EndRun();
            GameSceneLoader.LoadGameStart();
        }

        private void OnBattleDefeat()
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

        private void HandleDefeatStateChanged(RunDefeatViewState state)
        {
            if (_defeatView != null)
            {
                _defeatView.Render(state);
            }
        }

        private void HandleRestartRequested()
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

        private void HandleRankingRequested()
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

        private void HandleHomeRequested()
        {
            FinalizePendingDefeat();
            GameSceneLoader.LoadGameStart();
        }

        private void HandleReviveRequested()
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
                Debug.LogWarning("[RunGameSceneRoot] Rewarded revive is not ready.");
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

        private void HandleRewardedSessionEnded(
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

        private void HandleLeaderboardRefreshRequested()
        {
            _leaderboardVM.RefreshAsync().Forget();
        }

        private void HandleAdsRemoveChanged(bool isRemoved)
        {
            RefreshRewardedAvailability();
        }

        private void EnsureDefeatView()
        {
            if (_defeatView != null)
            {
                _defeatView.EnsureRuntimeLayout();
                return;
            }

            Transform searchRoot = _navigator != null ? _navigator.transform.root : transform.root;
            Transform host = SceneComponentResolver.FindDeepChild(searchRoot, "DefeatView") ??
                SceneComponentResolver.FindDeepChild(searchRoot, "GameOverView");
            if (host == null)
            {
                Debug.LogError("[RunGameSceneRoot] DefeatView host must be placed in the scene hierarchy.");
                return;
            }

            _defeatView = host.GetComponent<RunDefeatView>();
            if (_defeatView == null)
            {
                Debug.LogError("[RunGameSceneRoot] DefeatView host requires RunDefeatView.");
                return;
            }

            _defeatView.EnsureRuntimeLayout();
            host.gameObject.SetActive(false);
        }

        private void EnsureLeaderboardView()
        {
            if (_leaderboardView != null)
            {
                _leaderboardView.EnsureRuntimeLayout();
                _leaderboardView.SetLauncherVisible(false);
                return;
            }

            Transform searchRoot = _navigator != null ? _navigator.transform.root : transform.root;
            _leaderboardView =
                searchRoot.GetComponentInChildren<LeaderboardView>(includeInactive: true);

            if (_leaderboardView == null)
            {
                return;
            }

            _leaderboardView.EnsureRuntimeLayout();
            _leaderboardView.SetLauncherVisible(false);
        }

        private void EnsureInventoryView()
        {
            if (_inventoryView != null)
            {
                _inventoryView.EnsureRuntimeLayout();
                return;
            }

            Transform searchRoot = _navigator != null ? _navigator.transform.root : transform.root;
            _inventoryView =
                searchRoot.GetComponentInChildren<RunInventoryView>(
                    includeInactive: true);

            if (_inventoryView == null)
            {
                Canvas canvas = null;
                if (_battleView != null)
                {
                    canvas = _battleView.GetComponentInParent<Canvas>();
                }

                canvas ??= searchRoot.GetComponentInChildren<Canvas>(
                    includeInactive: true);
                if (canvas != null)
                {
                    _inventoryView = RunInventoryView.CreateRuntime(canvas.transform);
                }
            }

            _inventoryView?.EnsureRuntimeLayout();
        }

        private void EnsureTutorialOverlayView()
        {
            if (_tutorialOverlayView != null)
            {
                _tutorialOverlayView.EnsureRuntimeLayout();
                return;
            }

            Transform searchRoot = _navigator != null ? _navigator.transform.root : transform.root;
            _tutorialOverlayView =
                searchRoot.GetComponentInChildren<RunTutorialOverlayView>(
                    includeInactive: true);

            if (_tutorialOverlayView == null)
            {
                Canvas canvas = null;
                if (_battleView != null)
                {
                    canvas = _battleView.GetComponentInParent<Canvas>();
                }

                canvas ??= searchRoot.GetComponentInChildren<Canvas>(
                    includeInactive: true);
                if (canvas != null)
                {
                    _tutorialOverlayView =
                        RunTutorialOverlayView.CreateRuntime(canvas.transform);
                }
            }

            _tutorialOverlayView?.EnsureRuntimeLayout();
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

        private static void OnPauseRequested()
        {
            // TODO: UI_PopupCanvas의 PausePopup 활성화
            Debug.Log("[RunGameSceneRoot] Pause requested.");
        }

        private void RefreshHud()
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

        private void RefreshRewardedAvailability()
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
            _defeatCountdownCts = new CancellationTokenSource();
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
                GameFlowSession.BuildSlotSymbolContributionSummary());
            RefreshRewardedAvailability();
        }

        private void CancelDefeatCountdown()
        {
            _defeatCountdownCts?.Cancel();
            _defeatCountdownCts?.Dispose();
            _defeatCountdownCts = null;
        }

        private void RenderStartRelicState(StartRelicSelectViewState state)
        {
            _startRelicSelectView.Render(state);
            _relicIconRenderer?.RenderStartRelicIcons(_startRelicSelectView, state);
        }

        private void RenderRewardState(RunRewardViewState state)
        {
            _rewardView.Render(state);
            _relicIconRenderer?.RenderRewardIcons(_rewardView, state);
        }
    }

    // ── View Bind 인터페이스 ─────────────────────────────────────────────

    public interface IStartRelicSelectView : IRunGameView
    {
        void Render(StartRelicSelectViewState state);
    }

    public interface IRunRewardView : IRunGameView
    {
        void Render(RunRewardViewState state);
    }

    public interface IRunHUDView : IRunGameView
    {
        void Render(RunHUDViewState state);
    }

    public sealed class RunInventoryView : MonoBehaviour
    {
        private static readonly Color BackdropColor = new(0.02f, 0.015f, 0.025f, 0.72f);
        private static readonly Color PanelColor = new(0.06f, 0.055f, 0.08f, 0.98f);
        private static readonly Color TabActiveColor = new(0.88f, 0.62f, 0.18f, 1f);
        private static readonly Color TabInactiveColor = new(0.18f, 0.18f, 0.24f, 1f);
        private static readonly Color CloseButtonColor = new(0.35f, 0.09f, 0.12f, 1f);

        [SerializeField] private Button _openButton;
        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _summaryText;
        [SerializeField] private Button _symbolTabButton;
        [SerializeField] private Text _symbolTabText;
        [SerializeField] private Button _relicTabButton;
        [SerializeField] private Text _relicTabText;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Text _contentText;
        [SerializeField] private Button _closeButton;

        private bool _subscribed;

        public event Action OpenRequested;

        public event Action CloseRequested;

        public event Action SymbolTabRequested;

        public event Action RelicTabRequested;

        public static RunInventoryView CreateRuntime(Transform canvasTransform)
        {
            var hostObject = new GameObject("RunInventoryView", typeof(RectTransform));
            var rectTransform = (RectTransform)hostObject.transform;
            rectTransform.SetParent(canvasTransform, false);
            Stretch(rectTransform);

            RunInventoryView view = hostObject.AddComponent<RunInventoryView>();
            view.EnsureRuntimeLayout();
            return view;
        }

        private void Awake()
        {
            EnsureRuntimeLayout();
        }

        private void OnDestroy()
        {
            UnsubscribeButtons();
        }

        public void EnsureRuntimeLayout()
        {
            ResolveSceneReferences();
            EnsureOpenButton();

            if (_panelRoot == null)
            {
                BuildRuntimeLayout();
            }

            SubscribeButtons();
        }

        public void Render(RunInventoryViewState state)
        {
            EnsureRuntimeLayout();

            state ??= RunInventoryViewState.Empty;
            if (_panelRoot != null)
            {
                _panelRoot.gameObject.SetActive(state.IsOpen);
            }

            if (!state.IsOpen)
            {
                return;
            }

            transform.SetAsLastSibling();
            SetText(_titleText, "런 인벤토리");
            SetText(_summaryText, state.Summary);
            RenderTabs(state.ActiveTab);
            SetText(_contentText, BuildContentText(state));

            if (_contentText != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    _contentText.rectTransform);
            }

            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void ResolveSceneReferences()
        {
            _panelRoot ??= FindDeepChild(transform, "Run Inventory Panel") as RectTransform;
            _titleText ??= FindChildComponent<Text>("Run Inventory Title");
            _summaryText ??= FindChildComponent<Text>("Run Inventory Summary");
            _symbolTabButton ??= FindChildComponent<Button>("Symbol Pool Tab Button");
            _symbolTabText ??= FindChildComponent<Text>("Symbol Pool Tab Text");
            _relicTabButton ??= FindChildComponent<Button>("Relic Tab Button");
            _relicTabText ??= FindChildComponent<Text>("Relic Tab Text");
            _scrollRect ??= FindChildComponent<ScrollRect>("Run Inventory Scroll");
            _contentText ??= FindChildComponent<Text>("Run Inventory Content");
            _closeButton ??= FindChildComponent<Button>("Run Inventory Close Button");
        }

        private void EnsureOpenButton()
        {
            if (_openButton != null)
            {
                return;
            }

            Transform searchRoot = transform.root != null ? transform.root : transform;
            Transform origin =
                SceneComponentResolver.FindDeepChild(searchRoot, "Relic Inventory Origin");
            if (origin == null)
            {
                return;
            }

            _openButton = origin.GetComponent<Button>();
            Image image = origin.GetComponent<Image>();
            if (_openButton == null)
            {
                _openButton = origin.gameObject.AddComponent<Button>();
            }

            if (image != null)
            {
                image.raycastTarget = true;
                _openButton.targetGraphic = image;
            }
        }

        private void BuildRuntimeLayout()
        {
            Font font = Resources.Load<Font>("Galmuri11-Bold");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            _panelRoot = CreateRect("Run Inventory Panel", transform);
            Stretch(_panelRoot);
            Image backdrop = _panelRoot.gameObject.AddComponent<Image>();
            backdrop.color = BackdropColor;

            Button backdropButton = _panelRoot.gameObject.AddComponent<Button>();
            backdropButton.targetGraphic = backdrop;

            RectTransform panel = CreateRect("Run Inventory Body", _panelRoot);
            SetAnchors(panel, new Vector2(0.07f, 0.12f), new Vector2(0.93f, 0.88f));
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;

            _titleText = CreateText(
                "Run Inventory Title",
                panel,
                font,
                46,
                new Vector2(0.05f, 0.88f),
                new Vector2(0.78f, 0.98f),
                TextAnchor.MiddleLeft);
            _titleText.color = new Color(1f, 0.84f, 0.48f, 1f);

            _summaryText = CreateText(
                "Run Inventory Summary",
                panel,
                font,
                25,
                new Vector2(0.05f, 0.81f),
                new Vector2(0.95f, 0.88f),
                TextAnchor.MiddleLeft);
            _summaryText.color = new Color(0.82f, 0.88f, 1f, 1f);

            CreateButton(
                "Run Inventory Close Button",
                "Run Inventory Close Text",
                panel,
                font,
                "닫기",
                CloseButtonColor,
                new Vector2(0.80f, 0.90f),
                new Vector2(0.95f, 0.97f),
                out _closeButton,
                out _);

            CreateButton(
                "Symbol Pool Tab Button",
                "Symbol Pool Tab Text",
                panel,
                font,
                "심볼 풀",
                TabActiveColor,
                new Vector2(0.05f, 0.72f),
                new Vector2(0.49f, 0.80f),
                out _symbolTabButton,
                out _symbolTabText);

            CreateButton(
                "Relic Tab Button",
                "Relic Tab Text",
                panel,
                font,
                "유물",
                TabInactiveColor,
                new Vector2(0.51f, 0.72f),
                new Vector2(0.95f, 0.80f),
                out _relicTabButton,
                out _relicTabText);

            _contentText = CreateScrollableText(
                "Run Inventory Scroll",
                "Run Inventory Content",
                panel,
                font,
                new Vector2(0.05f, 0.05f),
                new Vector2(0.95f, 0.70f),
                out _scrollRect);

            _panelRoot.gameObject.SetActive(false);
        }

        private void SubscribeButtons()
        {
            UnsubscribeButtons();

            if (_openButton != null)
            {
                _openButton.onClick.AddListener(HandleOpenClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_panelRoot != null)
            {
                Button backdropButton = _panelRoot.GetComponent<Button>();
                if (backdropButton != null)
                {
                    backdropButton.onClick.AddListener(HandleCloseClicked);
                }
            }

            if (_symbolTabButton != null)
            {
                _symbolTabButton.onClick.AddListener(HandleSymbolTabClicked);
            }

            if (_relicTabButton != null)
            {
                _relicTabButton.onClick.AddListener(HandleRelicTabClicked);
            }

            _subscribed = true;
        }

        private void UnsubscribeButtons()
        {
            if (!_subscribed)
            {
                return;
            }

            _openButton?.onClick.RemoveListener(HandleOpenClicked);
            _closeButton?.onClick.RemoveListener(HandleCloseClicked);
            if (_panelRoot != null)
            {
                Button backdropButton = _panelRoot.GetComponent<Button>();
                backdropButton?.onClick.RemoveListener(HandleCloseClicked);
            }

            _symbolTabButton?.onClick.RemoveListener(HandleSymbolTabClicked);
            _relicTabButton?.onClick.RemoveListener(HandleRelicTabClicked);
            _subscribed = false;
        }

        private void RenderTabs(RunInventoryTab activeTab)
        {
            bool symbolsActive = activeTab == RunInventoryTab.SymbolPool;
            SetButtonColor(_symbolTabButton, symbolsActive ? TabActiveColor : TabInactiveColor);
            SetButtonColor(_relicTabButton, symbolsActive ? TabInactiveColor : TabActiveColor);
            SetTextColor(_symbolTabText, symbolsActive ? Color.black : Color.white);
            SetTextColor(_relicTabText, symbolsActive ? Color.white : Color.black);
        }

        private static string BuildContentText(RunInventoryViewState state)
        {
            return state.ActiveTab == RunInventoryTab.SymbolPool
                ? BuildSymbolContent(state)
                : BuildRelicContent(state);
        }

        private static string BuildSymbolContent(RunInventoryViewState state)
        {
            if (state.Symbols.Count == 0)
            {
                return "심볼 풀이 비어 있습니다.";
            }

            var builder = new StringBuilder();
            builder.AppendLine("현재 심볼 풀");
            builder.AppendLine();
            for (int index = 0; index < state.Symbols.Count; index++)
            {
                RunInventorySymbolViewState symbol = state.Symbols[index];
                builder.Append(symbol.DisplayName);
                builder.Append("  ");
                builder.Append(symbol.Count);
                builder.Append("개");
                builder.Append(symbol.IsHighProbability ? "  기본 고확률" : "  기본 저확률");
                if (index < state.Symbols.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string BuildRelicContent(RunInventoryViewState state)
        {
            if (state.Relics.Count == 0)
            {
                return "보유 유물이 없습니다.";
            }

            var builder = new StringBuilder();
            builder.Append("현재 유물 ");
            builder.Append(state.Relics.Count);
            builder.AppendLine("개");
            for (int index = 0; index < state.Relics.Count; index++)
            {
                RunInventoryRelicViewState relic = state.Relics[index];
                builder.AppendLine();
                builder.Append(index + 1);
                builder.Append(". ");
                builder.Append(relic.Name);
                builder.Append(" [");
                builder.Append(relic.Id);
                builder.Append("]");
                builder.AppendLine();
                builder.Append('[');
                builder.Append(relic.Grade);
                builder.Append(" · ");
                builder.Append(relic.Role);
                builder.AppendLine("]");
                builder.Append(relic.Description);
                if (index < state.Relics.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private void HandleOpenClicked()
        {
            OpenRequested?.Invoke();
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        private void HandleSymbolTabClicked()
        {
            SymbolTabRequested?.Invoke();
        }

        private void HandleRelicTabClicked()
        {
            RelicTabRequested?.Invoke();
        }

        private T FindChildComponent<T>(string objectName) where T : Component
        {
            Transform child = FindDeepChild(transform, objectName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static Transform FindDeepChild(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == objectName)
            {
                return parent;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform found = FindDeepChild(parent.GetChild(index), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Text CreateScrollableText(
            string scrollName,
            string contentName,
            Transform parent,
            Font font,
            Vector2 anchorMin,
            Vector2 anchorMax,
            out ScrollRect scrollRect)
        {
            RectTransform viewport = CreateRect(scrollName, parent);
            SetAnchors(viewport, anchorMin, anchorMax);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0.035f, 0.035f, 0.055f, 0.95f);

            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.viewport = viewport;

            RectTransform content = CreateRect(contentName, viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(-36f, 0f);

            Text text = content.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 26;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = content;
            return text;
        }

        private static void CreateButton(
            string buttonName,
            string textName,
            Transform parent,
            Font font,
            string label,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            out Button button,
            out Text buttonText)
        {
            RectTransform buttonRect = CreateRect(buttonName, parent);
            SetAnchors(buttonRect, anchorMin, anchorMax);
            Image image = buttonRect.gameObject.AddComponent<Image>();
            image.color = color;

            button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            buttonText = CreateText(
                textName,
                buttonRect,
                font,
                25,
                Vector2.zero,
                Vector2.one,
                TextAnchor.MiddleCenter);
            buttonText.text = label;
        }

        private static Text CreateText(
            string objectName,
            Transform parent,
            Font font,
            int fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            TextAnchor alignment)
        {
            RectTransform rectTransform = CreateRect(objectName, parent);
            SetAnchors(rectTransform, anchorMin, anchorMax);

            Text text = rectTransform.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            SetAnchors(rectTransform, Vector2.zero, Vector2.one);
        }

        private static void SetAnchors(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static void SetTextColor(Text text, Color color)
        {
            if (text != null)
            {
                text.color = color;
            }
        }

        private static void SetButtonColor(Button button, Color color)
        {
            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = color;
            }
        }
    }
}
