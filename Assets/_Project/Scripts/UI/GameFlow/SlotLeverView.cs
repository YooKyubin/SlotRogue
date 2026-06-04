using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class SlotLeverView : MonoBehaviour
    {
        private const string LeverSpriteResourcePath = "Textures/Ingame_lever";

        private static readonly int[] DownFrameOrder = { 0, 1, 2, 6, 7 };
        private static readonly int[] UpFrameOrder = { 7, 6, 2, 1, 0 };

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

        public void PlayUp()
        {
            PlayFrames(UpFrameOrder);
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
            _activeRoutine = StartCoroutine(PlayFrameRoutine(frameOrder));
        }

        private IEnumerator PlayFrameRoutine(int[] frameOrder)
        {
            for (int index = 0; index < frameOrder.Length; index++)
            {
                ApplyFrame(frameOrder[index]);

                if (_frameInterval > 0f && index < frameOrder.Length - 1)
                {
                    yield return new WaitForSecondsRealtime(_frameInterval);
                }
            }

            _activeRoutine = null;
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
    }
}
