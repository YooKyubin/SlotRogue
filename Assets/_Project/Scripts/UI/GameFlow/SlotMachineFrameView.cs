using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class SlotMachineFrameView : MonoBehaviour
    {
        public const string AnimationImageName = "Slot Machine Animation";
        public const string ReelFrameRootName = "Slot Machine Reel Frame Animation";
        public const float BaseFrameWidth = 268f;
        public const float BaseFrameHeight = 145f;
        public const float AnimationFrameWidth = 268f;
        public const float AnimationFrameHeight = 145f;

        private const int ReelCount = 5;
        private const int IdleFrameIndex = 0;
        private const int FirstSpinFrameIndex = 1;
        private const int SecondSpinFrameIndex = 2;

        [SerializeField, FormerlySerializedAs("_slotMachineImage")] private Image _animationImage;
        [SerializeField] private Sprite[] _slotMachineSprites;
        [SerializeField] private Sprite[] _leftReelSprites;
        [SerializeField] private Sprite[] _middleReelSprites;
        [SerializeField] private Sprite[] _rightReelSprites;
        [SerializeField] private float _cycleInterval = 0.08f;
        [SerializeField] private float _settleInterval = 0.08f;

        private readonly Image[] _reelFrameImages = new Image[ReelCount];
        private readonly bool[] _reelSpinning = new bool[ReelCount];
        private Coroutine _activeRoutine;
        private RectTransform _reelFrameRoot;
        private Vector2 _lastConfiguredReelFrameSize;
        private int _animationVersion;
        private bool _reportedMissingAnimationImage;

        public void Bind(Image animationImage, Sprite[] slotMachineSprites = null)
        {
            _animationImage = animationImage;

            if (slotMachineSprites != null && slotMachineSprites.Length > 0)
            {
                _slotMachineSprites = slotMachineSprites;
            }

            SetIdleImmediate();
        }

        public void PlaySpin()
        {
            EnsureReferences();

            if (UsesSplitReelFrames())
            {
                StopActiveRoutine();
                SetAllReelsSpinning();
                int splitAnimationVersion = ++_animationVersion;
                _activeRoutine = StartCoroutine(PlaySplitReelSpinRoutine(splitAnimationVersion));
                return;
            }

            if (!isActiveAndEnabled ||
                _animationImage == null ||
                _slotMachineSprites == null ||
                _slotMachineSprites.Length < 3)
            {
                return;
            }

            StopActiveRoutine();
            int animationVersion = ++_animationVersion;
            _activeRoutine = StartCoroutine(PlaySpinRoutine(animationVersion));
        }

        public async UniTask StopAtIdleAsync(CancellationToken cancellationToken = default)
        {
            EnsureReferences();
            StopActiveRoutine();

            int animationVersion = ++_animationVersion;

            if (UsesSplitReelFrames())
            {
                ApplyFrameToSpinningReels(FirstSpinFrameIndex);

                if (_settleInterval > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_settleInterval),
                        DelayType.UnscaledDeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken);
                }

                if (animationVersion == _animationVersion)
                {
                    SetAllReelsIdle();
                }

                return;
            }

            ApplyFrame(1);

            if (_settleInterval > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_settleInterval),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken);
            }

            if (animationVersion == _animationVersion)
            {
                ApplyFrame(0);
            }
        }

        public void SetIdleImmediate()
        {
            EnsureReferences();
            StopActiveRoutine();
            _animationVersion++;

            if (UsesSplitReelFrames())
            {
                SetAllReelsIdle();
                return;
            }

            ApplyFrame(IdleFrameIndex);
        }

        public void SetReelIdle(int reelIndex)
        {
            EnsureReferences();

            if (!UsesSplitReelFrames() || reelIndex < 0 || reelIndex >= ReelCount)
            {
                return;
            }

            _reelSpinning[reelIndex] = false;
            ApplyReelFrame(reelIndex, IdleFrameIndex);
        }

        public static Vector2 ResolveAnimationImageSize(Vector2 containerSize)
        {
            if (containerSize.x <= 0f || containerSize.y <= 0f)
            {
                return new Vector2(AnimationFrameWidth, AnimationFrameHeight);
            }

            float baseAspect = BaseFrameWidth / BaseFrameHeight;
            float containerAspect = containerSize.x / containerSize.y;
            Vector2 displayedBaseSize = containerAspect > baseAspect
                ? new Vector2(containerSize.y * baseAspect, containerSize.y)
                : new Vector2(containerSize.x, containerSize.x / baseAspect);

            return new Vector2(
                displayedBaseSize.x * (AnimationFrameWidth / BaseFrameWidth),
                displayedBaseSize.y * (AnimationFrameHeight / BaseFrameHeight));
        }

        private void OnDisable()
        {
            StopActiveRoutine();
            _animationVersion++;

            if (UsesSplitReelFrames())
            {
                SetAllReelsIdle();
                return;
            }

            ApplyFrame(IdleFrameIndex);
        }

        private void Reset()
        {
            SetIdleImmediate();
        }

        private void OnValidate()
        {
            if (_animationImage == null || _animationImage.transform == transform)
            {
                return;
            }

            ConfigureAnimationImage(_animationImage);
            ApplyFrame(0);
        }

        private void LateUpdate()
        {
            if (!UsesSplitReelFrames() || _reelFrameRoot == null)
            {
                return;
            }

            Vector2 currentSize = ResolveRectSize(_reelFrameRoot);
            if (!HasPositiveSize(currentSize) ||
                (currentSize - _lastConfiguredReelFrameSize).sqrMagnitude < 0.01f)
            {
                return;
            }

            for (int reelIndex = 0; reelIndex < ReelCount; reelIndex++)
            {
                ConfigureReelFrameImage(reelIndex, _reelFrameImages[reelIndex]);
            }
        }

        private IEnumerator PlaySpinRoutine(int animationVersion)
        {
            int frameIndex = FirstSpinFrameIndex;

            while (animationVersion == _animationVersion)
            {
                ApplyFrame(frameIndex);
                frameIndex = frameIndex == FirstSpinFrameIndex ? SecondSpinFrameIndex : FirstSpinFrameIndex;

                if (_cycleInterval > 0f)
                {
                    yield return new WaitForSecondsRealtime(_cycleInterval);
                }
                else
                {
                    yield return null;
                }
            }
        }

        private IEnumerator PlaySplitReelSpinRoutine(int animationVersion)
        {
            int frameIndex = FirstSpinFrameIndex;

            while (animationVersion == _animationVersion && AnyReelSpinning())
            {
                ApplyFrameToSpinningReels(frameIndex);
                frameIndex = frameIndex == FirstSpinFrameIndex ? SecondSpinFrameIndex : FirstSpinFrameIndex;

                if (_cycleInterval > 0f)
                {
                    yield return new WaitForSecondsRealtime(_cycleInterval);
                }
                else
                {
                    yield return null;
                }
            }

            if (animationVersion == _animationVersion)
            {
                _activeRoutine = null;
            }
        }

        private void EnsureReferences()
        {
            if (UsesSplitReelFrames())
            {
                EnsureSplitReelFrames();
                SetLegacyAnimationVisible(false);
                return;
            }

            SetSplitReelFramesVisible(false);

            if (IsValidAnimationImage(_animationImage))
            {
                ConfigureAnimationImage(_animationImage);
                SetLegacyAnimationVisible(true);
                return;
            }

            _animationImage = FindAnimationImage();
            if (_animationImage == null)
            {
                if (!_reportedMissingAnimationImage)
                {
                    Debug.LogError(
                        "[SlotMachineFrameView] Slot Machine Animation must be placed in the hierarchy when split reel frames are not assigned.");
                    _reportedMissingAnimationImage = true;
                }

                return;
            }

            ConfigureAnimationImage(_animationImage);
            SetLegacyAnimationVisible(true);
        }

        private void ApplyFrame(int frameIndex)
        {
            if (_animationImage == null ||
                _slotMachineSprites == null ||
                frameIndex < 0 ||
                frameIndex >= _slotMachineSprites.Length)
            {
                return;
            }

            Sprite sprite = _slotMachineSprites[frameIndex];
            _animationImage.sprite = sprite;
            _animationImage.enabled = sprite != null;
            _animationImage.preserveAspect = false;
        }

        private void SetAllReelsSpinning()
        {
            for (int reelIndex = 0; reelIndex < ReelCount; reelIndex++)
            {
                _reelSpinning[reelIndex] = true;
            }

            ApplyFrameToSpinningReels(FirstSpinFrameIndex);
        }

        private void SetAllReelsIdle()
        {
            for (int reelIndex = 0; reelIndex < ReelCount; reelIndex++)
            {
                _reelSpinning[reelIndex] = false;
                ApplyReelFrame(reelIndex, IdleFrameIndex);
            }
        }

        private bool AnyReelSpinning()
        {
            for (int reelIndex = 0; reelIndex < ReelCount; reelIndex++)
            {
                if (_reelSpinning[reelIndex])
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyFrameToSpinningReels(int frameIndex)
        {
            for (int reelIndex = 0; reelIndex < ReelCount; reelIndex++)
            {
                if (_reelSpinning[reelIndex])
                {
                    ApplyReelFrame(reelIndex, frameIndex);
                }
            }
        }

        private void ApplyReelFrame(int reelIndex, int frameIndex)
        {
            if (reelIndex < 0 || reelIndex >= ReelCount)
            {
                return;
            }

            Image image = _reelFrameImages[reelIndex];
            Sprite[] sprites = ResolveReelSprites(reelIndex);
            if (image == null ||
                sprites == null ||
                frameIndex < 0 ||
                frameIndex >= sprites.Length)
            {
                return;
            }

            Sprite sprite = sprites[frameIndex];
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = false;
        }

        private void EnsureSplitReelFrames()
        {
            RectTransform root = FindReelFrameRoot();
            if (root == null)
            {
                return;
            }

            _reelFrameRoot = root;
            ConfigureReelFrameRoot(_reelFrameRoot);

            for (int reelIndex = 0; reelIndex < ReelCount; reelIndex++)
            {
                Image image = FindReelFrameImage(reelIndex);
                if (image == null)
                {
                    continue;
                }

                _reelFrameImages[reelIndex] = image;
                ConfigureReelFrameImage(reelIndex, image);
            }

            SetSplitReelFramesVisible(true);
        }

        private bool UsesSplitReelFrames()
        {
            return HasThreeFrames(_leftReelSprites) &&
                HasThreeFrames(_middleReelSprites) &&
                HasThreeFrames(_rightReelSprites);
        }

        private static bool HasThreeFrames(Sprite[] sprites)
        {
            return sprites != null &&
                sprites.Length >= 3 &&
                sprites[IdleFrameIndex] != null &&
                sprites[FirstSpinFrameIndex] != null &&
                sprites[SecondSpinFrameIndex] != null;
        }

        private Sprite[] ResolveReelSprites(int reelIndex)
        {
            if (reelIndex == 0)
            {
                return _leftReelSprites;
            }

            return reelIndex == ReelCount - 1 ? _rightReelSprites : _middleReelSprites;
        }

        private RectTransform FindReelFrameRoot()
        {
            if (_reelFrameRoot != null)
            {
                return _reelFrameRoot;
            }

            Transform existing = transform.Find(ReelFrameRootName);
            return existing as RectTransform;
        }

        private Image FindReelFrameImage(int reelIndex)
        {
            if (_reelFrameImages[reelIndex] != null)
            {
                return _reelFrameImages[reelIndex];
            }

            Transform existing = _reelFrameRoot != null
                ? _reelFrameRoot.Find(ReelFrameImageName(reelIndex))
                : null;
            return existing != null ? existing.GetComponent<Image>() : null;
        }

        private void ConfigureReelFrameRoot(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            RectTransform reference = FindReelOverlay();
            if (reference != null)
            {
                root.anchorMin = reference.anchorMin;
                root.anchorMax = reference.anchorMax;
                root.pivot = reference.pivot;
                root.anchoredPosition = reference.anchoredPosition;
                root.sizeDelta = reference.sizeDelta;
                root.localScale = reference.localScale;
            }
            else
            {
                root.anchorMin = new Vector2(0.5f, 0.5f);
                root.anchorMax = new Vector2(0.5f, 0.5f);
                root.pivot = new Vector2(0.5f, 0.5f);
                root.anchoredPosition = Vector2.zero;
                root.sizeDelta = new Vector2(
                    ReelFrameNativeWidth(0) +
                    ReelFrameNativeWidth(1) * 3f +
                    ReelFrameNativeWidth(ReelCount - 1),
                    ReelFrameNativeHeight());
                root.localScale = Vector3.one;
            }

        }

        private void ConfigureReelFrameImage(int reelIndex, Image image)
        {
            if (image == null || image.transform is not RectTransform rectTransform)
            {
                return;
            }

            image.raycastTarget = false;
            image.preserveAspect = false;

            float totalNativeWidth =
                ReelFrameNativeWidth(0) +
                ReelFrameNativeWidth(1) * 3f +
                ReelFrameNativeWidth(ReelCount - 1);
            float nativeHeight = ReelFrameNativeHeight();
            float width = ReelFrameNativeWidth(reelIndex);

            float left = -totalNativeWidth * 0.5f;
            for (int index = 0; index < reelIndex; index++)
            {
                left += ReelFrameNativeWidth(index);
            }

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(left + width * 0.5f, 0f);
            rectTransform.sizeDelta = new Vector2(width, nativeHeight);
            rectTransform.localScale = Vector3.one;

            Vector2 rootSize = ResolveRectSize(_reelFrameRoot);
            if (!HasPositiveSize(rootSize))
            {
                rootSize = ResolveRectSize(FindReelOverlay());
            }

            if (!HasPositiveSize(rootSize))
            {
                rootSize = new Vector2(totalNativeWidth, nativeHeight);
            }

            _lastConfiguredReelFrameSize = rootSize;
        }

        private RectTransform FindReelOverlay()
        {
            Transform existing = transform.Find("Slot Reel Overlay");
            return existing as RectTransform;
        }

        private float ReelFrameNativeWidth(int reelIndex)
        {
            Sprite[] sprites = ResolveReelSprites(reelIndex);
            return sprites != null && sprites[IdleFrameIndex] != null
                ? sprites[IdleFrameIndex].rect.width
                : 1f;
        }

        private float ReelFrameNativeHeight()
        {
            return _middleReelSprites != null && _middleReelSprites[IdleFrameIndex] != null
                ? _middleReelSprites[IdleFrameIndex].rect.height
                : AnimationFrameHeight;
        }

        private static Vector2 ResolveRectSize(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return Vector2.zero;
            }

            Vector2 size = rectTransform.rect.size;
            if (HasPositiveSize(size))
            {
                return size;
            }

            size = rectTransform.sizeDelta;
            return HasPositiveSize(size) ? size : Vector2.zero;
        }

        private static bool HasPositiveSize(Vector2 size)
        {
            return size.x > 0f && size.y > 0f;
        }

        private static string ReelFrameImageName(int reelIndex)
        {
            return $"Slot Machine Reel Frame {reelIndex}";
        }

        private void SetLegacyAnimationVisible(bool visible)
        {
            if (_animationImage != null)
            {
                _animationImage.enabled = visible && _animationImage.sprite != null;
            }
        }

        private void SetSplitReelFramesVisible(bool visible)
        {
            if (_reelFrameRoot != null)
            {
                _reelFrameRoot.gameObject.SetActive(visible);
            }
        }

        private bool IsValidAnimationImage(Image image)
        {
            return image != null && image.transform != transform;
        }

        private Image FindAnimationImage()
        {
            Transform existing = transform.Find(AnimationImageName);
            return existing != null ? existing.GetComponent<Image>() : null;
        }

        private static void ConfigureAnimationImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.raycastTarget = false;
            image.preserveAspect = false;

            if (image.transform is RectTransform rectTransform)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = ResolveAnimationImageSize(ResolveParentSize(rectTransform));
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.localScale = Vector3.one;
            }
        }

        private static Vector2 ResolveParentSize(RectTransform rectTransform)
        {
            if (rectTransform.parent is not RectTransform parent)
            {
                return new Vector2(BaseFrameWidth, BaseFrameHeight);
            }

            Vector2 size = parent.rect.size;
            if (size.x > 0f && size.y > 0f)
            {
                return size;
            }

            size = parent.sizeDelta;
            return size.x > 0f && size.y > 0f
                ? size
                : new Vector2(BaseFrameWidth, BaseFrameHeight);
        }

        private void StopActiveRoutine()
        {
            if (_activeRoutine == null)
            {
                return;
            }

            StopCoroutine(_activeRoutine);
            _activeRoutine = null;
        }

    }
}
