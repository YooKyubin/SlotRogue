using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Relics.Pool;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.Iap;
using SlotRogue.UI.Leaderboard;
using TMPro;
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
        private static readonly string[] DefaultLoadingMessages =
        {
            "우주선 시동 거는 중...",
            "슬롯 엔진 예열 중...",
            "별가루 연료 주입 중...",
            "항로 계산 중...",
            "유물 신호 스캔 중...",
            "보급품 적재 중...",
            "도킹 해제 준비 중...",
            "외계 신호 수신 중..."
        };

        [Header("Loading")]
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private Slider _loadingSlider;
        [SerializeField] private Image _loadingFillImage;
        [SerializeField] private TMP_Text _loadingText;
        [SerializeField] private Text _legacyLoadingText;
        [SerializeField] private float _loadingMessageIntervalSeconds = 0.8f;
        [SerializeField]
        private string[] _loadingMessages =
        {
            "우주선 시동 거는 중...",
            "슬롯 엔진 예열 중...",
            "별가루 연료 주입 중...",
            "항로 계산 중...",
            "유물 신호 스캔 중...",
            "보급품 적재 중...",
            "도킹 해제 준비 중...",
            "외계 신호 수신 중..."
        };

        [Header("Resume")]
        [SerializeField] private GameObject _resumePanel;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _declineResumeButton;

        private CancellationTokenSource _loadingMessageCts;
        private RectTransform _loadingFillRect;
        private bool _hasLoadingFillRectMetrics;
        private float _loadingFillFullWidth;
        private float _loadingFillLeftEdge;
        private float _loadingFillScaleX = 1f;

        private void Awake()
        {
            IapStoreConnectionCallbacks.Register();
            EnsureLoadingReferences();
            ShowLoadingPanel();
            HideResumePanel();
            BindResumeButtons();
        }

        private void OnDestroy()
        {
            StopLoadingMessageLoop();
            UnbindResumeButtons();
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

                StartLoadingMessageLoop();
                SetLoadingProgress(0f);

                await BootAssetPreloader.PreloadAsync(
                    new Progress<float>(progress =>
                        SetLoadingProgress(Mathf.Lerp(0f, 0.8f, progress))));

                if (RunPersistenceStore.HasSaved && _resumePanel != null)
                {
                    StopLoadingMessageLoop();
                    HideLoadingPanel();
                    ShowResumePanel();
                    return;
                }

                SetLoadingMessage("도킹 게이트 여는 중...");
                await GameSceneLoader.LoadGameStartAsync(
                    new Progress<float>(progress =>
                        SetLoadingProgress(Mathf.Lerp(0.8f, 1f, progress))));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                GameSceneLoader.LoadGameStart();
            }
        }

        private void ShowResumePanel()
        {
            StopLoadingMessageLoop();
            HideLoadingPanel();
            _resumePanel.SetActive(true);

            if (_resumeButton == null && _declineResumeButton == null)
            {
                Debug.LogWarning("[BootController] Resume buttons must be wired in the inspector.");
                GameSceneLoader.LoadGameStart();
            }
        }

        private void HideResumePanel()
        {
            if (_resumePanel != null)
            {
                _resumePanel.SetActive(false);
            }
        }

        private void BindResumeButtons()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(OnResumeChosen);
                _resumeButton.onClick.AddListener(OnResumeChosen);
            }

            if (_declineResumeButton != null)
            {
                _declineResumeButton.onClick.RemoveListener(OnDeclineResume);
                _declineResumeButton.onClick.AddListener(OnDeclineResume);
            }
        }

        private void UnbindResumeButtons()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(OnResumeChosen);
            }

            if (_declineResumeButton != null)
            {
                _declineResumeButton.onClick.RemoveListener(OnDeclineResume);
            }
        }

        private void ShowLoadingPanel()
        {
            EnsureLoadingReferences();

            if (_loadingPanel != null)
            {
                _loadingPanel.SetActive(true);
            }

            SetLoadingProgress(0f);
            SetRandomLoadingMessage(-1);
        }

        private void EnsureLoadingReferences()
        {
            if (_loadingPanel != null)
            {
                _loadingSlider ??= _loadingPanel.GetComponentInChildren<Slider>(true);
                _loadingText ??= _loadingPanel.GetComponentInChildren<TMP_Text>(true);
                _legacyLoadingText ??= _loadingPanel.GetComponentInChildren<Text>(true);
            }

            if (_loadingSlider != null &&
                _loadingFillImage == null &&
                _loadingSlider.fillRect != null)
            {
                _loadingFillImage = _loadingSlider.fillRect.GetComponent<Image>();
            }

            CacheLoadingFillRectMetrics();
        }

        private void HideLoadingPanel()
        {
            if (_loadingPanel != null)
            {
                _loadingPanel.SetActive(false);
            }
        }

        private void SetLoadingProgress(float progress)
        {
            float normalizedProgress = Mathf.Clamp01(progress);

            if (_loadingSlider != null)
            {
                _loadingSlider.normalizedValue = normalizedProgress;
            }

            if (_loadingFillImage != null)
            {
                _loadingFillImage.fillAmount = normalizedProgress;
            }

            if (_loadingSlider == null)
            {
                SetLoadingFillRectProgress(normalizedProgress);
            }
        }

        private void SetLoadingFillRectProgress(float normalizedProgress)
        {
            bool fillAmountDrivesVisibleMesh =
                _loadingFillImage != null &&
                _loadingFillImage.type == Image.Type.Filled &&
                (_loadingFillImage.sprite != null || _loadingFillImage.overrideSprite != null);

            if (_loadingFillImage == null ||
                fillAmountDrivesVisibleMesh)
            {
                return;
            }

            CacheLoadingFillRectMetrics();
            if (!_hasLoadingFillRectMetrics || _loadingFillRect == null)
            {
                return;
            }

            float width = _loadingFillFullWidth * normalizedProgress;
            _loadingFillRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                width);

            // Sprite-less UI Images ignore fillAmount, so keep the original left edge fixed.
            Vector2 anchoredPosition = _loadingFillRect.anchoredPosition;
            anchoredPosition.x = _loadingFillLeftEdge +
                width * _loadingFillRect.pivot.x * _loadingFillScaleX;
            _loadingFillRect.anchoredPosition = anchoredPosition;
        }

        private void CacheLoadingFillRectMetrics()
        {
            if (_loadingFillImage == null)
            {
                return;
            }

            RectTransform fillRect = _loadingFillImage.rectTransform;
            if (fillRect == null)
            {
                return;
            }

            if (_hasLoadingFillRectMetrics && _loadingFillRect == fillRect)
            {
                return;
            }

            float width = Mathf.Abs(fillRect.rect.width);
            if (width <= Mathf.Epsilon)
            {
                width = Mathf.Abs(fillRect.sizeDelta.x);
            }

            if (width <= Mathf.Epsilon)
            {
                return;
            }

            _loadingFillRect = fillRect;
            _loadingFillFullWidth = width;
            _loadingFillScaleX = Mathf.Abs(fillRect.localScale.x);
            if (_loadingFillScaleX <= Mathf.Epsilon)
            {
                _loadingFillScaleX = 1f;
            }

            _loadingFillLeftEdge = fillRect.anchoredPosition.x -
                width * fillRect.pivot.x * _loadingFillScaleX;
            _hasLoadingFillRectMetrics = true;
        }

        private void SetLoadingMessage(string message)
        {
            if (_loadingText != null)
            {
                _loadingText.text = message ?? string.Empty;
            }

            if (_legacyLoadingText != null)
            {
                _legacyLoadingText.text = message ?? string.Empty;
            }
        }

        private void StartLoadingMessageLoop()
        {
            StopLoadingMessageLoop();
            _loadingMessageCts = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy());
            CycleLoadingMessagesAsync(_loadingMessageCts.Token).Forget();
        }

        private void StopLoadingMessageLoop()
        {
            if (_loadingMessageCts == null)
            {
                return;
            }

            _loadingMessageCts.Cancel();
            _loadingMessageCts.Dispose();
            _loadingMessageCts = null;
        }

        private async UniTaskVoid CycleLoadingMessagesAsync(CancellationToken cancellationToken)
        {
            int lastMessageIndex = -1;
            TimeSpan interval = TimeSpan.FromSeconds(
                Mathf.Max(0.2f, _loadingMessageIntervalSeconds));

            while (!cancellationToken.IsCancellationRequested)
            {
                lastMessageIndex = SetRandomLoadingMessage(lastMessageIndex);

                try
                {
                    await UniTask.Delay(
                        interval,
                        DelayType.UnscaledDeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private int SetRandomLoadingMessage(int lastMessageIndex)
        {
            string[] messages = HasLoadingMessages(_loadingMessages)
                ? _loadingMessages
                : DefaultLoadingMessages;

            if (!HasLoadingMessages(messages))
            {
                SetLoadingMessage(string.Empty);
                return -1;
            }

            int messageIndex = UnityEngine.Random.Range(0, messages.Length);
            if (messages.Length > 1 && messageIndex == lastMessageIndex)
            {
                messageIndex = (messageIndex + 1) % messages.Length;
            }

            SetLoadingMessage(messages[messageIndex]);
            return messageIndex;
        }

        private static bool HasLoadingMessages(string[] messages)
        {
            return messages != null && messages.Length > 0;
        }

        private void OnResumeChosen()
        {
            if (RunPersistenceService.TryResume())
            {
                GameSceneLoader.LoadRunGame();
            }
            else
            {
                GameSceneLoader.LoadGameStart();
            }
        }

        private void OnDeclineResume()
        {
            RunPersistenceStore.Clear();
            GameSceneLoader.LoadGameStart();
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

        internal static async UniTask PreloadAsync(IProgress<float> progress = null)
        {
            if (_completed)
            {
                progress?.Report(1f);
                return;
            }

            progress?.Report(0f);
            await InitializeAddressablesAsync(progress, 0f, 0.25f);
            await LoadDefaultAddressablesAsync(progress, 0.25f, 0.75f);
            await LoadPresentationSpritesAsync(progress, 0.75f, 1f);
            _completed = true;
            progress?.Report(1f);
        }

        private static async UniTask InitializeAddressablesAsync(
            IProgress<float> progress,
            float startProgress,
            float endProgress)
        {
            if (!_hasInitializeHandle)
            {
                _initializeHandle = Addressables.InitializeAsync();
                _hasInitializeHandle = true;
            }

            while (_initializeHandle.IsValid() && !_initializeHandle.IsDone)
            {
                ReportOperationProgress(progress, _initializeHandle, startProgress, endProgress);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            progress?.Report(endProgress);

            if (!_initializeHandle.IsValid() ||
                _initializeHandle.Status == AsyncOperationStatus.Succeeded)
            {
                return;
            }

            Debug.LogWarning(
                $"[BootAssetPreloader] Addressables initialization failed: {FailureReason(_initializeHandle)}");
        }

        private static async UniTask LoadDefaultAddressablesAsync(
            IProgress<float> progress,
            float startProgress,
            float endProgress)
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
                ReportOperationProgress(progress, _defaultAssetsHandle, startProgress, endProgress);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            progress?.Report(endProgress);

            if (!_defaultAssetsHandle.IsValid() ||
                _defaultAssetsHandle.Status == AsyncOperationStatus.Succeeded)
            {
                return;
            }

            Debug.LogWarning(
                $"[BootAssetPreloader] Default Addressables preload failed: {FailureReason(_defaultAssetsHandle)}");
        }

        private static async UniTask LoadPresentationSpritesAsync(
            IProgress<float> progress,
            float startProgress,
            float endProgress)
        {
            IReadOnlyList<string> keys = BuildPresentationSpriteKeys();
            progress?.Report(startProgress);
            IProgress<float> spriteProgress = progress == null
                ? null
                : new Progress<float>(value =>
                    progress.Report(Mathf.Lerp(startProgress, endProgress, value)));

            await AddressableSpriteCache.PreloadAsync(
                keys,
                CancellationToken.None,
                spriteProgress);
            progress?.Report(endProgress);
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

        private static void ReportOperationProgress(
            IProgress<float> progress,
            AsyncOperationHandle handle,
            float startProgress,
            float endProgress)
        {
            if (progress == null)
            {
                return;
            }

            progress.Report(Mathf.Lerp(
                startProgress,
                endProgress,
                Mathf.Clamp01(handle.PercentComplete)));
        }
    }
}
