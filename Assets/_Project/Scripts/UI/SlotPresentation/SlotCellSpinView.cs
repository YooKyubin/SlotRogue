using System;
using System.Collections;
using DG.Tweening;
using SlotRogue.Slot.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class SlotCellSpinView : MonoBehaviour
    {
        private const string SpinSpriteResourcePath = "Textures/icon_slot_ani";

        [SerializeField] private Image[] _cellIcons;
        [SerializeField] private Sprite[] _symbolSprites;
        [SerializeField] private Sprite[] _spinSymbolSprites;

        [Header("Timing")]
        [SerializeField] private float _spinDuration = 1.2f;
        [SerializeField] private float _cycleInterval = 0.045f;
        [SerializeField] private float _stopIntervalPerColumn = 0.12f;

        [Header("Lock Effect")]
        [SerializeField] private float _iconSize = 32f;
        [SerializeField] private float _spinningIconSize = 34f;
        [SerializeField] private float _spinPulseScale = 1.04f;
        [SerializeField] private float _lockPunchScale = 0.18f;
        [SerializeField] private float _lockPunchDuration = 0.22f;

        public void Bind(Image[] cellIcons, Sprite[] symbolSprites)
        {
            Bind(cellIcons, symbolSprites, null);
        }

        public void Bind(Image[] cellIcons, Sprite[] symbolSprites, Sprite[] spinSymbolSprites)
        {
            _cellIcons = cellIcons;
            _symbolSprites = symbolSprites;
            _spinSymbolSprites = spinSymbolSprites;
        }

        public void StopImmediate(SlotSpinResult spinResult = null)
        {
            KillTweens();

            if (spinResult != null)
            {
                SetAllIconsImmediate(spinResult);
            }
        }

        public IEnumerator Play(SlotSpinResult spinResult, Func<bool> shouldSkip)
        {
            if (!CanPlay(spinResult))
            {
                yield break;
            }

            StopImmediate();

            if (IsSkipped(shouldSkip))
            {
                SetAllIconsImmediate(spinResult);
                yield break;
            }

            EnsureLockedCells();
            ClearLockedCells();
            StartCycleTween();
            Tween spinDelay = DOVirtual.DelayedCall(_spinDuration, NoOp).SetUpdate(true);
            yield return WaitForTweenOrSkip(spinDelay, shouldSkip);

            if (IsSkipped(shouldSkip))
            {
                StopCycleTween();
                SetAllIconsImmediate(spinResult);
                yield break;
            }

            _lockSequence = DOTween.Sequence().SetUpdate(true);

            for (int column = 0; column < SlotSpinResult.Columns; column++)
            {
                int capturedColumn = column;
                _lockSequence.AppendCallback(() => LockColumn(capturedColumn, spinResult));
                _lockSequence.AppendInterval(_stopIntervalPerColumn);
            }

            yield return WaitForTweenOrSkip(_lockSequence, shouldSkip);
            StopCycleTween();

            if (IsSkipped(shouldSkip))
            {
                SetAllIconsImmediate(spinResult);
            }
        }

        private void OnDisable()
        {
            KillTweens();
        }

        private bool CanPlay(SlotSpinResult spinResult)
        {
            return _cellIcons != null &&
                _symbolSprites != null &&
                _symbolSprites.Length > 0 &&
                spinResult != null;
        }

        private void StartCycleTween()
        {
            StopCycleTween();
            CycleAllIconsOnce();

            _cycleTween = DOTween.Sequence()
                .AppendInterval(_cycleInterval)
                .AppendCallback(CycleAllIconsOnce)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);
        }

        private void StopCycleTween()
        {
            if (_cycleTween != null && _cycleTween.IsActive())
            {
                _cycleTween.Kill();
            }

            _cycleTween = null;
        }

        private void CycleAllIconsOnce()
        {
            Sprite[] spinSprites = GetSpinSprites();

            for (int index = 0; index < _cellIcons.Length; index++)
            {
                if (_cellIcons[index] == null || IsLocked(index))
                {
                    continue;
                }

                int spriteIndex = UnityEngine.Random.Range(0, spinSprites.Length);
                ApplySpinningIcon(index, spinSprites[spriteIndex]);
            }
        }

        private void LockColumn(int column, SlotSpinResult spinResult)
        {
            for (int row = 0; row < SlotSpinResult.Rows; row++)
            {
                int index = SlotSpinResult.ToIndex(column, row);

                if (index >= _cellIcons.Length || _cellIcons[index] == null)
                {
                    continue;
                }

                SetLocked(index, true);
                SetIcon(index, spinResult.GetSymbol(column, row));

                Transform iconTransform = _cellIcons[index].transform;
                iconTransform.DOKill();
                iconTransform.localScale = Vector3.one;
                iconTransform
                    .DOPunchScale(Vector3.one * _lockPunchScale, _lockPunchDuration, 5, 0.5f)
                    .SetUpdate(true);
            }
        }

        private void SetAllIconsImmediate(SlotSpinResult spinResult)
        {
            if (_cellIcons == null || spinResult == null)
            {
                return;
            }

            for (int column = 0; column < SlotSpinResult.Columns; column++)
            {
                for (int row = 0; row < SlotSpinResult.Rows; row++)
                {
                    int index = SlotSpinResult.ToIndex(column, row);

                    if (index < _cellIcons.Length)
                    {
                        SetIcon(index, spinResult.GetSymbol(column, row));
                    }
                }
            }
        }

        private void SetIcon(int index, SlotSymbolType symbol)
        {
            int spriteIndex = (int)symbol;

            if (_symbolSprites == null || spriteIndex < 0 || spriteIndex >= _symbolSprites.Length)
            {
                return;
            }

            ApplyIcon(index, _symbolSprites[spriteIndex]);
        }

        private void ApplyIcon(int index, Sprite sprite)
        {
            if (_cellIcons == null || index < 0 || index >= _cellIcons.Length || _cellIcons[index] == null)
            {
                return;
            }

            _cellIcons[index].sprite = sprite;
            _cellIcons[index].enabled = sprite != null;
            _cellIcons[index].preserveAspect = true;

            RectTransform rectTransform = _cellIcons[index].rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(_iconSize, _iconSize);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        private void ApplySpinningIcon(int index, Sprite sprite)
        {
            if (_cellIcons == null || index < 0 || index >= _cellIcons.Length || _cellIcons[index] == null)
            {
                return;
            }

            Image icon = _cellIcons[index];
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.preserveAspect = true;

            RectTransform rectTransform = icon.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(_spinningIconSize, _spinningIconSize);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = new Vector3(_spinPulseScale, _spinPulseScale, 1f);
        }

        private IEnumerator WaitForTweenOrSkip(Tween tween, Func<bool> shouldSkip)
        {
            if (tween == null)
            {
                yield break;
            }

            while (tween.IsActive() && !tween.IsComplete() && !IsSkipped(shouldSkip))
            {
                yield return null;
            }

            if (tween.IsActive() && !tween.IsComplete())
            {
                tween.Kill();
            }
        }

        private static bool IsSkipped(Func<bool> shouldSkip)
        {
            return shouldSkip != null && shouldSkip();
        }

        private void KillTweens()
        {
            StopCycleTween();

            if (_lockSequence != null && _lockSequence.IsActive())
            {
                _lockSequence.Kill();
            }

            _lockSequence = null;

            if (_cellIcons == null)
            {
                return;
            }

            for (int index = 0; index < _cellIcons.Length; index++)
            {
                if (_cellIcons[index] == null)
                {
                    continue;
                }

                Transform iconTransform = _cellIcons[index].transform;
                iconTransform.DOKill();
                iconTransform.localScale = Vector3.one;

                RectTransform rectTransform = _cellIcons[index].rectTransform;
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localRotation = Quaternion.identity;
            }
        }

        private Sprite[] GetSpinSprites()
        {
            if (_spinSymbolSprites == null || _spinSymbolSprites.Length == 0)
            {
                _spinSymbolSprites = Resources.LoadAll<Sprite>(SpinSpriteResourcePath);
                SortSprites(_spinSymbolSprites);
            }

            return _spinSymbolSprites != null && _spinSymbolSprites.Length > 0
                ? _spinSymbolSprites
                : _symbolSprites;
        }

        private void EnsureLockedCells()
        {
            if (_lockedCells == null || _lockedCells.Length != SlotSpinResult.CellCount)
            {
                _lockedCells = new bool[SlotSpinResult.CellCount];
            }
        }

        private void ClearLockedCells()
        {
            if (_lockedCells == null)
            {
                return;
            }

            for (int index = 0; index < _lockedCells.Length; index++)
            {
                _lockedCells[index] = false;
            }
        }

        private bool IsLocked(int index)
        {
            return _lockedCells != null &&
                index >= 0 &&
                index < _lockedCells.Length &&
                _lockedCells[index];
        }

        private void SetLocked(int index, bool locked)
        {
            EnsureLockedCells();

            if (index >= 0 && index < _lockedCells.Length)
            {
                _lockedCells[index] = locked;
            }
        }

        private static void SortSprites(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length <= 1)
            {
                return;
            }

            Array.Sort(
                sprites,
                (left, right) =>
                {
                    int yCompare = right.rect.y.CompareTo(left.rect.y);
                    return yCompare != 0 ? yCompare : left.rect.x.CompareTo(right.rect.x);
                });
        }

        private static void NoOp()
        {
        }

        private Tween _cycleTween;
        private Sequence _lockSequence;
        private bool[] _lockedCells;
    }
}
