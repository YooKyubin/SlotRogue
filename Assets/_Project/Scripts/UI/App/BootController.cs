using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Relics.Pool;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.Iap;
using SlotRogue.UI.Leaderboard;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace SlotRogue.UI.App
{
    [DefaultExecutionOrder(-10000)]
    public sealed class BootController : MonoBehaviour
    {
        private BootLoadingScreen _loadingScreen;

        private void Awake()
        {
            IapStoreConnectionCallbacks.Register();
            _loadingScreen = BootLoadingScreen.Show("Loading...");
        }

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            try
            {
                AdsRemoveState.Initialize();
                SlotRogueLeaderboardService.InitializeAsync().Forget();

                _loadingScreen?.SetMessage("Loading assets...");
                await BootAssetPreloader.PreloadAsync();

                _loadingScreen?.SetMessage("Starting...");
                await GameSceneLoader.LoadGameStartAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                GameSceneLoader.LoadGameStart();
            }

            if (_loadingScreen != null)
            {
                await _loadingScreen.HideAfterSceneReadyAsync();
            }
        }
    }

    internal static class BootAssetPreloader
    {
        private const string DefaultAddressablesLabel = "default";

        private static AsyncOperationHandle<IResourceLocator> _initializeHandle;
        private static AsyncOperationHandle<IList<UnityEngine.Object>> _defaultAssetsHandle;
        private static bool _hasInitializeHandle;
        private static bool _hasDefaultAssetsHandle;
        private static bool _completed;

        internal static async UniTask PreloadAsync()
        {
            if (_completed)
            {
                return;
            }

            await InitializeAddressablesAsync();
            await LoadDefaultAddressablesAsync();
            await LoadPresentationSpritesAsync();
            _completed = true;
        }

        private static async UniTask InitializeAddressablesAsync()
        {
            if (!_hasInitializeHandle)
            {
                _initializeHandle = Addressables.InitializeAsync();
                _hasInitializeHandle = true;
            }

            while (_initializeHandle.IsValid() && !_initializeHandle.IsDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (!_initializeHandle.IsValid() ||
                _initializeHandle.Status == AsyncOperationStatus.Succeeded)
            {
                return;
            }

            Debug.LogWarning(
                $"[BootAssetPreloader] Addressables initialization failed: {FailureReason(_initializeHandle)}");
        }

        private static async UniTask LoadDefaultAddressablesAsync()
        {
            if (!_hasDefaultAssetsHandle)
            {
                _defaultAssetsHandle = Addressables.LoadAssetsAsync<UnityEngine.Object>(
                    DefaultAddressablesLabel,
                    _ => { });
                _hasDefaultAssetsHandle = true;
            }

            while (_defaultAssetsHandle.IsValid() && !_defaultAssetsHandle.IsDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (!_defaultAssetsHandle.IsValid() ||
                _defaultAssetsHandle.Status == AsyncOperationStatus.Succeeded)
            {
                return;
            }

            Debug.LogWarning(
                $"[BootAssetPreloader] Default Addressables preload failed: {FailureReason(_defaultAssetsHandle)}");
        }

        private static async UniTask LoadPresentationSpritesAsync()
        {
            IReadOnlyList<string> keys = BuildPresentationSpriteKeys();
            await AddressableSpriteCache.PreloadAsync(keys, CancellationToken.None);
        }

        private static IReadOnlyList<string> BuildPresentationSpriteKeys()
        {
            var keys = new List<string>();

            AddRange(keys, RelicIconKeys.All);
            foreach (RelicDefinition relic in RelicCatalog.All)
            {
                AddUnique(keys, relic.IconKey);
            }

            AddRange(keys, SlotSymbolIconKeys.HighlightSpriteKeys);
            AddRange(keys, SlotSymbolIconKeys.NormalSpriteKeys);
            AddRange(keys, SlotSymbolIconKeys.AnimationSpriteKeys);
            AddUnique(keys, RewardModifierIconKeys.AddOne);
            AddUnique(keys, RewardModifierIconKeys.RemoveOne);

            return keys;
        }

        private static void AddRange(List<string> keys, IReadOnlyList<string> source)
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                AddUnique(keys, source[index]);
            }
        }

        private static void AddUnique(List<string> keys, string key)
        {
            if (string.IsNullOrEmpty(key) || keys.Contains(key))
            {
                return;
            }

            keys.Add(key);
        }

        private static string FailureReason(AsyncOperationHandle handle)
        {
            return handle.OperationException != null
                ? handle.OperationException.Message
                : "unknown error";
        }
    }

    internal sealed class BootLoadingScreen : MonoBehaviour
    {
        private const int SortingOrder = 32767;
        private const float FadeDuration = 0.2f;

        private static BootLoadingScreen _active;

        private CanvasGroup _canvasGroup;
        private Text _messageText;

        internal static BootLoadingScreen Show(string message)
        {
            if (_active != null)
            {
                _active.SetMessage(message);
                return _active;
            }

            var root = new GameObject("Boot Loading Screen");
            DontDestroyOnLoad(root);

            BootLoadingScreen screen = root.AddComponent<BootLoadingScreen>();
            screen.Build(message);
            _active = screen;
            return screen;
        }

        internal void SetMessage(string message)
        {
            if (_messageText != null)
            {
                _messageText.text = message ?? string.Empty;
            }
        }

        internal async UniTask HideAfterSceneReadyAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.Update);
            await UniTask.Yield(PlayerLoopTiming.Update);
            await FadeOutAsync();

            if (_active == this)
            {
                _active = null;
            }

            Destroy(gameObject);
        }

        private void Build(string message)
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;

            Image background = CreateImage("Background", transform);
            background.color = Color.black;
            Stretch(background.rectTransform);

            _messageText = CreateText("Loading Text", transform);
            _messageText.alignment = TextAnchor.MiddleCenter;
            _messageText.color = new Color(0.88f, 0.9f, 0.94f, 1f);
            _messageText.font = ResolveDefaultFont();
            _messageText.fontSize = 34;
            _messageText.raycastTarget = false;
            _messageText.text = message ?? string.Empty;

            RectTransform textRect = _messageText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.offsetMin = new Vector2(96f, 120f);
            textRect.offsetMax = new Vector2(-96f, 220f);
        }

        private async UniTask FadeOutAsync()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / FadeDuration);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            if (_active == this)
            {
                _active = null;
            }
        }

        private static Image CreateImage(string name, Transform parent)
        {
            var imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.raycastTarget = true;
            return image;
        }

        private static Text CreateText(string name, Transform parent)
        {
            var textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(parent, false);
            return textObject.GetComponent<Text>();
        }

        private static Font ResolveDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null
                ? font
                : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
