using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Relics.Pool;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.RunGame.ViewModels;
using UnityEngine;
using UnityEngine.Serialization;

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
        public static RunGameSceneRoot Instance { get; private set; }

        // ── Inspector 연결 ───────────────────────────────────────────────

        [Header("Navigator")]
        [SerializeField] private RunGameNavigator _navigator;

        [Header("Game Views  (IRunGameView 구현체)")]
        [SerializeField] private StartArtifactSelectionView _startRelicSelectView;
        [SerializeField] private BattleView                 _battleView;
        [SerializeField] private RunRewardView              _rewardView;
        [SerializeField] private RunDefeatView              _defeatView;

        [Header("HUD  (항상 표시)")]
        [SerializeField] private RunHUDView _hudView;

        [Header("Battle Flow")]
        [FormerlySerializedAs("_battleFlowController")]
        [SerializeField] private BattleSceneCompositionRoot _battleSceneCompositionRoot;

        private StartRelicSelectViewModel _startRelicSelectVM;
        private RunRewardViewModel        _rewardVM;
        private RunHUDViewModel           _hudVM;
        private RunDefeatViewModel        _defeatVM;
        private AddressableSpriteProvider _relicIconProvider;
        private CancellationTokenSource   _iconLoadCts;
        private int _startRelicIconRenderVersion;
        private int _rewardIconRenderVersion;

        // ── 초기화 ──────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _iconLoadCts = new CancellationTokenSource();
            _relicIconProvider = new AddressableSpriteProvider(RelicIconKeys.Default);

            // 런 보장은 ViewModel 생성·View 바인딩·Navigator.GoTo 이전에 실행되어야 합니다.
            // BootScene → GameStart → RunGame 경로에서는 GameStart에서 이미 새 런을 시작했으므로
            // 여기서는 그대로 이어가고, RunGame 단독 Play 경로에서는 여기서 새 런을 시작합니다.
            EnsureRunStarted();

            CreateViewModels();
            EnsureDefeatView();
            BindViews();
            RegisterViews();
            SubscribeEvents();
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

        protected virtual void Start()
        {
            _navigator.GoTo(RunGameState.StartRelicSelect);
        }

        protected virtual void OnDestroy()
        {
            _startRelicIconRenderVersion++;
            _rewardIconRenderVersion++;
            _iconLoadCts?.Cancel();
            UnsubscribeEvents();
            _relicIconProvider?.Dispose();
            _relicIconProvider = null;
            _iconLoadCts?.Dispose();
            _iconLoadCts = null;

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
            _defeatVM           = new RunDefeatViewModel();
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

            if (_defeatView != null)
            {
                _defeatView.Render(_defeatVM.State);
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
            _defeatVM.Changed += HandleDefeatStateChanged;
            _defeatVM.NewRunRequested += HandleNewRunRequested;

            if (_startRelicSelectView != null)
            {
                _startRelicSelectView.Entered += HandleStartRelicEntered;
                _startRelicSelectView.RelicSelectionRequested += HandleStarterRelicSelectionRequested;
            }

            if (_rewardView != null)
            {
                _rewardView.Entered += HandleRewardEntered;
                _rewardView.RewardSelectionRequested += HandleRewardSelectionRequested;
                _rewardView.RerollRequested += _rewardVM.RerollRewards;
                _rewardView.ExtraRewardRequested += _rewardVM.AddExtraReward;
            }

            if (_battleView != null)
            {
                _battleView.Entered += HandleBattleEntered;
            }

            if (_hudView != null)
            {
                _hudView.PauseRequested += _hudVM.RequestPause;
            }

            if (_defeatView != null)
            {
                _defeatView.NewRunRequested += _defeatVM.RequestNewRun;
            }

            if (_battleSceneCompositionRoot != null)
            {
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

            if (_defeatVM != null)
            {
                _defeatVM.Changed -= HandleDefeatStateChanged;
                _defeatVM.NewRunRequested -= HandleNewRunRequested;
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
                _rewardView.RerollRequested -= _rewardVM.RerollRewards;
                _rewardView.ExtraRewardRequested -= _rewardVM.AddExtraReward;
            }

            if (_battleView != null)
            {
                _battleView.Entered -= HandleBattleEntered;
            }

            if (_hudView != null)
            {
                _hudView.PauseRequested -= _hudVM.RequestPause;
            }

            if (_defeatView != null && _defeatVM != null)
            {
                _defeatView.NewRunRequested -= _defeatVM.RequestNewRun;
            }

            if (_battleSceneCompositionRoot != null)
            {
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
            RefreshHud();
            _navigator.GoTo(RunGameState.Battle);
        }

        private void HandleRewardEntered()
        {
            _rewardVM.Refresh();
        }

        private void HandleRewardSelectionRequested(int optionIndex)
        {
            _rewardVM.ClaimReward(optionIndex);
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
            StartNextBattle();
        }

        private void HandleHudStateChanged(RunHUDViewState state)
        {
            if (_hudView != null)
            {
                _hudView.Render(state);
            }
        }

        private void HandleBattleEntered()
        {
            if (_battleSceneCompositionRoot != null)
            {
                _battleSceneCompositionRoot.BeginBattle();
            }
        }

        // ── 전투 결과 처리 ───────────────────────────────────────────────

        private void OnBattleVictory()
        {
            // 매 wave 승리 시 보상 화면(3택)으로 이동.
            RefreshHud();
            _navigator.GoTo(RunGameState.Reward);
        }

        private void OnBattleDefeat()
        {
            _defeatVM.Refresh(
                GameFlowSession.CurrentBattleNumber,
                GameFlowSession.Victories,
                GameFlowSession.RewardsClaimed);
            RefreshHud();
            _navigator.GoTo(RunGameState.Defeat);
        }

        private void HandleDefeatStateChanged(RunDefeatViewState state)
        {
            if (_defeatView != null)
            {
                _defeatView.Render(state);
            }
        }

        private void HandleNewRunRequested()
        {
            GameFlowSession.StartNewRun();
            RefreshHud();
            _navigator.GoTo(RunGameState.StartRelicSelect);
        }

        private void EnsureDefeatView()
        {
            if (_defeatView != null)
            {
                _defeatView.EnsureRuntimeLayout();
                return;
            }

            Transform searchRoot = _navigator != null ? _navigator.transform.root : transform.root;
            Transform host = SceneComponentResolver.FindDeepChild(searchRoot, "GameOverView");
            if (host == null)
            {
                var hostObject = new GameObject("GameOverView", typeof(RectTransform));
                host = hostObject.transform;

                Transform parent = _rewardView != null
                    ? _rewardView.transform.parent
                    : transform.parent;
                host.SetParent(parent, false);

                if (host is RectTransform rectTransform)
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                }
            }

            _defeatView = host.GetComponent<RunDefeatView>();
            if (_defeatView == null)
            {
                _defeatView = host.gameObject.AddComponent<RunDefeatView>();
            }

            _defeatView.EnsureRuntimeLayout();
            host.gameObject.SetActive(false);
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
            Debug.Log("[RunGameSceneRoot] Pause requested.");
        }

        private void RefreshHud()
        {
            _hudVM.Refresh();
        }

        private void RenderStartRelicState(StartRelicSelectViewState state)
        {
            _startRelicSelectView.Render(state);

            int renderVersion = ++_startRelicIconRenderVersion;
            if (_iconLoadCts != null)
            {
                ApplyStartRelicIconsAsync(
                    state,
                    renderVersion,
                    _iconLoadCts.Token).Forget();
            }
        }

        private void RenderRewardState(RunRewardViewState state)
        {
            _rewardView.Render(state);

            int renderVersion = ++_rewardIconRenderVersion;
            if (_iconLoadCts != null)
            {
                ApplyRewardIconsAsync(
                    state,
                    renderVersion,
                    _iconLoadCts.Token).Forget();
            }
        }

        private async UniTask ApplyStartRelicIconsAsync(
            StartRelicSelectViewState state,
            int renderVersion,
            CancellationToken cancellationToken)
        {
            if (state == null || _relicIconProvider == null)
            {
                return;
            }

            try
            {
                GameFlowOptionView[] views = _startRelicSelectView.ArtifactOptions;
                int count = Mathf.Min(views?.Length ?? 0, state.Options.Count);

                for (int index = 0; index < count; index++)
                {
                    Sprite icon = await _relicIconProvider.LoadAsync(
                        state.Options[index].IconKey,
                        cancellationToken);

                    if (renderVersion != _startRelicIconRenderVersion)
                    {
                        return;
                    }

                    if (views[index] != null)
                    {
                        views[index].SetIcon(icon);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask ApplyRewardIconsAsync(
            RunRewardViewState state,
            int renderVersion,
            CancellationToken cancellationToken)
        {
            if (state == null || _relicIconProvider == null)
            {
                return;
            }

            try
            {
                GameFlowOptionView[] views = _rewardView.RewardOptions;
                int count = Mathf.Min(views?.Length ?? 0, state.Options.Count);

                for (int index = 0; index < count; index++)
                {
                    if (cancellationToken.IsCancellationRequested ||
                        renderVersion != _rewardIconRenderVersion)
                    {
                        return;
                    }

                    string iconKey = state.Options[index].IconKey;
                    if (string.IsNullOrEmpty(iconKey))
                    {
                        if (views[index] != null)
                        {
                            views[index].SetIcon(null);
                        }

                        continue;
                    }

                    Sprite icon = await _relicIconProvider.LoadAsync(
                        iconKey,
                        cancellationToken);

                    if (renderVersion != _rewardIconRenderVersion)
                    {
                        return;
                    }

                    if (views[index] != null)
                    {
                        views[index].SetIcon(icon);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
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
