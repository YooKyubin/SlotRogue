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
    /// (PatternPresentationDirector / FinalResultDirector) keep highlighting the same cells unchanged.
    ///
    /// Reels are built once and reused. During a spin the cells already hold the final symbols and
    /// are simply covered by the reels; when a reel stops the result is revealed seamlessly.
    /// </summary>
    public sealed class SlotMachineSpinDirector : MonoBehaviour
    {
        private const int Columns = SlotSpinResult.Columns;
        private const int VisibleRows = SlotSpinResult.Rows;

        [Header("Authored reels")]
        [Tooltip("Place the reel overlay in the scene and assign its container + 5 reels here.")]
        [SerializeField] private RectTransform _overlay;
        [SerializeField] private SlotReelView[] _reels;

        [Header("Timing")]
        [SerializeField] private float _spinUpDuration = 0.5f;
        [SerializeField] private float _stopIntervalPerColumn = 0.18f;

        // Needs symbol sprites plus reel objects placed in the hierarchy.
        public bool IsReady =>
            _symbolSprites != null && _symbolSprites.Length > 0 && HasAuthoredReels();

        public event Action<int> ReelStopped;

        private bool ReelsAreDisplay => _authored || HasAuthoredReels();

        public void Initialize(Image[] cellIcons, Sprite[] symbolSprites, Sprite[] spinSprites)
        {
            _cellIcons = cellIcons;
            _symbolSprites = symbolSprites;
            _spinSprites = spinSprites;
        }

        public bool TryGetVisibleCellIcons(out Image[] cellIcons)
        {
            cellIcons = null;

            if (_reels == null || _reels.Length < Columns)
            {
                return false;
            }

            var result = new Image[SlotSpinResult.CellCount];
            for (int column = 0; column < Columns; column++)
            {
                if (_reels[column] == null)
                {
                    return false;
                }

                for (int row = 0; row < VisibleRows; row++)
                {
                    Image icon = _reels[column].GetVisibleIcon(row);
                    if (icon == null)
                    {
                        return false;
                    }

                    result[SlotSpinResult.ToIndex(column, row)] = icon;
                }
            }

            cellIcons = result;
            return true;
        }

        /// <summary>
        /// Returns stable per-window hit targets (5 columns x visible rows) for swap input. Unlike
        /// <see cref="TryGetVisibleCellIcons"/>, these do not move when the reel recycles its symbol
        /// items, so swap input stays aligned with the visible cells after a spin.
        /// </summary>
        public bool TryGetWindowHitTargets(out Image[] targets)
        {
            targets = null;

            if (!HasAuthoredReels())
            {
                return false;
            }

            var result = new Image[SlotSpinResult.CellCount];
            for (int column = 0; column < Columns; column++)
            {
                if (_reels[column] == null)
                {
                    return false;
                }

                for (int row = 0; row < VisibleRows; row++)
                {
                    Image target = _reels[column].GetWindowHitTarget(row);
                    if (target == null)
                    {
                        return false;
                    }

                    result[SlotSpinResult.ToIndex(column, row)] = target;
                }
            }

            targets = result;
            return true;
        }

        public IEnumerator Play(SlotSpinResult result, Func<bool> shouldSkip)
        {
            if (result == null || !EnsureReels())
            {
                yield break;
            }

            // Hide the underlying cell icons so only the reels animate (the reel symbol sprites are
            // transparent, otherwise static cells would show through and look like a second slot).
            // In reel-display mode the cells stay hidden permanently — they can be deleted.
            HideCellIcons();
            ShowReels();
            ResetReelStopNotifications();
            if (IsSkipped(shouldSkip))
            {
                SetAllReelsImmediate(result);
                NotifyAllReelsStopped();
                FinishSpin();
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
                StartCoroutine(StopReelRoutine(column, result, shouldSkip));
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
                NotifyAllReelsStopped();
            }

            FinishSpin();
        }

        public void ShowImmediate(SlotSpinResult result)
        {
            if (result == null || !EnsureReels())
            {
                return;
            }

            if (ReelsAreDisplay)
            {
                HideCellIcons();
            }

            StopImmediate(result);

            if (ReelsAreDisplay)
            {
                ShowReels();
            }
        }

        private void SetAllReelsImmediate(SlotSpinResult result)
        {
            for (int column = 0; column < Columns; column++)
            {
                _reels[column].SetImmediate(ResultColumn(result, column));
            }
        }

        private IEnumerator StopReelRoutine(int column, SlotSpinResult result, Func<bool> shouldSkip)
        {
            yield return _reels[column].StopRoutine(ResultColumn(result, column), shouldSkip);
            NotifyReelStopped(column);
        }

        private void FinishSpin()
        {
            // The authored reels stay on showing the final result; nothing to reveal.
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

            // Reels are the slot face: keep them on, leave the hidden cell grid alone.
        }

        private void Awake()
        {
            // Authored reels are the slot face, so they stay visible between spins.
        }

        private IEnumerator Start()
        {
            // Wait one frame for the canvas to lay out, then arrange authored reels so the slot shows
            // at rest before any spin.
            yield return null;
            PrepareAuthoredRestDisplay();
        }

        private void OnEnable()
        {
            if (_built && ReelsAreDisplay)
            {
                ShowReels();
            }
        }

        private void PrepareAuthoredRestDisplay()
        {
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

            if (!IsReady)
            {
                Debug.LogError(
                    "[SlotMachineSpinDirector] Slot reel overlay, Reel 0-4, and symbol sprites must be wired in the inspector.",
                    this);
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

                _built = true;
                return true;
            }

            return false;
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

        private SlotSymbolType[] ResultColumn(SlotSpinResult result, int column)
        {
            var rows = new SlotSymbolType[VisibleRows];
            for (int row = 0; row < VisibleRows; row++)
            {
                rows[row] = result.GetSymbol(column, row);
            }

            return rows;
        }

        private void ShowReels()
        {
            // 이 director는 자기 GameObject(보통 "Slot Reel Overlay") 위에서 코루틴을 돌리므로,
            // host 자체를 반드시 활성화한다. 씬에서 비활성으로 저작됐거나 _overlay 참조가
            // host와 다른 오브젝트를 가리켜도, 릴(자식)과 코루틴이 정상 동작하게 한다.
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (_overlay != null)
            {
                _overlay.gameObject.SetActive(true);
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
            if (_reels == null)
            {
                return;
            }

            // 주의: 오버레이 GameObject 자체를 끄지 않는다. 이 director가 거기 붙어 있어서
            // GameObject를 끄면 다음 스핀에서 자기 코루틴(StartCoroutine)을 못 켠다.
            // 릴은 개별 SetVisible(false)로만 숨긴다(오버레이는 계속 활성 유지).
            for (int column = 0; column < Columns && column < _reels.Length; column++)
            {
                if (_reels[column] != null)
                {
                    _reels[column].StopImmediate();
                    _reels[column].SetVisible(false);
                }
            }
        }

        private void ResetReelStopNotifications()
        {
            if (_reelStopNotified == null || _reelStopNotified.Length != Columns)
            {
                _reelStopNotified = new bool[Columns];
                return;
            }

            for (int column = 0; column < Columns; column++)
            {
                _reelStopNotified[column] = false;
            }
        }

        private void NotifyAllReelsStopped()
        {
            for (int column = 0; column < Columns; column++)
            {
                NotifyReelStopped(column);
            }
        }

        private void NotifyReelStopped(int column)
        {
            if (column < 0 || column >= Columns)
            {
                return;
            }

            if (_reelStopNotified == null || _reelStopNotified.Length != Columns)
            {
                _reelStopNotified = new bool[Columns];
            }

            if (_reelStopNotified[column])
            {
                return;
            }

            _reelStopNotified[column] = true;
            ReelStopped?.Invoke(column);
        }

        private static bool IsSkipped(Func<bool> shouldSkip)
        {
            return shouldSkip != null && shouldSkip();
        }

        private Image[] _cellIcons;
        private Sprite[] _symbolSprites;
        private Sprite[] _spinSprites;
        private bool[] _reelStopNotified = new bool[Columns];
        private bool _authored;
        private bool _built;
    }
}
