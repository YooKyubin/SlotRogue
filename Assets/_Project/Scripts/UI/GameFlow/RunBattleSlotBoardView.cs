using System;
using System.Collections.Generic;
using DG.Tweening;
using SlotRogue.Slot.Data;
using SlotRogue.UI.SlotPresentation;
using SlotRogue.UI.SlotPresentation.Reel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleSlotBoardView : MonoBehaviour
    {
        private enum PatternCueMotion
        {
            TiltPulse,
            SlotSettle,
            RisePop,
        }

        private const string SwapHitAreaName = "Swap Hit Area";
        private const string HighlightOverlayName = "Pattern Highlight";
        private const float SymbolNativeSizeMultiplier = 1.25f;

        private static readonly Color PatternHitColor = new(1f, 0.82f, 0.23f, 1f);
        private static readonly Color SwapSelectedColor = new(0.38f, 0.74f, 1f, 1f);
        private static readonly Color SwapSelectedIconColor = new(0.38f, 0.74f, 1f, 0.45f);
        private static readonly Color TransparentIconColor = new(1f, 1f, 1f, 0f);

        [SerializeField] private Text[] _slotCells;
        [SerializeField] private Image[] _slotCellIcons;
        [SerializeField] private SlotMachineSpinDirector _spinDirector;
        [SerializeField] private SlotCellSpinView _slotCellSpinView;
        [SerializeField] private PatternCueMotion _patternCueMotion = PatternCueMotion.TiltPulse;

        private Color[] _slotCellDefaultColors;
        private Image[] _slotCellVisualIcons;
        private Image[] _slotCellHighlightOverlays;
        private Sprite[] _slotCellHighlightSprites;
        private bool _hasCachedDefaults;
        private bool _swapInputEnabled;
        private bool _slotCellIconsAreHitTargets;
        private Button[] _slotCellButtons;
        private Button[] _slotCellIconButtons;
        private Button[] _subscribedSlotCellButtons;
        private Button[] _subscribedSlotCellIconButtons;
        private bool _missingReferenceErrorLogged;
        private UnityAction[] _slotCellButtonHandlers;
        private UnityAction[] _slotCellIconButtonHandlers;
        private readonly List<Tween> _patternCueTweens = new();
        private readonly List<Transform> _patternCueTargets = new();
        private readonly List<Vector3> _patternCueDefaultLocalPositions = new();
        private readonly List<Vector3> _patternCueDefaultLocalScales = new();
        private readonly List<Quaternion> _patternCueDefaultLocalRotations = new();
        private readonly List<Image> _highlightedVisualIcons = new();
        private readonly List<Sprite> _highlightedVisualDefaultSprites = new();
        private readonly List<Sprite> _highlightedVisualAppliedSprites = new();
        private readonly List<bool> _highlightedVisualDefaultEnabled = new();
        private readonly List<Vector2> _highlightedVisualDefaultSizes = new();
        private int[] _activePatternCueIndices = Array.Empty<int>();
        private bool _activePatternCuePending;

        public event Action<int> SlotCellSelected;
        public event Action<int, int> SlotCellsDragged;

        private void Awake()
        {
            EnsureSlotCellButtons();
            EnsureSlotCellIconButtons();
            SubscribeSlotCellButtons();
            SubscribeSlotCellIconButtons();
        }

        private void OnDestroy()
        {
            StopPatternCue();
            UnsubscribeSlotCellButtons();
            UnsubscribeSlotCellIconButtons();
        }

        private void OnDisable()
        {
            StopPatternCue();
        }

        public void Bind(Text[] slotCells)
        {
            UnsubscribeSlotCellButtons();
            _slotCells = slotCells;
            _slotCellButtons = null;
            _slotCellIconButtons = null;
            _slotCellVisualIcons = null;
            _slotCellHighlightOverlays = null;
            _slotCellDefaultColors = null;
            _hasCachedDefaults = false;
            EnsureSlotCellButtons();
            EnsureSlotCellIconButtons();
            SubscribeSlotCellButtons();
            SubscribeSlotCellIconButtons();
        }

        public void SetHighlightSymbolSprites(Sprite[] sprites)
        {
            _slotCellHighlightSprites = sprites;
        }

        public void Render(RunBattleScreenState state)
        {
            EnsureSlotCellButtons();
            EnsureSlotCellIconButtons();
            SubscribeSlotCellButtons();
            SubscribeSlotCellIconButtons();
            RenderSlotCells(state.SlotCells);
            RenderOutcome(state.SlotOutcome, state.Swap, state.ActionMode == RunBattleActionMode.Attack);
            RenderBoardInput(state);
        }

        private void RenderSlotCells(string[] values)
        {
            if (_slotCells == null || values == null)
            {
                return;
            }

            int count = Mathf.Min(_slotCells.Length, values.Length);
            for (int index = 0; index < count; index++)
            {
                if (_slotCells[index] != null)
                {
                    _slotCells[index].text = values[index];
                }
            }
        }

        private void RenderOutcome(
            RunBattleSlotOutcomeState outcome,
            RunBattleSwapState swap,
            bool pendingPatternCue)
        {
            CacheDefaultsIfNeeded();
            ResetSlotCellColors();
            ResetSlotCellIconHighlights();
            RenderPatternHit(outcome, pendingPatternCue);
            RenderSwapSelection(swap);
            RenderSwapSelectionIcon(swap);
        }

        // The static text cells are unused in the reel display, so the swap selection highlight is
        // drawn on the fixed per-window hit targets instead.
        private void ResetSlotCellIconHighlights()
        {
            RestoreHighlightedVisualSprites();
            EnsureSlotCellHighlightOverlays();

            if (_slotCellIconsAreHitTargets && _slotCellIcons != null)
            {
                for (int index = 0; index < _slotCellIcons.Length; index++)
                {
                    if (_slotCellIcons[index] != null)
                    {
                        _slotCellIcons[index].color = TransparentIconColor;
                    }
                }
            }

            if (_slotCellHighlightOverlays == null)
            {
                return;
            }

            for (int index = 0; index < _slotCellHighlightOverlays.Length; index++)
            {
                Image overlay = _slotCellHighlightOverlays[index];
                if (overlay == null)
                {
                    continue;
                }

                overlay.enabled = false;
                overlay.sprite = null;
                overlay.color = Color.white;
                overlay.rectTransform.localScale = Vector3.one;
            }
        }

        private void RenderSwapSelectionIcon(RunBattleSwapState swap)
        {
            if (!_slotCellIconsAreHitTargets ||
                !swap.HasSelection ||
                _slotCellIcons == null ||
                swap.SelectedCellIndex < 0 ||
                swap.SelectedCellIndex >= _slotCellIcons.Length ||
                _slotCellIcons[swap.SelectedCellIndex] == null)
            {
                return;
            }

            _slotCellIcons[swap.SelectedCellIndex].color = SwapSelectedIconColor;
        }

        private void RenderPatternHit(
            RunBattleSlotOutcomeState outcome,
            bool pendingPatternCue)
        {
            int[] highlightedCellIndices = outcome.HighlightedCellIndices;
            if (!outcome.HasPattern || highlightedCellIndices.Length == 0)
            {
                StopPatternCue();
                return;
            }

            for (int index = 0; index < highlightedCellIndices.Length; index++)
            {
                int cellIndex = highlightedCellIndices[index];
                RenderPatternHitText(cellIndex);
                RenderPatternHighlight(cellIndex, ResolveHighlightedSymbol(outcome, cellIndex));
            }

            UpdatePatternCue(highlightedCellIndices, pendingPatternCue);
        }

        private static SlotSymbolType? ResolveHighlightedSymbol(
            RunBattleSlotOutcomeState outcome,
            int cellIndex)
        {
            SlotSymbolType?[] symbols = outcome.HighlightedCellSymbols;
            if (!SlotSpinResult.IsValidIndex(cellIndex) ||
                symbols == null ||
                cellIndex >= symbols.Length)
            {
                return null;
            }

            return symbols[cellIndex];
        }

        private void RenderPatternHitText(int cellIndex)
        {
            if (_slotCells == null ||
                cellIndex < 0 ||
                cellIndex >= _slotCells.Length ||
                _slotCells[cellIndex] == null)
            {
                return;
            }

            _slotCells[cellIndex].color = PatternHitColor;
        }

        private void RenderPatternHighlight(int cellIndex, SlotSymbolType? symbol)
        {
            if (!TryResolveHighlightSprite(cellIndex, symbol, out Sprite sprite))
            {
                return;
            }

            RefreshSlotCellVisualIcons();
            Image visualIcon = ResolveVisualIcon(cellIndex);
            if (visualIcon != null)
            {
                ApplyPatternHighlightSprite(visualIcon, sprite);
                RenderPatternHighlightOverlay(cellIndex, visualIcon, sprite);
                return;
            }

            RenderPatternHighlightOverlay(cellIndex, sprite);
        }

        private void ApplyPatternHighlightSprite(Image visualIcon, Sprite sprite)
        {
            if (visualIcon == null || sprite == null)
            {
                return;
            }

            int trackedIndex = _highlightedVisualIcons.IndexOf(visualIcon);
            if (trackedIndex < 0)
            {
                _highlightedVisualIcons.Add(visualIcon);
                _highlightedVisualDefaultSprites.Add(visualIcon.sprite);
                _highlightedVisualAppliedSprites.Add(sprite);
                _highlightedVisualDefaultEnabled.Add(visualIcon.enabled);
                _highlightedVisualDefaultSizes.Add(visualIcon.rectTransform.sizeDelta);
            }
            else
            {
                _highlightedVisualAppliedSprites[trackedIndex] = sprite;
            }

            visualIcon.sprite = sprite;
            visualIcon.preserveAspect = true;
            visualIcon.enabled = true;
            ApplyNativeSymbolSize(visualIcon);
        }

        private void RenderPatternHighlightOverlay(int cellIndex, Sprite sprite)
        {
            Image overlay = ResolveHighlightOverlay(cellIndex);
            if (overlay == null || sprite == null)
            {
                return;
            }

            RenderPatternHighlightOverlay(overlay, sprite);
        }

        private void RenderPatternHighlightOverlay(int cellIndex, Image parent, Sprite sprite)
        {
            if (parent == null || sprite == null)
            {
                return;
            }

            Image overlay = EnsureHighlightOverlay(parent);
            if (_slotCellHighlightOverlays != null &&
                SlotSpinResult.IsValidIndex(cellIndex) &&
                cellIndex < _slotCellHighlightOverlays.Length)
            {
                _slotCellHighlightOverlays[cellIndex] = overlay;
            }

            RenderPatternHighlightOverlay(overlay, sprite);
        }

        private static void RenderPatternHighlightOverlay(Image overlay, Sprite sprite)
        {
            if (overlay == null || sprite == null)
            {
                return;
            }

            overlay.sprite = sprite;
            overlay.color = Color.white;
            overlay.preserveAspect = true;
            overlay.enabled = true;
        }

        private void HidePatternHighlightOverlay(int cellIndex)
        {
            Image overlay = ResolveHighlightOverlay(cellIndex);
            if (overlay == null)
            {
                return;
            }

            overlay.enabled = false;
            overlay.sprite = null;
            overlay.color = Color.white;
            overlay.rectTransform.localScale = Vector3.one;
        }

        private void RestoreHighlightedVisualSprites()
        {
            int restoreCount = Mathf.Min(
                _highlightedVisualIcons.Count,
                _highlightedVisualDefaultSprites.Count,
                _highlightedVisualAppliedSprites.Count,
                _highlightedVisualDefaultEnabled.Count);
            for (int index = 0; index < restoreCount; index++)
            {
                Image icon = _highlightedVisualIcons[index];
                if (icon == null)
                {
                    continue;
                }

                if (icon.sprite == _highlightedVisualAppliedSprites[index])
                {
                    icon.sprite = _highlightedVisualDefaultSprites[index];
                    icon.enabled = _highlightedVisualDefaultEnabled[index];
                    if (index < _highlightedVisualDefaultSizes.Count)
                    {
                        icon.rectTransform.sizeDelta = _highlightedVisualDefaultSizes[index];
                    }
                }
            }

            _highlightedVisualIcons.Clear();
            _highlightedVisualDefaultSprites.Clear();
            _highlightedVisualAppliedSprites.Clear();
            _highlightedVisualDefaultEnabled.Clear();
            _highlightedVisualDefaultSizes.Clear();
        }

        private static void ApplyNativeSymbolSize(Image icon)
        {
            if (icon == null || icon.sprite == null)
            {
                return;
            }

            icon.SetNativeSize();
            icon.rectTransform.sizeDelta *= SymbolNativeSizeMultiplier;
        }

        private void RenderSwapSelection(RunBattleSwapState swap)
        {
            if (!swap.HasSelection ||
                _slotCells == null ||
                swap.SelectedCellIndex >= _slotCells.Length ||
                _slotCells[swap.SelectedCellIndex] == null)
            {
                return;
            }

            _slotCells[swap.SelectedCellIndex].color = SwapSelectedColor;
        }

        private void RenderBoardInput(RunBattleScreenState state)
        {
            _swapInputEnabled = state.Swap.CanSelectCells;

            if (_slotCellButtons == null)
            {
                RenderBoardInput(_slotCellIconButtons, _swapInputEnabled);
                return;
            }

            RenderBoardInput(_slotCellButtons, _swapInputEnabled);
            RenderBoardInput(_slotCellIconButtons, _swapInputEnabled);
        }

        private static void RenderBoardInput(Button[] buttons, bool inputEnabled)
        {
            if (buttons == null)
            {
                return;
            }

            for (int index = 0; index < buttons.Length; index++)
            {
                Button button = buttons[index];
                if (button != null)
                {
                    button.interactable = inputEnabled;
                    if (button.targetGraphic != null)
                    {
                        button.targetGraphic.raycastTarget = inputEnabled;
                    }
                }
            }
        }

        private void EnsureSlotCellButtons()
        {
            if (_slotCells == null)
            {
                _slotCellButtons = Array.Empty<Button>();
                return;
            }

            if (_slotCellButtons != null && _slotCellButtons.Length == _slotCells.Length)
            {
                ConfigureSlotCellButtons();
                return;
            }

            UnsubscribeSlotCellButtons();
            _slotCellButtons = new Button[_slotCells.Length];
            ConfigureSlotCellButtons();
        }

        private void EnsureSlotCellIconButtons()
        {
            EnsureSlotCellIcons();

            if (_slotCellIcons == null)
            {
                _slotCellIconButtons = Array.Empty<Button>();
                return;
            }

            if (_slotCellIconButtons != null && _slotCellIconButtons.Length == _slotCellIcons.Length)
            {
                ConfigureSlotCellIconButtons();
                return;
            }

            UnsubscribeSlotCellIconButtons();
            _slotCellIconButtons = new Button[_slotCellIcons.Length];
            ConfigureSlotCellIconButtons();
        }

        private void ConfigureSlotCellButtons()
        {
            if (_slotCells == null || _slotCellButtons == null)
            {
                return;
            }

            int count = Mathf.Min(_slotCells.Length, _slotCellButtons.Length);
            for (int index = 0; index < count; index++)
            {
                Text cell = _slotCells[index];
                if (cell == null)
                {
                    _slotCellButtons[index] = null;
                    continue;
                }

                Button button = cell.GetComponent<Button>();
                if (button == null)
                {
                    button = cell.gameObject.AddComponent<Button>();
                }

                button.transition = Selectable.Transition.None;
                button.targetGraphic = cell;
                button.interactable = false;
                cell.raycastTarget = false;
                _slotCellButtons[index] = button;

                SlotCellSwapInput input = cell.GetComponent<SlotCellSwapInput>();
                if (input == null)
                {
                    input = cell.gameObject.AddComponent<SlotCellSwapInput>();
                }

                input.Bind(this, index);
            }
        }

        private void ConfigureSlotCellIconButtons()
        {
            if (_slotCellIcons == null || _slotCellIconButtons == null)
            {
                return;
            }

            int count = Mathf.Min(_slotCellIcons.Length, _slotCellIconButtons.Length);
            for (int index = 0; index < count; index++)
            {
                Image icon = _slotCellIcons[index];
                if (icon == null)
                {
                    _slotCellIconButtons[index] = null;
                    continue;
                }

                Graphic hitGraphic = EnsureIconHitArea(icon);
                Button button = hitGraphic.GetComponent<Button>();
                if (button == null)
                {
                    button = hitGraphic.gameObject.AddComponent<Button>();
                }

                button.transition = Selectable.Transition.None;
                button.targetGraphic = hitGraphic;
                button.interactable = false;
                hitGraphic.raycastTarget = false;
                _slotCellIconButtons[index] = button;

                SlotCellSwapInput input = hitGraphic.GetComponent<SlotCellSwapInput>();
                if (input == null)
                {
                    input = hitGraphic.gameObject.AddComponent<SlotCellSwapInput>();
                }

                input.Bind(this, index);
            }
        }

        private static Graphic EnsureIconHitArea(Image icon)
        {
            Transform existing = icon.transform.Find(SwapHitAreaName);
            if (existing != null && existing.TryGetComponent(out Image existingImage))
            {
                StretchToParent(existingImage.rectTransform);
                existing.SetAsLastSibling();
                return existingImage;
            }

            var hitArea = new GameObject(SwapHitAreaName, typeof(RectTransform), typeof(Image));
            hitArea.transform.SetParent(icon.transform, false);
            hitArea.transform.SetAsLastSibling();

            var rectTransform = hitArea.GetComponent<RectTransform>();
            StretchToParent(rectTransform);

            Image image = hitArea.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = false;
            return image;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }

        private void EnsureSlotCellIcons()
        {
            RefreshSlotCellVisualIcons();

            if (HasUsableSlotCellIcons(_slotCellIcons))
            {
                return;
            }

            // Prefer the fixed per-window hit targets: the reel recycles/reorders its symbol item
            // icons during a spin, so binding swap input to the visible icons would leave the hit
            // areas misaligned with the cells after every spin.
            if (_spinDirector != null &&
                _spinDirector.TryGetWindowHitTargets(out Image[] hitTargets) &&
                HasUsableSlotCellIcons(hitTargets))
            {
                _slotCellIcons = hitTargets;
                _slotCellIconsAreHitTargets = true;
                RefreshSlotCellVisualIcons(_spinDirector);
                return;
            }

            if (_spinDirector != null &&
                _spinDirector.TryGetVisibleCellIcons(out Image[] visibleIcons) &&
                HasUsableSlotCellIcons(visibleIcons))
            {
                _slotCellIcons = visibleIcons;
                _slotCellVisualIcons = visibleIcons;
                _slotCellIconsAreHitTargets = false;
                return;
            }

            if (_slotCellSpinView != null &&
                _slotCellSpinView.TryGetReelBindings(
                    out Image[] cellIcons,
                    out _,
                    out _) &&
                HasUsableSlotCellIcons(cellIcons))
            {
                _slotCellIcons = cellIcons;
                _slotCellVisualIcons = cellIcons;
                _slotCellIconsAreHitTargets = false;
                return;
            }

            LogMissingIconReferences();
        }

        private void RefreshSlotCellVisualIcons(SlotMachineSpinDirector spinDirector = null)
        {
            spinDirector ??= _spinDirector;
            if (spinDirector != null &&
                spinDirector.TryGetVisibleCellIcons(out Image[] visibleIcons) &&
                HasUsableSlotCellIcons(visibleIcons))
            {
                _slotCellVisualIcons = visibleIcons;
                return;
            }

            if (!_slotCellIconsAreHitTargets && HasUsableSlotCellIcons(_slotCellIcons))
            {
                _slotCellVisualIcons = _slotCellIcons;
            }
        }

        private void LogMissingIconReferences()
        {
            if (_missingReferenceErrorLogged)
            {
                return;
            }

            _missingReferenceErrorLogged = true;
            Debug.LogError(
                "[RunBattleSlotBoardView] Slot cell icons must be wired in the inspector, " +
                "or Slot Machine Spin Director / Slot Cell Spin View must be assigned as an explicit source.",
                this);
        }

        private void EnsureSlotCellHighlightOverlays()
        {
            if (!HasUsableSlotCellIcons(_slotCellIcons) &&
                !HasUsableSlotCellIcons(_slotCellVisualIcons))
            {
                _slotCellHighlightOverlays = Array.Empty<Image>();
                return;
            }

            int overlayCount = HasUsableSlotCellIcons(_slotCellIcons)
                ? _slotCellIcons.Length
                : _slotCellVisualIcons.Length;
            if (_slotCellHighlightOverlays != null &&
                _slotCellHighlightOverlays.Length == overlayCount)
            {
                return;
            }

            _slotCellHighlightOverlays = new Image[overlayCount];
            for (int index = 0; index < overlayCount; index++)
            {
                Image parent = ResolveHighlightParent(index);
                if (parent == null)
                {
                    continue;
                }

                _slotCellHighlightOverlays[index] = EnsureHighlightOverlay(parent);
            }
        }

        private Image ResolveHighlightParent(int cellIndex)
        {
            Image visualIcon = ResolveVisualIcon(cellIndex);
            if (visualIcon != null)
            {
                return visualIcon;
            }

            if (SlotSpinResult.IsValidIndex(cellIndex) &&
                _slotCellIcons != null &&
                cellIndex < _slotCellIcons.Length &&
                _slotCellIcons[cellIndex] != null)
            {
                return _slotCellIcons[cellIndex];
            }

            if (SlotSpinResult.IsValidIndex(cellIndex) &&
                _slotCellVisualIcons != null &&
                cellIndex < _slotCellVisualIcons.Length &&
                _slotCellVisualIcons[cellIndex] != null)
            {
                return _slotCellVisualIcons[cellIndex];
            }

            return null;
        }

        private Image ResolveVisualIcon(int cellIndex)
        {
            if (SlotSpinResult.IsValidIndex(cellIndex) &&
                _slotCellVisualIcons != null &&
                cellIndex < _slotCellVisualIcons.Length &&
                _slotCellVisualIcons[cellIndex] != null)
            {
                return _slotCellVisualIcons[cellIndex];
            }

            if (SlotSpinResult.IsValidIndex(cellIndex) &&
                !_slotCellIconsAreHitTargets &&
                _slotCellIcons != null &&
                cellIndex < _slotCellIcons.Length &&
                _slotCellIcons[cellIndex] != null)
            {
                return _slotCellIcons[cellIndex];
            }

            return null;
        }

        private static Image EnsureHighlightOverlay(Image parent)
        {
            Transform existing = parent.transform.Find(HighlightOverlayName);
            if (existing != null && existing.TryGetComponent(out Image existingImage))
            {
                StretchToParent(existingImage.rectTransform);
                existingImage.raycastTarget = false;
                existingImage.preserveAspect = true;
                return existingImage;
            }

            var go = new GameObject(HighlightOverlayName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent.transform, false);

            var rectTransform = go.GetComponent<RectTransform>();
            StretchToParent(rectTransform);

            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.enabled = false;
            image.color = Color.white;
            return image;
        }

        private Image ResolveHighlightOverlay(int cellIndex)
        {
            EnsureSlotCellHighlightOverlays();
            if (!SlotSpinResult.IsValidIndex(cellIndex) ||
                _slotCellHighlightOverlays == null ||
                cellIndex >= _slotCellHighlightOverlays.Length)
            {
                return null;
            }

            return _slotCellHighlightOverlays[cellIndex];
        }

        private bool TryResolveHighlightSprite(
            int cellIndex,
            SlotSymbolType? highlightedSymbol,
            out Sprite sprite)
        {
            sprite = null;
            SlotSymbolType symbol;
            if (highlightedSymbol.HasValue)
            {
                symbol = highlightedSymbol.Value;
            }
            else if (!TryResolveCellSymbol(cellIndex, out symbol))
            {
                return false;
            }

            int spriteIndex = (int)symbol;
            if (_slotCellHighlightSprites != null &&
                spriteIndex >= 0 &&
                spriteIndex < _slotCellHighlightSprites.Length &&
                _slotCellHighlightSprites[spriteIndex] != null)
            {
                sprite = _slotCellHighlightSprites[spriteIndex];
                return true;
            }

            return AddressableSpriteCache.TryGet(
                SlotSymbolIconKeys.For(symbol),
                out sprite);
        }

        private bool TryResolveCellSymbol(int cellIndex, out SlotSymbolType symbol)
        {
            symbol = default;
            return _slotCells != null &&
                cellIndex >= 0 &&
                cellIndex < _slotCells.Length &&
                _slotCells[cellIndex] != null &&
                Enum.TryParse(
                    _slotCells[cellIndex].text,
                    ignoreCase: true,
                    out symbol);
        }

        private static bool HasUsableSlotCellIcons(Image[] icons)
        {
            if (icons == null || icons.Length < SlotSpinResult.CellCount)
            {
                return false;
            }

            for (int index = 0; index < SlotSpinResult.CellCount; index++)
            {
                if (icons[index] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void SubscribeSlotCellButtons()
        {
            if (_slotCellButtons == null || _subscribedSlotCellButtons == _slotCellButtons)
            {
                return;
            }

            UnsubscribeSlotCellButtons();
            _subscribedSlotCellButtons = _slotCellButtons;
            _slotCellButtonHandlers = new UnityAction[_subscribedSlotCellButtons.Length];

            for (int index = 0; index < _subscribedSlotCellButtons.Length; index++)
            {
                Button button = _subscribedSlotCellButtons[index];
                if (button == null)
                {
                    continue;
                }

                int capturedIndex = index;
                UnityAction handler = () => HandleSlotCellSelected(capturedIndex);
                _slotCellButtonHandlers[index] = handler;
                button.onClick.AddListener(handler);
            }
        }

        private void SubscribeSlotCellIconButtons()
        {
            if (_slotCellIconButtons == null || _subscribedSlotCellIconButtons == _slotCellIconButtons)
            {
                return;
            }

            UnsubscribeSlotCellIconButtons();
            _subscribedSlotCellIconButtons = _slotCellIconButtons;
            _slotCellIconButtonHandlers = new UnityAction[_subscribedSlotCellIconButtons.Length];

            for (int index = 0; index < _subscribedSlotCellIconButtons.Length; index++)
            {
                Button button = _subscribedSlotCellIconButtons[index];
                if (button == null)
                {
                    continue;
                }

                int capturedIndex = index;
                UnityAction handler = () => HandleSlotCellSelected(capturedIndex);
                _slotCellIconButtonHandlers[index] = handler;
                button.onClick.AddListener(handler);
            }
        }

        private void UnsubscribeSlotCellButtons()
        {
            if (_subscribedSlotCellButtons == null || _slotCellButtonHandlers == null)
            {
                _subscribedSlotCellButtons = null;
                _slotCellButtonHandlers = null;
                return;
            }

            int count = Mathf.Min(_subscribedSlotCellButtons.Length, _slotCellButtonHandlers.Length);
            for (int index = 0; index < count; index++)
            {
                Button button = _subscribedSlotCellButtons[index];
                UnityAction handler = _slotCellButtonHandlers[index];
                if (button != null && handler != null)
                {
                    button.onClick.RemoveListener(handler);
                }
            }

            _subscribedSlotCellButtons = null;
            _slotCellButtonHandlers = null;
        }

        private void UnsubscribeSlotCellIconButtons()
        {
            if (_subscribedSlotCellIconButtons == null || _slotCellIconButtonHandlers == null)
            {
                _subscribedSlotCellIconButtons = null;
                _slotCellIconButtonHandlers = null;
                return;
            }

            int count = Mathf.Min(_subscribedSlotCellIconButtons.Length, _slotCellIconButtonHandlers.Length);
            for (int index = 0; index < count; index++)
            {
                Button button = _subscribedSlotCellIconButtons[index];
                UnityAction handler = _slotCellIconButtonHandlers[index];
                if (button != null && handler != null)
                {
                    button.onClick.RemoveListener(handler);
                }
            }

            _subscribedSlotCellIconButtons = null;
            _slotCellIconButtonHandlers = null;
        }

        private void HandleSlotCellSelected(int cellIndex)
        {
            if (!_swapInputEnabled)
            {
                return;
            }

            SlotCellSelected?.Invoke(cellIndex);
        }

        private void HandleSlotCellDragged(int firstIndex, PointerEventData eventData)
        {
            if (!_swapInputEnabled || !SlotSpinResult.IsValidIndex(firstIndex))
            {
                return;
            }

            int secondIndex = ResolveCellIndex(eventData);
            if (!SlotSpinResult.AreAdjacent(firstIndex, secondIndex))
            {
                return;
            }

            SlotCellsDragged?.Invoke(firstIndex, secondIndex);
        }

        private int ResolveCellIndex(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return -1;
            }

            GameObject raycastTarget = eventData.pointerCurrentRaycast.gameObject;
            int targetIndex = FindCellIndex(raycastTarget != null ? raycastTarget.transform : null);
            if (targetIndex >= 0)
            {
                return targetIndex;
            }

            return FindCellIndexAtScreenPoint(eventData.position, eventData.pressEventCamera);
        }

        private int FindCellIndex(Transform target)
        {
            if (target == null)
            {
                return -1;
            }

            Transform current = target;
            while (current != null)
            {
                int imageIndex = FindCellIndexInIcons(current);
                if (imageIndex >= 0)
                {
                    return imageIndex;
                }

                if (_slotCells != null)
                {
                    for (int index = 0; index < _slotCells.Length; index++)
                    {
                        if (_slotCells[index] != null && current == _slotCells[index].transform)
                        {
                            return index;
                        }
                    }
                }

                current = current.parent;
            }

            return -1;
        }

        private int FindCellIndexAtScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            int imageIndex = FindCellIconIndexAtScreenPoint(screenPoint, eventCamera);
            if (imageIndex >= 0)
            {
                return imageIndex;
            }

            if (_slotCells == null)
            {
                return -1;
            }

            for (int index = 0; index < _slotCells.Length; index++)
            {
                Text cell = _slotCells[index];
                if (cell == null ||
                    cell.rectTransform == null ||
                    !RectTransformUtility.RectangleContainsScreenPoint(
                        cell.rectTransform,
                        screenPoint,
                        eventCamera))
                {
                    continue;
                }

                return index;
            }

            return -1;
        }

        private int FindCellIndexInIcons(Transform target)
        {
            if (target == null || _slotCellIcons == null)
            {
                return -1;
            }

            for (int index = 0; index < _slotCellIcons.Length; index++)
            {
                if (_slotCellIcons[index] != null && target == _slotCellIcons[index].transform)
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindCellIconIndexAtScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            if (_slotCellIcons == null)
            {
                return -1;
            }

            for (int index = 0; index < _slotCellIcons.Length; index++)
            {
                Image icon = _slotCellIcons[index];
                if (icon == null ||
                    icon.rectTransform == null ||
                    !RectTransformUtility.RectangleContainsScreenPoint(
                        icon.rectTransform,
                        screenPoint,
                        eventCamera))
                {
                    continue;
                }

                return index;
            }

            return -1;
        }

        private static int CompareLeftToRight(Button left, Button right)
        {
            float leftX = left != null ? left.transform.position.x : 0f;
            float rightX = right != null ? right.transform.position.x : 0f;
            int xCompare = leftX.CompareTo(rightX);
            if (xCompare != 0)
            {
                return xCompare;
            }

            int leftSibling = left != null ? left.transform.GetSiblingIndex() : 0;
            int rightSibling = right != null ? right.transform.GetSiblingIndex() : 0;
            return leftSibling.CompareTo(rightSibling);
        }

        private Camera ResolveEventCamera()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera;
        }

        private void CacheDefaultsIfNeeded()
        {
            if (_hasCachedDefaults)
            {
                return;
            }

            if (_slotCells == null)
            {
                _slotCellDefaultColors = Array.Empty<Color>();
            }
            else
            {
                _slotCellDefaultColors = new Color[_slotCells.Length];
                for (int index = 0; index < _slotCells.Length; index++)
                {
                    _slotCellDefaultColors[index] = _slotCells[index] != null
                        ? _slotCells[index].color
                        : Color.white;
                }
            }

            _hasCachedDefaults = true;
        }

        private void ResetSlotCellColors()
        {
            if (_slotCells == null)
            {
                return;
            }

            for (int index = 0; index < _slotCells.Length; index++)
            {
                if (_slotCells[index] == null)
                {
                    continue;
                }

                _slotCells[index].color = _slotCellDefaultColors != null && index < _slotCellDefaultColors.Length
                    ? _slotCellDefaultColors[index]
                    : Color.white;
            }
        }

        private void UpdatePatternCue(int[] highlightedCellIndices, bool pendingPatternCue)
        {
            if (!pendingPatternCue ||
                highlightedCellIndices == null ||
                highlightedCellIndices.Length == 0)
            {
                StopPatternCue();
                return;
            }

            if (_activePatternCuePending &&
                AreSameCellIndices(_activePatternCueIndices, highlightedCellIndices))
            {
                return;
            }

            StopPatternCue(hideOverlays: false);
            RefreshSlotCellVisualIcons();

            for (int index = 0; index < highlightedCellIndices.Length; index++)
            {
                Transform target = ResolvePatternCueTarget(highlightedCellIndices[index]);
                if (target == null)
                {
                    continue;
                }

                Vector3 defaultPosition = target.localPosition;
                Vector3 defaultScale = target.localScale;
                Quaternion defaultRotation = target.localRotation;
                Tween tween = CreatePatternCueTween(
                    target,
                    defaultPosition,
                    defaultScale,
                    defaultRotation,
                    _patternCueMotion);
                if (tween == null)
                {
                    continue;
                }

                _patternCueTargets.Add(target);
                _patternCueDefaultLocalPositions.Add(defaultPosition);
                _patternCueDefaultLocalScales.Add(defaultScale);
                _patternCueDefaultLocalRotations.Add(defaultRotation);
                _patternCueTweens.Add(tween);
            }

            _activePatternCueIndices = CloneCellIndices(highlightedCellIndices);
            _activePatternCuePending = _patternCueTweens.Count > 0;
        }

        private Transform ResolvePatternCueTarget(int cellIndex)
        {
            if (SlotSpinResult.IsValidIndex(cellIndex) &&
                _slotCellVisualIcons != null &&
                cellIndex < _slotCellVisualIcons.Length &&
                _slotCellVisualIcons[cellIndex] != null)
            {
                return _slotCellVisualIcons[cellIndex].transform;
            }

            if (SlotSpinResult.IsValidIndex(cellIndex) &&
                !_slotCellIconsAreHitTargets &&
                _slotCellIcons != null &&
                cellIndex < _slotCellIcons.Length &&
                _slotCellIcons[cellIndex] != null)
            {
                return _slotCellIcons[cellIndex].transform;
            }

            if (SlotSpinResult.IsValidIndex(cellIndex) &&
                _slotCells != null &&
                cellIndex < _slotCells.Length &&
                _slotCells[cellIndex] != null)
            {
                return _slotCells[cellIndex].transform;
            }

            return null;
        }

        private static Tween CreatePatternCueTween(
            Transform target,
            Vector3 defaultPosition,
            Vector3 defaultScale,
            Quaternion defaultRotation,
            PatternCueMotion motion)
        {
            if (target == null)
            {
                return null;
            }

            target.localPosition = defaultPosition;
            target.localScale = defaultScale;
            target.localRotation = defaultRotation;
            switch (motion)
            {
                case PatternCueMotion.SlotSettle:
                    return CreateSlotSettleCueTween(target, defaultPosition, defaultScale);
                case PatternCueMotion.RisePop:
                    return CreateRisePopCueTween(target, defaultPosition, defaultScale);
                case PatternCueMotion.TiltPulse:
                default:
                    return CreateTiltPulseCueTween(target, defaultScale, defaultRotation);
            }
        }

        private static Tween CreateTiltPulseCueTween(
            Transform target,
            Vector3 defaultScale,
            Quaternion defaultRotation)
        {
            const float pulseScale = 1.08f;
            const float tiltDegrees = 5f;
            Vector3 largeScale = new(
                defaultScale.x * pulseScale,
                defaultScale.y * pulseScale,
                defaultScale.z);
            Vector3 smallScale = new(
                defaultScale.x * 1.03f,
                defaultScale.y * 1.03f,
                defaultScale.z);
            Vector3 defaultEuler = defaultRotation.eulerAngles;
            Vector3 leftTilt = new(
                defaultEuler.x,
                defaultEuler.y,
                defaultEuler.z - tiltDegrees);
            Vector3 rightTilt = new(
                defaultEuler.x,
                defaultEuler.y,
                defaultEuler.z + tiltDegrees);

            Sequence sequence = CreateLoopingCueSequence();
            sequence.Append(
                target
                    .DOScale(largeScale, 0.1f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true));
            sequence.Join(
                target
                    .DOLocalRotate(leftTilt, 0.1f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true));
            sequence.Append(
                target
                    .DOLocalRotate(rightTilt, 0.14f)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true));
            sequence.Join(
                target
                    .DOScale(smallScale, 0.14f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true));
            sequence.Append(
                target
                    .DOLocalRotate(defaultEuler, 0.13f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true));
            sequence.Join(
                target
                    .DOScale(defaultScale, 0.13f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true));
            sequence.AppendInterval(0.2f);
            return sequence;
        }

        private static Tween CreateSlotSettleCueTween(
            Transform target,
            Vector3 defaultPosition,
            Vector3 defaultScale)
        {
            const float drop = 3f;
            const float rise = 2f;
            Vector3 droppedPosition = new(
                defaultPosition.x,
                defaultPosition.y - drop,
                defaultPosition.z);
            Vector3 raisedPosition = new(
                defaultPosition.x,
                defaultPosition.y + rise,
                defaultPosition.z);
            Vector3 squashScale = new(
                defaultScale.x * 1.08f,
                defaultScale.y * 0.94f,
                defaultScale.z);
            Vector3 stretchScale = new(
                defaultScale.x * 0.98f,
                defaultScale.y * 1.07f,
                defaultScale.z);

            Sequence sequence = CreateLoopingCueSequence();
            sequence.Append(
                target
                    .DOScale(squashScale, 0.1f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true));
            sequence.Join(
                target
                    .DOLocalMove(droppedPosition, 0.1f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true));
            sequence.Append(
                target
                    .DOScale(stretchScale, 0.12f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true));
            sequence.Join(
                target
                    .DOLocalMove(raisedPosition, 0.12f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true));
            sequence.Append(
                target
                    .DOScale(defaultScale, 0.16f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true));
            sequence.Join(
                target
                    .DOLocalMove(defaultPosition, 0.16f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true));
            sequence.AppendInterval(0.18f);
            return sequence;
        }

        private static Tween CreateRisePopCueTween(
            Transform target,
            Vector3 defaultPosition,
            Vector3 defaultScale)
        {
            const float lift = 4f;
            const float pulseScale = 1.1f;
            Vector3 liftedPosition = new(
                defaultPosition.x,
                defaultPosition.y + lift,
                defaultPosition.z);
            Vector3 largeScale = new(
                defaultScale.x * pulseScale,
                defaultScale.y * pulseScale,
                defaultScale.z);

            Sequence sequence = CreateLoopingCueSequence();
            sequence.Append(
                target
                    .DOScale(largeScale, 0.12f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true));
            sequence.Join(
                target
                    .DOLocalMove(liftedPosition, 0.12f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true));
            sequence.Append(
                target
                    .DOScale(defaultScale, 0.2f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true));
            sequence.Join(
                target
                    .DOLocalMove(defaultPosition, 0.2f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true));
            sequence.AppendInterval(0.22f);
            return sequence;
        }

        private static Sequence CreateLoopingCueSequence()
        {
            return DOTween.Sequence()
                .SetUpdate(true)
                .SetLoops(-1, LoopType.Restart);
        }

        private void StopPatternCue(bool hideOverlays = true)
        {
            for (int index = 0; index < _patternCueTweens.Count; index++)
            {
                Tween tween = _patternCueTweens[index];
                if (tween != null && tween.IsActive())
                {
                    tween.Kill();
                }
            }

            int restoreCount = Mathf.Min(
                _patternCueTargets.Count,
                _patternCueDefaultLocalPositions.Count);
            for (int index = 0; index < restoreCount; index++)
            {
                Transform target = _patternCueTargets[index];
                if (target != null)
                {
                    target.localPosition = _patternCueDefaultLocalPositions[index];
                    if (index < _patternCueDefaultLocalScales.Count)
                    {
                        target.localScale = _patternCueDefaultLocalScales[index];
                    }

                    if (index < _patternCueDefaultLocalRotations.Count)
                    {
                        target.localRotation = _patternCueDefaultLocalRotations[index];
                    }
                }
            }

            if (hideOverlays)
            {
                RestoreHighlightedVisualSprites();
            }

            if (hideOverlays && _slotCellHighlightOverlays != null)
            {
                for (int index = 0; index < _slotCellHighlightOverlays.Length; index++)
                {
                    Image overlay = _slotCellHighlightOverlays[index];
                    if (overlay != null)
                    {
                        overlay.enabled = false;
                        overlay.sprite = null;
                        overlay.color = Color.white;
                        overlay.rectTransform.localScale = Vector3.one;
                    }
                }
            }

            _patternCueTweens.Clear();
            _patternCueTargets.Clear();
            _patternCueDefaultLocalPositions.Clear();
            _patternCueDefaultLocalScales.Clear();
            _patternCueDefaultLocalRotations.Clear();
            _activePatternCueIndices = Array.Empty<int>();
            _activePatternCuePending = false;
        }

        private static bool AreSameCellIndices(int[] left, int[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static int[] CloneCellIndices(int[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<int>();
            }

            var result = new int[source.Length];
            Array.Copy(source, result, source.Length);
            return result;
        }

        private sealed class SlotCellSwapInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private RunBattleSlotBoardView _owner;
            private int _cellIndex = -1;

            internal void Bind(RunBattleSlotBoardView owner, int cellIndex)
            {
                _owner = owner;
                _cellIndex = cellIndex;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
            }

            public void OnDrag(PointerEventData eventData)
            {
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                _owner?.HandleSlotCellDragged(_cellIndex, eventData);
            }
        }
    }
}
