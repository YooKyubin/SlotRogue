using System;
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
    [DefaultExecutionOrder(-10000)]
    public sealed class GameStartSceneRoot : MonoBehaviour
    {
        private static readonly Color TemporaryDebugButtonColor =
            new(0.14f, 0.14f, 0.18f, 0.92f);
        private static readonly Color TemporaryDebugButtonTextColor =
            new(1f, 0.82f, 0.38f, 1f);
        private const string LobbyPlanetLayerName = "Animated Planet Layer";
        private const string LobbyPlanetNamePrefix = "Floating Planet ";
        private const float LobbyPlanetScale = 4f;
        private static bool _openLeaderboardOnNextLoad;

        private static readonly LobbyPlanetPreset[] LobbyPlanetPresets =
        {
            new(new Vector2(-300f, 58f), -10.0f, 16f, 10f, 0.34f, 0.2f, 2.0f, 0.72f),
            new(new Vector2(-86f, -46f), -7.5f, 12f, 8f, 0.48f, 1.5f, -6.0f, 0.82f),
            new(new Vector2(148f, 38f), -5.5f, 14f, 9f, 0.40f, 2.7f, 4.0f, 0.72f),
            new(new Vector2(340f, -22f), -4.0f, 18f, 11f, 0.30f, 4.0f, -1.5f, 0.65f),
        };

        [SerializeField] private GameStartSceneController _view;
        [SerializeField] private LeaderboardView _leaderboardView;
        [SerializeField] private LeaderboardLoginView _loginView;
        [SerializeField] private Button _removeAdsButton;
        [SerializeField] private TMP_Text _removeAdsButtonText;
        [SerializeField] private CodelessIAPButton _removeAdsIapButton;
        [SerializeField] private IapFulfillmentHandler _iapFulfillmentHandler;

        private GameStartViewModel _viewModel;
        private LeaderboardViewModel _leaderboardViewModel;
        private Button _temporaryTutorialStartButton;
        private Button _temporaryTutorialSkipButton;
        private Button _temporaryTutorialResetButton;
        private Button _temporaryAdsResetButton;
        private RectTransform _lobbyPlanetLayer;
        private LobbyPlanetInstance[] _lobbyPlanetInstances = Array.Empty<LobbyPlanetInstance>();
        private float _lobbySpaceElapsed;

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
            BindLobbySpaceBackground();
            ConfigureRemoveAdsButton();
            EnsureTemporaryResetButtons();
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
            AnimateLobbySpaceBackground();
        }

        private void OnDestroy()
        {
            AdsRemoveState.Changed -= HandleAdsRemoveChanged;
            UnsubscribeTemporaryResetButtons();

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

        private void BindLobbySpaceBackground()
        {
            _lobbyPlanetLayer = FindSceneChild(LobbyPlanetLayerName) as RectTransform;
            if (_lobbyPlanetLayer == null)
            {
                Debug.LogWarning(
                    "[GameStartSceneRoot] Animated Planet Layer must be placed in the lobby scene hierarchy.");
                _lobbyPlanetInstances = Array.Empty<LobbyPlanetInstance>();
                return;
            }

            LobbyPlanetInstance[] instances = new LobbyPlanetInstance[LobbyPlanetPresets.Length];
            int count = 0;
            for (int index = 0; index < LobbyPlanetPresets.Length; index++)
            {
                string objectName = $"{LobbyPlanetNamePrefix}{index + 1:00}";
                RectTransform planet = _lobbyPlanetLayer.Find(objectName) as RectTransform;
                if (planet == null)
                {
                    Debug.LogWarning(
                        $"[GameStartSceneRoot] {objectName} must be placed under {LobbyPlanetLayerName}.");
                    continue;
                }

                LobbyPlanetPreset preset = LobbyPlanetPresets[index];
                ConfigureLobbyPlanet(planet, preset);
                instances[count] = new LobbyPlanetInstance(
                    planet,
                    preset,
                    preset.Phase * 13f);
                count++;
            }

            _lobbyPlanetInstances = new LobbyPlanetInstance[count];
            Array.Copy(instances, _lobbyPlanetInstances, count);
            _lobbySpaceElapsed = 0f;
        }

        private void AnimateLobbySpaceBackground()
        {
            if (_lobbyPlanetInstances == null || _lobbyPlanetInstances.Length == 0)
            {
                return;
            }

            _lobbySpaceElapsed += Time.unscaledDeltaTime;
            Vector2 layerSize = ResolveLobbyPlanetLayerSize();
            float wrapWidth = layerSize.x + 192f;

            for (int index = 0; index < _lobbyPlanetInstances.Length; index++)
            {
                LobbyPlanetInstance planet = _lobbyPlanetInstances[index];
                if (planet.Rect == null)
                {
                    continue;
                }

                LobbyPlanetPreset preset = planet.Preset;
                float wrappedX = Wrap(
                    preset.StartPosition.x + (preset.DriftSpeed * _lobbySpaceElapsed),
                    -wrapWidth * 0.5f,
                    wrapWidth * 0.5f);
                float bobX = Mathf.Sin(
                    (_lobbySpaceElapsed * preset.BobFrequency * 0.73f) + preset.Phase) *
                    preset.BobX;
                float bobY = Mathf.Sin(
                    (_lobbySpaceElapsed * preset.BobFrequency) + preset.Phase) *
                    preset.BobY;

                planet.Rect.anchoredPosition =
                    new Vector2(wrappedX + bobX, preset.StartPosition.y + bobY);
                planet.Rect.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        planet.InitialRotation + (preset.RotationSpeed * _lobbySpaceElapsed));
            }
        }

        private Vector2 ResolveLobbyPlanetLayerSize()
        {
            if (_lobbyPlanetLayer == null)
            {
                return new Vector2(820f, 260f);
            }

            Rect rect = _lobbyPlanetLayer.rect;
            return rect.size.sqrMagnitude > 0f ? rect.size : new Vector2(820f, 260f);
        }

        private static void ConfigureLobbyPlanet(
            RectTransform planet,
            LobbyPlanetPreset preset)
        {
            planet.anchorMin = new Vector2(0.5f, 0.5f);
            planet.anchorMax = new Vector2(0.5f, 0.5f);
            planet.pivot = new Vector2(0.5f, 0.5f);
            planet.anchoredPosition = preset.StartPosition;
            planet.localScale =
                new Vector3(LobbyPlanetScale, LobbyPlanetScale, LobbyPlanetScale);

            Image image = planet.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.raycastTarget = false;
            image.preserveAspect = true;
            image.color = new Color(image.color.r, image.color.g, image.color.b, preset.Alpha);
            if (image.sprite != null)
            {
                planet.sizeDelta = image.sprite.rect.size;
            }
        }

        private Transform FindSceneChild(string objectName)
        {
            GameObject[] roots = gameObject.scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                Transform found = FindDeepChild(roots[index].transform, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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

        private static float Wrap(float value, float min, float max)
        {
            float length = max - min;
            if (length <= 0f)
            {
                return value;
            }

            return min + Mathf.Repeat(value - min, length);
        }

        private void EnsureTemporaryResetButtons()
        {
            Canvas canvas = ResolveCanvas();
            if (canvas == null)
            {
                Debug.LogWarning(
                    "[GameStartSceneRoot] Temporary reset buttons require a Canvas.");
                return;
            }

            Transform existingHost = canvas.transform.Find("Temporary Reset Buttons");
            RectTransform host = existingHost as RectTransform;
            if (host == null)
            {
                host = CreateRect("Temporary Reset Buttons", canvas.transform);
                host.anchorMin = new Vector2(0.04f, 0.03f);
                host.anchorMax = new Vector2(0.86f, 0.14f);
                host.offsetMin = Vector2.zero;
                host.offsetMax = Vector2.zero;
            }

            _temporaryTutorialStartButton = EnsureTemporaryButton(
                host,
                "Temporary Tutorial Start Button",
                "튜토리얼 시작",
                new Vector2(0f, 0f),
                new Vector2(0.235f, 1f));
            _temporaryTutorialSkipButton = EnsureTemporaryButton(
                host,
                "Temporary Tutorial Skip Button",
                "튜토리얼 스킵",
                new Vector2(0.255f, 0f),
                new Vector2(0.49f, 1f));
            _temporaryTutorialResetButton = EnsureTemporaryButton(
                host,
                "Temporary Tutorial Reset Button",
                "튜토리얼 초기화",
                new Vector2(0.51f, 0f),
                new Vector2(0.745f, 1f));
            _temporaryAdsResetButton = EnsureTemporaryButton(
                host,
                "Temporary Ads Reset Button",
                "광고 구매 초기화",
                new Vector2(0.765f, 0f),
                new Vector2(1f, 1f));

            SubscribeTemporaryResetButtons();
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

        private static Button EnsureTemporaryButton(
            RectTransform parent,
            string objectName,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            Transform existing = parent.Find(objectName);
            RectTransform buttonRect = existing as RectTransform;
            if (buttonRect == null)
            {
                buttonRect = CreateRect(objectName, parent);
            }

            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            Image image = buttonRect.GetComponent<Image>();
            if (image == null)
            {
                image = buttonRect.gameObject.AddComponent<Image>();
            }

            image.color = TemporaryDebugButtonColor;
            image.raycastTarget = true;

            Button button = buttonRect.GetComponent<Button>();
            if (button == null)
            {
                button = buttonRect.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = image;

            Text text = buttonRect.GetComponentInChildren<Text>(includeInactive: true);
            if (text == null)
            {
                RectTransform textRect = CreateRect($"{objectName} Text", buttonRect);
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8f, 0f);
                textRect.offsetMax = new Vector2(-8f, 0f);
                text = textRect.gameObject.AddComponent<Text>();
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
            }

            Font font = Resources.Load<Font>("Galmuri11-Bold");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            text.font = font;
            text.fontSize = 24;
            text.color = TemporaryDebugButtonTextColor;
            text.text = label;

            return button;
        }

        private void SubscribeTemporaryResetButtons()
        {
            UnsubscribeTemporaryResetButtons();

            if (_temporaryTutorialStartButton != null)
            {
                _temporaryTutorialStartButton.onClick.AddListener(
                    HandleTemporaryTutorialStartClicked);
            }

            if (_temporaryTutorialSkipButton != null)
            {
                _temporaryTutorialSkipButton.onClick.AddListener(
                    HandleTemporaryTutorialSkipClicked);
            }

            if (_temporaryTutorialResetButton != null)
            {
                _temporaryTutorialResetButton.onClick.AddListener(
                    HandleTemporaryTutorialResetClicked);
            }

            if (_temporaryAdsResetButton != null)
            {
                _temporaryAdsResetButton.onClick.AddListener(
                    HandleTemporaryAdsResetClicked);
            }
        }

        private void UnsubscribeTemporaryResetButtons()
        {
            _temporaryTutorialStartButton?.onClick.RemoveListener(
                HandleTemporaryTutorialStartClicked);
            _temporaryTutorialSkipButton?.onClick.RemoveListener(
                HandleTemporaryTutorialSkipClicked);
            _temporaryTutorialResetButton?.onClick.RemoveListener(
                HandleTemporaryTutorialResetClicked);
            _temporaryAdsResetButton?.onClick.RemoveListener(
                HandleTemporaryAdsResetClicked);
        }

        private static void HandleTemporaryTutorialStartClicked()
        {
            GameFlowSession.StartTutorialRun();
            GameSceneLoader.LoadRunGame();
        }

        private static void HandleTemporaryTutorialSkipClicked()
        {
            FirstRunTutorialState.MarkCompleted();
            GameFlowSession.StartNewRun();
            GameSceneLoader.LoadRunGame();
        }

        private void HandleTemporaryTutorialResetClicked()
        {
            FirstRunTutorialState.ResetForDebug();
            Debug.Log("[GameStartSceneRoot] First-run tutorial flag reset.");
        }

        private void HandleTemporaryAdsResetClicked()
        {
            AdsRemoveState.ResetForDebug();
            RenderRemoveAdsButton(AdsRemoveState.IsRemoved);
            Debug.Log("[GameStartSceneRoot] Remove Ads local cache reset.");
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

            if (FirstRunTutorialState.IsCompleted)
            {
                GameFlowSession.StartNewRun();
            }
            else
            {
                GameFlowSession.StartTutorialRun();
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

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private readonly struct LobbyPlanetPreset
        {
            internal LobbyPlanetPreset(
                Vector2 startPosition,
                float driftSpeed,
                float bobX,
                float bobY,
                float bobFrequency,
                float phase,
                float rotationSpeed,
                float alpha)
            {
                StartPosition = startPosition;
                DriftSpeed = driftSpeed;
                BobX = bobX;
                BobY = bobY;
                BobFrequency = bobFrequency;
                Phase = phase;
                RotationSpeed = rotationSpeed;
                Alpha = alpha;
            }

            internal Vector2 StartPosition { get; }

            internal float DriftSpeed { get; }

            internal float BobX { get; }

            internal float BobY { get; }

            internal float BobFrequency { get; }

            internal float Phase { get; }

            internal float RotationSpeed { get; }

            internal float Alpha { get; }
        }

        private readonly struct LobbyPlanetInstance
        {
            internal LobbyPlanetInstance(
                RectTransform rect,
                LobbyPlanetPreset preset,
                float initialRotation)
            {
                Rect = rect;
                Preset = preset;
                InitialRotation = initialRotation;
            }

            internal RectTransform Rect { get; }

            internal LobbyPlanetPreset Preset { get; }

            internal float InitialRotation { get; }
        }
    }
}
