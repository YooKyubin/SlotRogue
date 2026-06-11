using System;
using System.Collections;
using SlotRogue.Slot.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation.Reel
{
    /// <summary>
    /// Drives the slot spin as five vertically rotating reels overlaid on the existing 5x3 cell
    /// grid. Replaces the sprite-swap animation of <see cref="SlotCellSpinView"/> while keeping the
    /// original cell <see cref="Image"/>s as the canonical final-result display, so downstream views
    /// (PatternPresentationView / FinalResultView) keep highlighting the same cells unchanged.
    ///
    /// Reels are built once and reused. During a spin the cells already hold the final symbols and
    /// are simply covered by the reels; when a reel stops the result is revealed seamlessly.
    /// </summary>
    public sealed class SlotMachineSpinPresenter : MonoBehaviour
    {
        private const int Columns = SlotSpinResult.Columns;
        private const int VisibleRows = SlotSpinResult.Rows;

        [Header("Authored reels (optional)")]
        [Tooltip("Place the reel overlay in the scene and assign its container + 5 reels here. " +
            "If left empty, the overlay is built at runtime over the slot cells.")]
        [SerializeField] private RectTransform _overlay;
        [SerializeField] private SlotReelView[] _reels;

        [Header("Timing")]
        [SerializeField] private float _spinUpDuration = 0.5f;
        [SerializeField] private float _stopIntervalPerColumn = 0.18f;

        // Needs symbol sprites, plus either authored reels (the reels are the display) or the legacy
        // cell grid (runtime builds the overlay over the cells and the cells show the final result).
        public bool IsReady =>
            _symbolSprites != null && _symbolSprites.Length > 0 && (HasAuthoredReels() || HasCells());

        // When the reels are authored in the scene they stay on as the slot face; the legacy 5x3 cell
        // grid is no longer required and can be deleted. Runtime-built reels still use the cells.
        private bool ReelsAreDisplay => _authored || HasAuthoredReels();

        public void Initialize(Image[] cellIcons, Sprite[] symbolSprites, Sprite[] spinSprites)
        {
            _cellIcons = cellIcons;
            _symbolSprites = symbolSprites;
            _spinSprites = spinSprites;
        }

        public IEnumerator Play(SlotSpinResult result, Func<bool> shouldSkip)
        {
            if (result == null || !EnsureReels())
            {
                yield break;
            }

            bool reelDisplay = ReelsAreDisplay;

            // Hide the underlying cell icons so only the reels animate (the reel symbol sprites are
            // transparent, otherwise static cells would show through and look like a second slot).
            // In reel-display mode the cells stay hidden permanently — they can be deleted.
            HideCellIcons();
            ShowReels();
            if (!reelDisplay)
            {
                PositionReels();
            }

            if (IsSkipped(shouldSkip))
            {
                SetAllReelsImmediate(result);
                FinishSpin(result, reelDisplay);
                yield break;
            }

            for (int column = 0; column < Columns; column++)
            {
                _reels[column].SetVisible(true);
                _reels[column].StartSpin();
            }

            yield return WaitSpinUp(shouldSkip);

            for (int column = 0; column < Columns; column++)
            {
                StartCoroutine(_reels[column].StopRoutine(ResultColumn(result, column), shouldSkip));
                yield return WaitColumnInterval(shouldSkip);

                if (IsSkipped(shouldSkip))
                {
                    break;
                }
            }

            while (!AllReelsStopped() && !IsSkipped(shouldSkip))
            {
                yield return null;
            }

            if (IsSkipped(shouldSkip))
            {
                SetAllReelsImmediate(result);
            }

            FinishSpin(result, reelDisplay);
        }

        private void SetAllReelsImmediate(SlotSpinResult result)
        {
            for (int column = 0; column < Columns; column++)
            {
                _reels[column].SetImmediate(ResultColumn(result, column));
            }
        }

        private void FinishSpin(SlotSpinResult result, bool reelDisplay)
        {
            if (reelDisplay)
            {
                // The reels stay on showing the final result; nothing to reveal.
                return;
            }

            // Legacy path: copy the result onto the cell grid and hide the reels so the cells (which
            // downstream highlight views target) show the result.
            ApplyResultToCells(result);
            HideReels();
        }

        public void StopImmediate(SlotSpinResult result = null)
        {
            if (_reels != null)
            {
                for (int column = 0; column < Columns && column < _reels.Length; column++)
                {
                    if (_reels[column] == null)
                    {
                        continue;
                    }

                    if (result != null)
                    {
                        _reels[column].SetImmediate(ResultColumn(result, column));
                    }
                    else
                    {
                        _reels[column].StopImmediate();
                    }
                }
            }

            if (ReelsAreDisplay)
            {
                // Reels are the slot face: keep them on, leave the (deletable) cell grid alone.
                return;
            }

            if (result != null)
            {
                ApplyResultToCells(result);
            }
            else
            {
                ShowCellIcons();
            }

            HideReels();
        }

        private void Awake()
        {
            // Runtime-built overlays start hidden until a spin. Authored reels are the slot face, so
            // they stay visible (showing their authored/last result) between spins.
            if (_overlay != null && !ReelsAreDisplay)
            {
                _overlay.gameObject.SetActive(false);
            }
        }

        private IEnumerator Start()
        {
            // Wait one frame for the canvas to lay out, then arrange authored reels so the slot shows
            // at rest (before any spin). Runtime-built overlays handle this lazily on first spin.
            yield return null;
            PrepareAuthoredRestDisplay();
        }

        private void PrepareAuthoredRestDisplay()
        {
            if (!HasAuthoredReels())
            {
                TryAutoDiscoverReels();
            }

            if (!HasAuthoredReels())
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutAuthoredReelsIfNeeded();

            for (int column = 0; column < Columns; column++)
            {
                _reels[column].PrepareLayout();
            }
        }

        private void OnDisable()
        {
            HideReels();
        }

        private IEnumerator WaitSpinUp(Func<bool> shouldSkip)
        {
            float elapsed = 0f;
            while (elapsed < _spinUpDuration && !IsSkipped(shouldSkip))
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator WaitColumnInterval(Func<bool> shouldSkip)
        {
            float elapsed = 0f;
            while (elapsed < _stopIntervalPerColumn && !IsSkipped(shouldSkip))
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private bool AllReelsStopped()
        {
            for (int column = 0; column < Columns; column++)
            {
                if (!_reels[column].IsStopped)
                {
                    return false;
                }
            }

            return true;
        }

        private bool EnsureReels()
        {
            if (_built)
            {
                return true;
            }

            // Reels placed in the scene are used as-is; discover them even if the inspector links
            // were never wired, so no manual setup (or runtime UI creation) is required.
            if (!HasAuthoredReels())
            {
                TryAutoDiscoverReels();
            }

            if (!IsReady)
            {
                return false;
            }

            if (HasAuthoredReels())
            {
                _authored = true;

                if (_overlay == null && _reels[0] != null)
                {
                    _overlay = _reels[0].transform.parent as RectTransform;
                }

                Canvas.ForceUpdateCanvases();
                LayoutAuthoredReelsIfNeeded();

                for (int column = 0; column < Columns; column++)
                {
                    _reels[column].Build(_symbolSprites, _spinSprites);
                }

                if (_overlay != null)
                {
                    _overlay.gameObject.SetActive(false);
                }

                _built = true;
                return true;
            }

            return BuildReelsAtRuntime();
        }

        private bool HasCells()
        {
            return _cellIcons != null && _cellIcons.Length >= SlotSpinResult.CellCount;
        }

        // Finds reels placed in the scene (e.g. under "Slot Reel Overlay") and orders them left -> right
        // so column mapping is correct, even if the presenter's inspector references were not wired.
        // Orders by the trailing index in the name ("Reel 0".."Reel 4"); falls back to horizontal
        // position when names don't carry an index (reels may overlap before they are auto-sized).
        private void TryAutoDiscoverReels()
        {
            SlotReelView[] found = transform.root.GetComponentsInChildren<SlotReelView>(true);
            if (found == null || found.Length < Columns)
            {
                return;
            }

            System.Array.Sort(found, CompareReelOrder);

            _reels = new SlotReelView[Columns];
            for (int column = 0; column < Columns; column++)
            {
                _reels[column] = found[column];
            }

            if (_overlay == null && _reels[0] != null)
            {
                _overlay = _reels[0].transform.parent as RectTransform;
            }
        }

        private static int CompareReelOrder(SlotReelView left, SlotReelView right)
        {
            int leftIndex = ParseTrailingIndex(left.name);
            int rightIndex = ParseTrailingIndex(right.name);
            if (leftIndex >= 0 && rightIndex >= 0 && leftIndex != rightIndex)
            {
                return leftIndex.CompareTo(rightIndex);
            }

            return left.transform.position.x.CompareTo(right.transform.position.x);
        }

        private static int ParseTrailingIndex(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return -1;
            }

            int end = name.Length;
            int start = end;
            while (start > 0 && char.IsDigit(name[start - 1]))
            {
                start--;
            }

            return start < end && int.TryParse(name.Substring(start, end - start), out int index) ? index : -1;
        }

        // When reels have no usable size (e.g. placed with a zero RectTransform), arrange the five
        // reels to evenly fill the overlay as columns. Reels the user has sized are left untouched, so
        // authoring just needs a sensibly sized overlay; the columns lay themselves out inside it.
        private void LayoutAuthoredReelsIfNeeded()
        {
            if (_overlay == null)
            {
                return;
            }

            Rect overlayRect = _overlay.rect;
            if (overlayRect.width <= 0f || overlayRect.height <= 0f)
            {
                return;
            }

            float columnWidth = overlayRect.width / Columns;
            float columnHeight = overlayRect.height;

            for (int column = 0; column < Columns; column++)
            {
                var rect = (RectTransform)_reels[column].transform;
                if (rect.rect.width > 1f && rect.rect.height > 1f)
                {
                    continue;
                }

                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(columnWidth, columnHeight);
                rect.anchoredPosition = new Vector2((column - (Columns - 1) * 0.5f) * columnWidth, 0f);
            }
        }

        private bool HasAuthoredReels()
        {
            if (_reels == null || _reels.Length < Columns)
            {
                return false;
            }

            for (int column = 0; column < Columns; column++)
            {
                if (_reels[column] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private bool BuildReelsAtRuntime()
        {
            Canvas canvas = _cellIcons[0] != null ? _cellIcons[0].canvas : null;
            if (canvas == null)
            {
                return false;
            }

            Canvas.ForceUpdateCanvases();

            _overlay = CreateOverlay(canvas.transform);
            _reels = new SlotReelView[Columns];

            for (int column = 0; column < Columns; column++)
            {
                if (!TryGetColumnRect(column, out Vector2 localCenter, out Vector2 localSize))
                {
                    _reels = null;
                    return false;
                }

                var go = new GameObject($"Reel {column}", typeof(RectTransform));
                var rect = (RectTransform)go.transform;
                rect.SetParent(_overlay, false);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = localCenter;
                rect.sizeDelta = localSize;

                var reel = go.AddComponent<SlotReelView>();
                reel.Build(_symbolSprites, _spinSprites);
                _reels[column] = reel;
            }

            _overlay.gameObject.SetActive(false);
            _authored = false;
            _built = true;
            return true;
        }

        // Re-aligns each reel viewport to its column's current cell rects. Cheap, and self-corrects
        // if the first build measured before the canvas layout had settled.
        private void PositionReels()
        {
            if (_authored || _reels == null || _overlay == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            for (int column = 0; column < Columns; column++)
            {
                if (_reels[column] == null || !TryGetColumnRect(column, out Vector2 localCenter, out Vector2 localSize))
                {
                    continue;
                }

                var rect = (RectTransform)_reels[column].transform;
                rect.anchoredPosition = localCenter;
                rect.sizeDelta = localSize;
            }
        }

        private RectTransform CreateOverlay(Transform canvasTransform)
        {
            var go = new GameObject("Slot Reel Overlay", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvasTransform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            return rect;
        }

        private bool TryGetColumnRect(int column, out Vector2 localCenter, out Vector2 localSize)
        {
            localCenter = Vector2.zero;
            localSize = Vector2.zero;

            var worldMin = new Vector3(float.MaxValue, float.MaxValue, 0f);
            var worldMax = new Vector3(float.MinValue, float.MinValue, 0f);
            var corners = new Vector3[4];

            for (int row = 0; row < VisibleRows; row++)
            {
                int index = SlotSpinResult.ToIndex(column, row);
                if (index >= _cellIcons.Length || _cellIcons[index] == null)
                {
                    return false;
                }

                _cellIcons[index].rectTransform.GetWorldCorners(corners);
                for (int corner = 0; corner < corners.Length; corner++)
                {
                    worldMin = Vector3.Min(worldMin, corners[corner]);
                    worldMax = Vector3.Max(worldMax, corners[corner]);
                }
            }

            Vector3 localLow = _overlay.InverseTransformPoint(worldMin);
            Vector3 localHigh = _overlay.InverseTransformPoint(worldMax);
            localCenter = (localLow + localHigh) * 0.5f;
            localSize = new Vector2(Mathf.Abs(localHigh.x - localLow.x), Mathf.Abs(localHigh.y - localLow.y));
            return localSize.x > 0f && localSize.y > 0f;
        }

        private void ApplyResultToCells(SlotSpinResult result)
        {
            for (int column = 0; column < Columns; column++)
            {
                for (int row = 0; row < VisibleRows; row++)
                {
                    int index = SlotSpinResult.ToIndex(column, row);
                    if (index >= _cellIcons.Length || _cellIcons[index] == null)
                    {
                        continue;
                    }

                    Sprite sprite = SymbolSprite(result.GetSymbol(column, row));
                    _cellIcons[index].sprite = sprite;
                    _cellIcons[index].enabled = sprite != null;
                    _cellIcons[index].preserveAspect = true;
                }
            }
        }

        private void HideCellIcons()
        {
            if (_cellIcons == null)
            {
                return;
            }

            for (int index = 0; index < _cellIcons.Length; index++)
            {
                if (_cellIcons[index] != null)
                {
                    _cellIcons[index].enabled = false;
                }
            }
        }

        private void ShowCellIcons()
        {
            if (_cellIcons == null)
            {
                return;
            }

            for (int index = 0; index < _cellIcons.Length; index++)
            {
                if (_cellIcons[index] != null)
                {
                    _cellIcons[index].enabled = _cellIcons[index].sprite != null;
                }
            }
        }

        private SlotSymbolType[] ResultColumn(SlotSpinResult result, int column)
        {
            var rows = new SlotSymbolType[VisibleRows];
            for (int row = 0; row < VisibleRows; row++)
            {
                rows[row] = result.GetSymbol(column, row);
            }

            return rows;
        }

        private Sprite SymbolSprite(SlotSymbolType symbol)
        {
            int index = (int)symbol;
            if (_symbolSprites == null || index < 0 || index >= _symbolSprites.Length)
            {
                return null;
            }

            return _symbolSprites[index];
        }

        private void ShowReels()
        {
            if (_overlay != null)
            {
                _overlay.gameObject.SetActive(true);
                if (!_authored)
                {
                    _overlay.SetAsLastSibling();
                }
            }

            if (_reels != null)
            {
                for (int column = 0; column < Columns && column < _reels.Length; column++)
                {
                    if (_reels[column] != null)
                    {
                        _reels[column].SetVisible(true);
                    }
                }
            }
        }

        private void HideReels()
        {
            if (_reels != null)
            {
                for (int column = 0; column < Columns && column < _reels.Length; column++)
                {
                    if (_reels[column] != null)
                    {
                        _reels[column].StopImmediate();
                    }
                }
            }

            if (_overlay != null)
            {
                _overlay.gameObject.SetActive(false);
            }
            else if (_reels != null)
            {
                for (int column = 0; column < Columns && column < _reels.Length; column++)
                {
                    if (_reels[column] != null)
                    {
                        _reels[column].SetVisible(false);
                    }
                }
            }
        }

        private static bool IsSkipped(Func<bool> shouldSkip)
        {
            return shouldSkip != null && shouldSkip();
        }

        private Image[] _cellIcons;
        private Sprite[] _symbolSprites;
        private Sprite[] _spinSprites;
        private bool _authored;
        private bool _built;
    }
}
