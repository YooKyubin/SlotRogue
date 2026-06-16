using System;
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

        [Header("Battle Flow")]
        [FormerlySerializedAs("_battleFlowController")]
        [SerializeField, AutoWire("BattleSceneCompositionRoot")]
        private BattleSceneCompositionRoot _battleSceneCompositionRoot;

        private StartRelicSelectViewModel _startRelicSelectVM;
        private RunRewardViewModel        _rewardVM;
        private RunHUDViewModel           _hudVM;
        private RunDefeatViewModel        _defeatVM;
        private LeaderboardViewModel       _leaderboardVM;
        private RelicIconRenderer          _relicIconRenderer;
        private CancellationTokenSource     _defeatCountdownCts;
        private bool                        _reviveAdPending;
        private bool                        _resumeBattleOnEnter;

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

        protected virtual void Start()
        {
            _navigator.GoTo(RunGameState.StartRelicSelect);
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
                _leaderboardView.PlayerProfileSubmitted += HandlePlayerProfileSubmitted;
                _leaderboardVM.Changed += _leaderboardView.Render;
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
                _leaderboardView.PlayerProfileSubmitted -= HandlePlayerProfileSubmitted;
                _leaderboardVM.Changed -= _leaderboardView.Render;
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

        // ── 전투 결과 처리 ───────────────────────────────────────────────

        private void OnBattleVictory()
        {
            // 매 wave 승리 시 보상 화면(3택)으로 이동.
            RefreshHud();
            _navigator.GoTo(RunGameState.Reward);
        }

        private void OnBattleDefeat()
        {
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
            FinalizePendingDefeat();
            GameFlowSession.StartNewRun();
            RefreshHud();
            _navigator.GoTo(RunGameState.StartRelicSelect);
        }

        private void HandleRankingRequested()
        {
            FinalizePendingDefeat();
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

        private void HandlePlayerProfileSubmitted(string playerName)
        {
            _leaderboardVM.SaveProfileAsync(playerName).Forget();
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
                Debug.LogWarning(
                    "[RunGameSceneRoot] DefeatView host is missing. Creating the runtime fallback.");

                var hostObject = new GameObject("DefeatView", typeof(RectTransform));
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
                Canvas canvas = _defeatView != null
                    ? _defeatView.GetComponentInParent<Canvas>()
                    : null;
                canvas ??= searchRoot.GetComponentInChildren<Canvas>(includeInactive: true);
                if (canvas != null)
                {
                    _leaderboardView = LeaderboardView.CreateRuntime(canvas.transform);
                }
            }

            if (_leaderboardView != null)
            {
                _leaderboardView.EnsureRuntimeLayout();
                _leaderboardView.SetLauncherVisible(false);
            }
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
                GameFlowSession.BuildRelicContributionSummary());
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
}
