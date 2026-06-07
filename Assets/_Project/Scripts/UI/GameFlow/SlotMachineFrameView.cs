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
        public const float BaseFrameWidth = 268f;
        public const float BaseFrameHeight = 145f;
        public const float AnimationFrameWidth = 268f;
        public const float AnimationFrameHeight = 145f;

        private const string SlotMachineSpriteResourcePath = "Textures/UI/Ingame_Slot_ani";

        [SerializeField, FormerlySerializedAs("_slotMachineImage")] private Image _animationImage;
        [SerializeField] private Sprite[] _slotMachineSprites;
        [SerializeField] private float _cycleInterval = 0.08f;
        [SerializeField] private float _settleInterval = 0.08f;

        private Coroutine _activeRoutine;
        private int _animationVersion;

        public void Bind(Image animationImage, Sprite[] slotMachineSprites = null)
        {
            _animationImage = animationImage;
            _slotMachineSprites = slotMachineSprites;
            SetIdleImmediate();
        }

        public void PlaySpin()
        {
            EnsureReferences();
            EnsureSpritesLoaded();

            if (!isActiveAndEnabled || _animationImage == null || _slotMachineSprites.Length < 3)
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
            EnsureSpritesLoaded();
            StopActiveRoutine();

            int animationVersion = ++_animationVersion;
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
            EnsureSpritesLoaded();
            StopActiveRoutine();
            _animationVersion++;
            ApplyFrame(0);
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
            ApplyFrame(0);
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
            EnsureSpritesLoaded();
            ApplyFrame(0);
        }

        private IEnumerator PlaySpinRoutine(int animationVersion)
        {
            int frameIndex = 1;

            while (animationVersion == _animationVersion)
            {
                ApplyFrame(frameIndex);
                frameIndex = frameIndex == 1 ? 2 : 1;

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

        private void EnsureReferences()
        {
            if (IsValidAnimationImage(_animationImage))
            {
                ConfigureAnimationImage(_animationImage);
                return;
            }

            _animationImage = FindAnimationImage();
            if (_animationImage == null)
            {
                _animationImage = CreateAnimationImage();
            }

            ConfigureAnimationImage(_animationImage);
        }

        private void EnsureSpritesLoaded()
        {
            if (_slotMachineSprites != null && _slotMachineSprites.Length > 0)
            {
                return;
            }

            _slotMachineSprites = Resources.LoadAll<Sprite>(SlotMachineSpriteResourcePath);
            SortSpritesByFrameName(_slotMachineSprites);
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

        private bool IsValidAnimationImage(Image image)
        {
            return image != null && image.transform != transform;
        }

        private Image FindAnimationImage()
        {
            Transform existing = transform.Find(AnimationImageName);
            return existing != null ? existing.GetComponent<Image>() : null;
        }

        private Image CreateAnimationImage()
        {
            var gameObject = new GameObject(AnimationImageName, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(transform, false);
            return gameObject.GetComponent<Image>();
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
                rectTransform.SetAsFirstSibling();
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

        private static void SortSpritesByFrameName(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length <= 1)
            {
                return;
            }

            Array.Sort(sprites, (left, right) => GetFrameIndex(left).CompareTo(GetFrameIndex(right)));
        }

        private static int GetFrameIndex(Sprite sprite)
        {
            if (sprite == null || string.IsNullOrEmpty(sprite.name))
            {
                return int.MaxValue;
            }

            int separatorIndex = sprite.name.LastIndexOf('_');
            if (separatorIndex < 0 || separatorIndex >= sprite.name.Length - 1)
            {
                return int.MaxValue;
            }

            return int.TryParse(sprite.name.Substring(separatorIndex + 1), out int frameIndex)
                ? frameIndex
                : int.MaxValue;
        }
    }
}
