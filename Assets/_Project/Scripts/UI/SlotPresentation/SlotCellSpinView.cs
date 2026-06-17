using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using SlotRogue.Slot.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class SlotCellSpinView : MonoBehaviour
    {
        private const float NativeSizeMultiplier = 1.25f;

        [SerializeField] private Image[] _cellIcons;
        [SerializeField] private Sprite[] _symbolSprites;
        [SerializeField] private Sprite[] _spinSymbolSprites;

        [Header("Timing")]
        [SerializeField] private float _spinDuration = 1.2f;
        [SerializeField] private float _cycleInterval = 0.045f;
        [SerializeField] private float _stopIntervalPerColumn = 0.12f;

        [Header("Lock Effect")]
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
            if (spinSymbolSprites != null && spinSymbolSprites.Length > 0)
            {
                _spinSymbolSprites = spinSymbolSprites;
            }
        }

        public void StopImmediate(SlotSpinResult spinResult = null)
        {
            EnsureReferences();
            KillTweens();

            if (spinResult != null)
            {
                SetAllIconsImmediate(spinResult);
            }
        }

        public bool EnsureReferences()
        {
            EnsureCellIcons();
            return HasUsableCellIcons();
        }

        /// <summary>
        /// Exposes the bound cell icons and sprite tables so the reel-based presenter can reuse the
        /// same wiring without duplicating serialized references in the prefab.
        /// </summary>
        public bool TryGetReelBindings(out Image[] cellIcons, out Sprite[] symbolSprites, out Sprite[] spinSprites)
        {
            EnsureReferences();
            cellIcons = _cellIcons;
            symbolSprites = _symbolSprites;
            spinSprites = GetSpinSprites();

            // Cells are optional: when the reels are authored in the scene they are the slot face and
            // the legacy 5x3 cell grid can be removed. Only the symbol sprite table is required.
            return _symbolSprites != null && _symbolSprites.Length > 0;
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
                if (column < SlotSpinResult.Columns - 1)
                {
                    _lockSequence.AppendInterval(_stopIntervalPerColumn);
                }
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
            EnsureReferences();

            return HasUsableCellIcons() &&
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
            EnsureReferences();

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
            ApplyNativeSize(_cellIcons[index]);
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
            ApplyNativeSize(icon);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = new Vector3(_spinPulseScale, _spinPulseScale, 1f);
        }

        private static void ApplyNativeSize(Image icon)
        {
            if (icon == null || icon.sprite == null)
            {
                return;
            }

            icon.SetNativeSize();
            icon.rectTransform.sizeDelta *= NativeSizeMultiplier;
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

        private static void NoOp()
        {
        }

        private void EnsureCellIcons()
        {
            if (HasUsableCellIcons())
            {
                return;
            }

            Image[] foundIcons = FindSlotCellIconsInScene();
            if (foundIcons.Length == SlotSpinResult.CellCount)
            {
                _cellIcons = foundIcons;
            }
        }

        private bool HasUsableCellIcons()
        {
            if (_cellIcons == null || _cellIcons.Length < SlotSpinResult.CellCount)
            {
                return false;
            }

            for (int index = 0; index < SlotSpinResult.CellCount; index++)
            {
                if (_cellIcons[index] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private Image[] FindSlotCellIconsInScene()
        {
            var icons = new List<IndexedSlotCellIcon>(SlotSpinResult.CellCount);
            Transform root = transform.root != null ? transform.root : transform;
            CollectSlotCellIcons(root, icons);

            Scene scene = gameObject.scene;
            if (icons.Count < SlotSpinResult.CellCount && scene.IsValid() && scene.isLoaded)
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int index = 0; index < roots.Length; index++)
                {
                    if (roots[index] == null || roots[index].transform == root)
                    {
                        continue;
                    }

                    CollectSlotCellIcons(roots[index].transform, icons);
                }
            }

            if (icons.Count == 0)
            {
                return Array.Empty<Image>();
            }

            icons.Sort((left, right) => left.Index.CompareTo(right.Index));
            var result = new Image[Math.Min(SlotSpinResult.CellCount, icons.Count)];
            for (int index = 0; index < result.Length; index++)
            {
                result[index] = icons[index].Image;
            }

            return result;
        }

        private static void CollectSlotCellIcons(Transform parent, List<IndexedSlotCellIcon> icons)
        {
            if (parent == null)
            {
                return;
            }

            if (TryGetSlotCellIconIndex(parent.name, out int slotIndex))
            {
                Image image = parent.GetComponent<Image>();
                if (image != null)
                {
                    icons.Add(new IndexedSlotCellIcon(slotIndex, image));
                }
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                CollectSlotCellIcons(parent.GetChild(index), icons);
            }
        }

        private static bool TryGetSlotCellIconIndex(string objectName, out int index)
        {
            index = -1;

            const string baseName = "Slot Cell Icon";
            if (objectName == baseName)
            {
                index = 0;
                return true;
            }

            if (string.IsNullOrEmpty(objectName) ||
                !objectName.StartsWith(baseName + " (", StringComparison.Ordinal) ||
                !objectName.EndsWith(")", StringComparison.Ordinal))
            {
                return false;
            }

            int startIndex = baseName.Length + 2;
            int length = objectName.Length - startIndex - 1;
            if (length <= 0)
            {
                return false;
            }

            return int.TryParse(objectName.Substring(startIndex, length), out index);
        }

        private readonly struct IndexedSlotCellIcon
        {
            public IndexedSlotCellIcon(int index, Image image)
            {
                Index = index;
                Image = image;
            }

            public int Index { get; }
            public Image Image { get; }
        }

        private Tween _cycleTween;
        private Sequence _lockSequence;
        private bool[] _lockedCells;
    }
}
