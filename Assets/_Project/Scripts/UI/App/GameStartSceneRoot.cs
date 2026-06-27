using Cysharp.Threading.Tasks;
using R3;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.Iap;
using SlotRogue.UI.Leaderboard;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace SlotRogue.UI.App
{
    /// <summary>
    /// GameStart(로비) 씬의 최상위 조립자입니다.
    /// ViewModel 생성 → View 바인딩 → 시작/종료 흐름 연결만 담당하고,
    /// 로비 우주 배경 애니메이션과 임시 디버그 버튼은 전용 헬퍼에 위임합니다(ADR-0020).
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class GameStartSceneRoot : MonoBehaviour
    {
        private static bool _openLeaderboardOnNextLoad;

        [SerializeField] private GameStartSceneController _view;
        [SerializeField] private LeaderboardView _leaderboardView;
        [SerializeField] private LeaderboardLoginView _loginView;
        [SerializeField] private Button _removeAdsButton;
        [SerializeField] private TMP_Text _removeAdsButtonText;
        [SerializeField] private CodelessIAPButton _removeAdsIapButton;
        [SerializeField] private IapFulfillmentHandler _iapFulfillmentHandler;

        private GameStartViewModel _viewModel;
        private LeaderboardViewModel _leaderboardViewModel;
        private readonly LobbySpaceBackground _lobbyBackground = new();
        private readonly LobbyDebugButtons _debugButtons = new();

        public static void RequestOpenLeaderboardOnNextLoad()
        {
            _openLeaderboardOnNextLoad = true;
        }

        private void Awake()
        {
            IapStoreConnectionCallbacks.Register();

            _viewModel = new GameStartViewModel();
            _leaderboardViewModel = new LeaderboardViewModel();

            if (_view == null)
            {
                _view = GetComponent<GameStartSceneController>();
            }

            EnsureLeaderboardView();
            EnsureLoginView();
            _lobbyBackground.Bind(gameObject.scene);
            ConfigureRemoveAdsButton();
            _debugButtons.Install(
                ResolveCanvas(),
                OnDebugTutorialStart,
                OnDebugTutorialSkip,
                OnDebugTutorialReset,
                OnDebugAdsReset);
            AdsRemoveState.Changed += HandleAdsRemoveChanged;

            if (_view != null)
            {
                _view.StartRequested += _viewModel.RequestStartGame;
                _view.QuitRequested += _viewModel.RequestQuitGame;
            }

            _viewModel.StartGameRequested += StartGame;
            _viewModel.QuitGameRequested += QuitGame;

            if (_leaderboardView != null)
            {
                // 상태 구독·close/refresh 입력은 View.Bind가 소유한다(ADR-0020).
                // launcher의 OpenRequested만 씬 진입 경로로 SceneRoot가 연결한다.
                _leaderboardView.OpenRequested += HandleLeaderboardOpenRequested;
                _leaderboardView.Bind(_leaderboardViewModel);
            }

            if (_loginView != null)
            {
                // 로그인 View는 같은 leaderboard 상태를 렌더한다(View 전용 ViewModel이 아니라
                // SceneRoot가 구독을 소유). 구독 즉시 현재 값이 1회 흘러 초기 Render를 처리한다.
                _loginView.Initialize();
                _loginView.ProfileSubmitted += HandlePlayerProfileSubmitted;
                _leaderboardViewModel.State.Subscribe(_loginView.Render).AddTo(this);
            }
        }

        private void Start()
        {
            if (_openLeaderboardOnNextLoad)
            {
                _openLeaderboardOnNextLoad = false;
                _leaderboardViewModel?.OpenAsync().Forget();
                return;
            }

            _leaderboardViewModel?.EvaluateProfileRequirement();
        }

        private void Update()
        {
            _lobbyBackground.Tick(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            AdsRemoveState.Changed -= HandleAdsRemoveChanged;
            _debugButtons.Dispose();

            if (_view != null)
            {
                _view.StartRequested -= _viewModel.RequestStartGame;
                _view.QuitRequested -= _viewModel.RequestQuitGame;
            }

            if (_viewModel != null)
            {
                _viewModel.StartGameRequested -= StartGame;
                _viewModel.QuitGameRequested -= QuitGame;
            }

            // Leaderboard close/refresh와 상태 구독은 View.Bind(.AddTo)가 소유해 자동 해제된다.
            // SceneRoot가 직접 연결한 OpenRequested만 여기서 해제한다(ADR-0020).
            if (_leaderboardView != null)
            {
                _leaderboardView.OpenRequested -= HandleLeaderboardOpenRequested;
            }

            if (_loginView != null)
            {
                _loginView.ProfileSubmitted -= HandlePlayerProfileSubmitted;
            }
        }

        private void HandleLeaderboardOpenRequested()
        {
            _leaderboardViewModel.OpenAsync().Forget();
        }

        private void HandlePlayerProfileSubmitted(string playerName)
        {
            _leaderboardViewModel.SaveProfileAsync(playerName).Forget();
        }

        private void EnsureLeaderboardView()
        {
            if (_leaderboardView != null)
            {
                _leaderboardView.gameObject.SetActive(true);
                _leaderboardView.EnsureRuntimeLayout();
                return;
            }

            GameObject[] roots = gameObject.scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                _leaderboardView =
                    roots[index].GetComponentInChildren<LeaderboardView>(includeInactive: true);
                if (_leaderboardView != null)
                {
                    _leaderboardView.gameObject.SetActive(true);
                    _leaderboardView.EnsureRuntimeLayout();
                    return;
                }
            }

            Debug.LogError("10_LeaderboardArea with LeaderboardView was not found in GameStart scene.");
        }

        private void EnsureLoginView()
        {
            if (_loginView != null)
            {
                return;
            }

            GameObject[] roots = gameObject.scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Transform[] descendants =
                    roots[rootIndex].GetComponentsInChildren<Transform>(
                        includeInactive: true);
                for (int index = 0; index < descendants.Length; index++)
                {
                    Transform descendant = descendants[index];
                    if (descendant.name != "20_LogInArea")
                    {
                        continue;
                    }

                    _loginView =
                        descendant.GetComponent<LeaderboardLoginView>();
                    if (_loginView == null)
                    {
                        Debug.LogError(
                            "20_LogInArea requires LeaderboardLoginView.");
                    }

                    return;
                }
            }

            Debug.LogError("20_LogInArea was not found in GameStart scene.");
        }

        private Canvas ResolveCanvas()
        {
            if (_view != null)
            {
                Canvas viewCanvas = _view.GetComponentInParent<Canvas>();
                if (viewCanvas != null)
                {
                    return viewCanvas;
                }
            }

            GameObject[] roots = gameObject.scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                Canvas canvas = roots[index].GetComponentInChildren<Canvas>(
                    includeInactive: true);
                if (canvas != null)
                {
                    return canvas;
                }
            }

            return null;
        }

        // ── 임시 디버그 버튼 동작 (UI 생성/배선은 LobbyDebugButtons가 담당) ──

        private static void OnDebugTutorialStart()
        {
            GameFlowSession.StartTutorialRun();
            GameSceneLoader.LoadRunGame();
        }

        private static void OnDebugTutorialSkip()
        {
            FirstRunTutorialState.MarkCompleted();
            GameFlowSession.StartNewRun();
            GameSceneLoader.LoadRunGame();
        }

        private void OnDebugTutorialReset()
        {
            FirstRunTutorialState.ResetForDebug();
            GameLog.Info("[GameStartSceneRoot] First-run tutorial flag reset.");
        }

        private void OnDebugAdsReset()
        {
            AdsRemoveState.ResetForDebug();
            RenderRemoveAdsButton(AdsRemoveState.IsRemoved);
            GameLog.Info("[GameStartSceneRoot] Remove Ads local cache reset.");
        }

        private void ConfigureRemoveAdsButton()
        {
            if (_removeAdsButton == null ||
                _removeAdsButtonText == null ||
                _removeAdsIapButton == null ||
                _iapFulfillmentHandler == null)
            {
                Debug.LogError(
                    "[GameStartSceneRoot] Remove Ads Button references are missing.");
                return;
            }

            _removeAdsIapButton.productId = AdsRemoveState.ProductId;
            _removeAdsIapButton.buttonType = CodelessButtonType.Purchase;
            _removeAdsIapButton.automaticallyConfirmTransaction = true;
            _removeAdsIapButton.button = _removeAdsButton;

            RenderRemoveAdsButton(AdsRemoveState.IsRemoved);
        }

        private void HandleAdsRemoveChanged(bool isRemoved)
        {
            RenderRemoveAdsButton(isRemoved);
        }

        private void RenderRemoveAdsButton(bool isRemoved)
        {
            if (_removeAdsButton != null)
            {
                _removeAdsButton.interactable = !isRemoved;
            }

            if (_removeAdsButtonText != null)
            {
                _removeAdsButtonText.text = isRemoved
                    ? "광고 제거 구매 완료"
                    : "광고 제거 구매";
            }
        }

        private void StartGame()
        {
            if (_leaderboardViewModel == null ||
                !_leaderboardViewModel.HasCompletedProfile)
            {
                _leaderboardViewModel?.RequireProfile();
                return;
            }

            if (!FirstRunTutorialState.IsCompleted)
            {
                GameFlowSession.StartTutorialRun();
            }
            else
            {
                // 이어하기는 Boot의 ResumePanel에서만 처리하고, 로비 Play는 새 런을 시작합니다.
                GameFlowSession.StartNewRun();
            }

            GameSceneLoader.LoadRunGame();
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
