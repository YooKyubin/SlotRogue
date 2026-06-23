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

namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// RunGameScene의 최상위 조립자입니다.
    /// ViewModel 생성 → FlowController 생성 → View Bind → Navigator 등록 →
    /// 입력 event 연결 → 첫 화면 진입을 담당합니다.
    ///
    /// 규칙:
    ///  - 화면 흐름 제어(전투/보상/패배/부활/튜토리얼/네비게이션)는 RunGameFlowController에 위임합니다.
    ///  - 게임 규칙·보상 계산은 GameFlowSession 또는 각 ViewModel에 위임합니다.
    ///  - 이 클래스는 '무엇을 만들고 무엇을 무엇에 연결하는가'만 담당합니다.
    /// </summary>
    public class RunGameSceneRoot : MonoBehaviour
    {
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
        private RunGameFlowController      _flow;

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
            CreateFlowController();
            BindViews();
            RegisterViews();
            SubscribeEvents();
            _flow.RefreshRewardedAvailability();
            _flow.RefreshHud();
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

        protected virtual void Start()
        {
            _navigator.GoTo(RunGameFlowController.GetInitialRunState());
        }

        protected virtual void OnDestroy()
        {
            _flow?.Dispose();
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

        // ── FlowController 생성 ──────────────────────────────────────────

        private void CreateFlowController()
        {
            _flow = new RunGameFlowController(
                _navigator,
                _startRelicSelectVM,
                _rewardVM,
                _hudVM,
                _inventoryVM,
                _defeatVM,
                _leaderboardVM,
                _battleView,
                _battleSceneCompositionRoot,
                _tutorialOverlayView,
                _defeatView,
                _leaderboardView,
                this.GetCancellationTokenOnDestroy());
        }

        // ── View Bind ────────────────────────────────────────────────────

        private void BindViews()
        {
            // 각 View가 자기 ViewModel을 구독(상태→Render, .AddTo(this))하고 입력 event를
            // FlowController(presenter)로 연결한다(ADR-0020). R3 구독은 즉시 초기값을 흘려보내
            // 첫 Render를 처리하므로 여기서 따로 Render하지 않는다.
            _startRelicSelectView?.Bind(_startRelicSelectVM, _flow, _relicIconRenderer);
            _rewardView?.Bind(_rewardVM, _flow, _relicIconRenderer);
            _hudView?.Bind(_hudVM);
            _inventoryView?.Bind(_inventoryVM, _flow);
            _defeatView?.Bind(_defeatVM);
            _leaderboardView?.Bind(_leaderboardVM);
            _battleView?.Bind(_flow);
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
        // 화면 상태는 R3 ReactiveProperty로 바인딩한다: 구독 한 번이면 이후 갱신이 자동 반영되고,
        // AddTo(this)가 이 컴포넌트 파괴 시 구독을 자동 해제한다.
        // 입력 event는 View/ViewModel → FlowController로 연결한다.

        private void SubscribeEvents()
        {
            // View 상태 구독과 View 입력 event 연결은 각 View의 Bind(BindViews)가 소유한다(ADR-0020).
            // 여기서는 ViewModel/System → presenter 같은 비-View 배선만 담당한다.
            _startRelicSelectVM.RelicSelected += _flow.HandleStarterRelicSelected;
            _rewardVM.RewardClaimed += _flow.HandleRewardClaimed;
            _hudVM.PauseRequested += _flow.OnPauseRequested;
            _defeatVM.RestartRequested += _flow.HandleRestartRequested;
            _defeatVM.RankingRequested += _flow.HandleRankingRequested;
            _defeatVM.HomeRequested += _flow.HandleHomeRequested;
            _defeatVM.ReviveRequested += _flow.HandleReviveRequested;

            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.RewardedAvailabilityChanged +=
                    _flow.RefreshRewardedAvailability;
                AdsManager.Instance.RewardedSessionEnded +=
                    _flow.HandleRewardedSessionEnded;
            }

            AdsRemoveState.Changed += _flow.HandleAdsRemoveChanged;

            // Leaderboard의 상태 구독·close/refresh 입력은 LeaderboardView.Bind가 소유한다(ADR-0020).
            // 패배 RANKING 진입(OpenAsync)은 presenter(HandleRankingRequested)가 담당한다.

            if (_battleSceneCompositionRoot != null)
            {
                _battleSceneCompositionRoot.TutorialSignalRaised +=
                    _flow.HandleBattleTutorialSignalRaised;
                _battleSceneCompositionRoot.BattleVictory += _flow.OnBattleVictory;
                _battleSceneCompositionRoot.BattleDefeat += _flow.OnBattleDefeat;
            }
        }

        private void UnsubscribeEvents()
        {
            // State 구독(_startRelicSelectVM/_rewardVM/_hudVM/_inventoryVM/_defeatVM)은
            // R3 AddTo(this)로 자동 해제되므로 여기서 수동 -= 하지 않는다.
            if (_flow == null)
            {
                return;
            }

            if (_startRelicSelectVM != null)
            {
                _startRelicSelectVM.RelicSelected -= _flow.HandleStarterRelicSelected;
            }

            if (_rewardVM != null)
            {
                _rewardVM.RewardClaimed -= _flow.HandleRewardClaimed;
            }

            if (_hudVM != null)
            {
                _hudVM.PauseRequested -= _flow.OnPauseRequested;
            }

            if (_defeatVM != null)
            {
                _defeatVM.RestartRequested -= _flow.HandleRestartRequested;
                _defeatVM.RankingRequested -= _flow.HandleRankingRequested;
                _defeatVM.HomeRequested -= _flow.HandleHomeRequested;
                _defeatVM.ReviveRequested -= _flow.HandleReviveRequested;
            }

            // View 입력 event(start/reward/inventory/hud/defeat/battle)는 각 View가 자기 publisher라
            // View 파괴 시 함께 해제되므로 여기서 -= 하지 않는다(ADR-0020).

            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.RewardedAvailabilityChanged -=
                    _flow.RefreshRewardedAvailability;
                AdsManager.Instance.RewardedSessionEnded -=
                    _flow.HandleRewardedSessionEnded;
            }

            AdsRemoveState.Changed -= _flow.HandleAdsRemoveChanged;

            // Leaderboard 입력 event는 View가 publisher라 파괴 시 자동 해제된다(ADR-0020).

            if (_battleSceneCompositionRoot != null)
            {
                _battleSceneCompositionRoot.TutorialSignalRaised -=
                    _flow.HandleBattleTutorialSignalRaised;
                _battleSceneCompositionRoot.BattleVictory -= _flow.OnBattleVictory;
                _battleSceneCompositionRoot.BattleDefeat -= _flow.OnBattleDefeat;
            }
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
                Debug.LogError(
                    "[RunGameSceneRoot] RunInventoryView must be placed in the scene hierarchy.");
                return;
            }

            _inventoryView.EnsureRuntimeLayout();
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
                Debug.LogError(
                    "[RunGameSceneRoot] RunTutorialOverlayView must be placed in the scene hierarchy.");
                return;
            }

            _tutorialOverlayView.EnsureRuntimeLayout();
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

}
