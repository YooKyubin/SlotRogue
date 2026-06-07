using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class SlotLeverView : MonoBehaviour
    {
        private const string LeverSpriteResourcePath = "Textures/UI/Ingame_lever";

        private static readonly int[] DownFrameOrder = { 0, 1, 2, 3, 4 };
        private static readonly int[] UpFrameOrder = { 4, 5, 6, 7 };

        [SerializeField] private Image _leverImage;
        [SerializeField] private Sprite[] _leverSprites;
        [SerializeField] private float _frameInterval = 0.04f;

        public void Bind(Image leverImage, Sprite[] leverSprites = null)
        {
            _leverImage = leverImage;
            _leverSprites = leverSprites;
            SetUpImmediate();
        }

        public void PlayDown()
        {
            PlayFrames(DownFrameOrder);
        }

        public UniTask PlayDownAsync(CancellationToken cancellationToken = default)
        {
            return PlayFramesAsync(DownFrameOrder, cancellationToken);
        }

        public void PlayUp()
        {
            PlayFrames(UpFrameOrder);
        }

        public UniTask PlayUpAsync(CancellationToken cancellationToken = default)
        {
            return PlayFramesAsync(UpFrameOrder, cancellationToken);
        }

        public void SetUpImmediate()
        {
            EnsureSpritesLoaded();
            ApplyFrame(0);
        }

        private void OnDisable()
        {
            StopActiveRoutine();
        }

        private void PlayFrames(int[] frameOrder)
        {
            EnsureSpritesLoaded();

            if (!isActiveAndEnabled || frameOrder == null || frameOrder.Length == 0)
            {
                return;
            }

            StopActiveRoutine();
            int animationVersion = ++_animationVersion;
            _activeRoutine = StartCoroutine(PlayFrameRoutine(frameOrder, animationVersion));
        }

        private IEnumerator PlayFrameRoutine(int[] frameOrder, int animationVersion)
        {
            for (int index = 0; index < frameOrder.Length; index++)
            {
                if (animationVersion != _animationVersion)
                {
                    yield break;
                }

                ApplyFrame(frameOrder[index]);

                if (_frameInterval > 0f && index < frameOrder.Length - 1)
                {
                    yield return new WaitForSecondsRealtime(_frameInterval);
                }
            }

            if (animationVersion == _animationVersion)
            {
                _activeRoutine = null;
            }
        }

        private async UniTask PlayFramesAsync(int[] frameOrder, CancellationToken cancellationToken)
        {
            EnsureSpritesLoaded();

            if (!isActiveAndEnabled || frameOrder == null || frameOrder.Length == 0)
            {
                return;
            }

            StopActiveRoutine();
            int animationVersion = ++_animationVersion;

            for (int index = 0; index < frameOrder.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!isActiveAndEnabled || animationVersion != _animationVersion)
                {
                    return;
                }

                ApplyFrame(frameOrder[index]);

                if (_frameInterval > 0f && index < frameOrder.Length - 1)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_frameInterval),
                        DelayType.UnscaledDeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken);
                }
            }
        }

        private void ApplyFrame(int frameIndex)
        {
            if (_leverImage == null || _leverSprites == null || frameIndex < 0 || frameIndex >= _leverSprites.Length)
            {
                return;
            }

            _leverImage.sprite = _leverSprites[frameIndex];
            _leverImage.enabled = _leverSprites[frameIndex] != null;
            _leverImage.preserveAspect = true;
        }

        private void EnsureSpritesLoaded()
        {
            if (_leverSprites != null && _leverSprites.Length > 0)
            {
                return;
            }

            _leverSprites = Resources.LoadAll<Sprite>(LeverSpriteResourcePath);
            SortSpritesByFrameName(_leverSprites);
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

        private void StopActiveRoutine()
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }
        }

        private Coroutine _activeRoutine;
        private int _animationVersion;
    }
}
