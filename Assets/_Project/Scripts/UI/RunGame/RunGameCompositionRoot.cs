using SlotRogue.UI.GameFlow;
using SlotRogue.UI.RunGame.ViewModels;
using UnityEngine;

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
    public sealed class RunGameCompositionRoot : MonoBehaviour
    {
        public static RunGameCompositionRoot Instance { get; private set; }

        // ── Inspector 연결 ───────────────────────────────────────────────

        [Header("Navigator")]
        [SerializeField] private RunGameNavigator _navigator;

        [Header("Game Views  (IRunGameView 구현체)")]
        [SerializeField] private StartArtifactSelectionView _startRelicSelectView;
        [SerializeField] private RunMapView                 _mapView;
        [SerializeField] private BattleView                 _battleView;
        [SerializeField] private RunRewardView              _rewardView;

        [Header("HUD  (항상 표시)")]
        [SerializeField] private RunHUDView _hudView;

        // ── ViewModel ────────────────────────────────────────────────────

        private StartRelicSelectViewModel _startRelicSelectVM;
        private RunMapViewModel           _mapVM;
        private RunRewardViewModel        _rewardVM;
        private RunHUDViewModel           _hudVM;

        // ── 초기화 ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            GameFlowSession.StartNewRun();

            CreateViewModels();
            BindViews();
            RegisterViews();
            SubscribeEvents();
        }

        private void Start()
        {
            _navigator.GoTo(RunGameState.StartRelicSelect);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── ViewModel 생성 ───────────────────────────────────────────────

        private void CreateViewModels()
        {
            _startRelicSelectVM = new StartRelicSelectViewModel();
            _mapVM              = new RunMapViewModel();
            _rewardVM           = new RunRewardViewModel();
            _hudVM              = new RunHUDViewModel();
        }

        // ── View Bind ────────────────────────────────────────────────────

        private void BindViews()
        {
            _startRelicSelectView?.Bind(_startRelicSelectVM);
            _mapView?.Bind(_mapVM);
            _rewardView?.Bind(_rewardVM);
            _hudView?.Bind(_hudVM);
            // BattleView는 RunBattleCompositionRoot를 직접 참조하므로 별도 Bind 불필요
        }

        // ── Navigator 등록 ───────────────────────────────────────────────

        private void RegisterViews()
        {
            RegisterIfPresent(RunGameState.StartRelicSelect, _startRelicSelectView);
            RegisterIfPresent(RunGameState.Map,              _mapView);
            RegisterIfPresent(RunGameState.Battle,           _battleView);
            RegisterIfPresent(RunGameState.Reward,           _rewardView);
        }

        // v1에서 미사용인 View(예: Map/HUD)는 인스펙터 미연결이 정상이므로 등록을 건너뜁니다.
        private void RegisterIfPresent(RunGameState state, IRunGameView view)
        {
            if (view as Object == null) return;
            _navigator.Register(state, view);
        }

        // ── 이벤트 구독 ──────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            // 시작 유물 선택 완료 → 첫 전투로 (무한모드는 맵을 건너뜀)
            _startRelicSelectVM.ArtifactSelected += _ =>
            {
                _hudVM.Refresh();
                _navigator.GoTo(RunGameState.Battle);
            };

            // (스토리모드용) 맵 노드 선택 → 전투로. 무한모드에선 Map에 진입하지 않음.
            _mapVM.NodeSelected += _ =>
            {
                _hudVM.Refresh();
                _navigator.GoTo(RunGameState.Battle);
            };

            // 보상 선택 완료 → 다음 전투로
            _rewardVM.RewardClaimed += () =>
            {
                GameFlowSession.AdvanceToNextBattle();
                _hudVM.Refresh();
                StartNextBattle();
            };

            // 전투 결과
            if (_battleView != null)
            {
                _battleView.BattleWon  += OnBattleVictory;
                _battleView.BattleLost += OnBattleDefeat;
            }

            // 일시정지
            _hudVM.PauseRequested += OnPauseRequested;
        }

        // ── 전투 결과 처리 ───────────────────────────────────────────────

        private void OnBattleVictory()
        {
            // 매 wave 승리 시 보상 화면(3택)으로 이동.
            // 엘리트/보스는 RunRewardViewModel.IsBigReward로 '큰 보상'을 구분한다.
            _rewardVM.Refresh();
            _hudVM.Refresh();
            _navigator.GoTo(RunGameState.Reward);
        }

        private void OnBattleDefeat()
        {
            // 무한모드는 별도 GameOver 화면이 없으므로 패배 시 런을 처음부터 재시작한다.
            // (HP·전투번호·슬롯 풀·유물 선택 등 런 상태가 모두 초기화됨)
            GameFlowSession.StartNewRun();
            _hudVM.Refresh();
            _navigator.GoTo(RunGameState.StartRelicSelect);
        }

        // 같은 Battle 상태에 머무를 땐 Navigator.GoTo가 no-op이므로 View를 직접 재진입시킨다.
        private void StartNextBattle()
        {
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
            Debug.Log("[RunGameCompositionRoot] Pause 요청됨");
        }
    }

    // ── View Bind 인터페이스 ─────────────────────────────────────────────

    public interface IStartRelicSelectView : IRunGameView
    {
        void Bind(StartRelicSelectViewModel viewModel);
    }

    public interface IRunMapView : IRunGameView
    {
        void Bind(RunMapViewModel viewModel);
    }

    public interface IRunRewardView : IRunGameView
    {
        void Bind(RunRewardViewModel viewModel);
    }

    public interface IRunHUDView : IRunGameView
    {
        void Bind(RunHUDViewModel viewModel);
    }
}
