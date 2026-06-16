using Cysharp.Threading.Tasks;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.Iap;
using SlotRogue.UI.Leaderboard;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace SlotRogue.UI.App
{
    [DefaultExecutionOrder(-10000)]
    public sealed class GameStartSceneRoot : MonoBehaviour
    {
        [SerializeField] private GameStartSceneController _view;
        [SerializeField] private LeaderboardView _leaderboardView;
        [SerializeField] private LeaderboardLoginView _loginView;
        [SerializeField] private Button _removeAdsButton;
        [SerializeField] private TMP_Text _removeAdsButtonText;
        [SerializeField] private CodelessIAPButton _removeAdsIapButton;
        [SerializeField] private IapFulfillmentHandler _iapFulfillmentHandler;

        private GameStartViewModel _viewModel;
        private LeaderboardViewModel _leaderboardViewModel;

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
            ConfigureRemoveAdsButton();
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
                _leaderboardView.OpenRequested += HandleLeaderboardOpenRequested;
                _leaderboardView.CloseRequested += _leaderboardViewModel.Close;
                _leaderboardView.RefreshRequested += HandleLeaderboardRefreshRequested;
                _leaderboardView.PlayerProfileSubmitted += HandlePlayerProfileSubmitted;
                _leaderboardViewModel.Changed += _leaderboardView.Render;
                _leaderboardView.Render(_leaderboardViewModel.State);
            }

            if (_loginView != null)
            {
                _loginView.Initialize();
                _loginView.ProfileSubmitted += HandlePlayerProfileSubmitted;
                _leaderboardViewModel.Changed += _loginView.Render;
                _loginView.Render(_leaderboardViewModel.State);
            }
        }

        private void Start()
        {
            _leaderboardViewModel?.EvaluateProfileRequirement();
        }

        private void OnDestroy()
        {
            AdsRemoveState.Changed -= HandleAdsRemoveChanged;

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

            if (_leaderboardView != null && _leaderboardViewModel != null)
            {
                _leaderboardView.OpenRequested -= HandleLeaderboardOpenRequested;
                _leaderboardView.CloseRequested -= _leaderboardViewModel.Close;
                _leaderboardView.RefreshRequested -= HandleLeaderboardRefreshRequested;
                _leaderboardView.PlayerProfileSubmitted -= HandlePlayerProfileSubmitted;
                _leaderboardViewModel.Changed -= _leaderboardView.Render;
            }

            if (_loginView != null && _leaderboardViewModel != null)
            {
                _loginView.ProfileSubmitted -= HandlePlayerProfileSubmitted;
                _leaderboardViewModel.Changed -= _loginView.Render;
            }
        }

        private void HandleLeaderboardOpenRequested()
        {
            _leaderboardViewModel.OpenAsync().Forget();
        }

        private void HandleLeaderboardRefreshRequested()
        {
            _leaderboardViewModel.RefreshAsync().Forget();
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

            Canvas canvas = null;
            for (int index = 0; index < roots.Length && canvas == null; index++)
            {
                canvas = roots[index].GetComponentInChildren<Canvas>(includeInactive: true);
            }

            if (canvas != null)
            {
                _leaderboardView = LeaderboardView.CreateRuntime(canvas.transform);
                _leaderboardView.gameObject.SetActive(true);
            }
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

            GameFlowSession.StartNewRun();
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
