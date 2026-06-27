using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using SlotRogue.Slot.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation.Reel
{
    /// <summary>
    /// One slot column rendered as a vertically scrolling reel: this RectTransform is the
    /// Viewport (with a <see cref="RectMask2D"/>), a Content child holds a fixed pool of
    /// <see cref="SlotSymbolItemView"/> items that are recycled top &lt;-&gt; bottom so the reel
    /// appears to spin forever. The reel decelerates, snaps, and bounces onto the final rows.
    /// Geometry and sprite tables are supplied by <see cref="SlotMachineSpinDirector"/>.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SlotReelView : MonoBehaviour
    {
        private const int TopBufferCount = 2;
        private const int BottomBufferCount = 1;

        [Header("Layout (author in scene)")]
        [Tooltip("Number of symbols visible in the window. Slot board is 3 rows.")]
        [SerializeField] private int _visibleRows = SlotSpinResult.Rows;
        [Tooltip("Optional. Scrolling content holder. Auto-created under this reel if left empty.")]
        [SerializeField] private RectTransform _content;
        [Tooltip("Optional. Pooled symbol items. Auto-created if empty.")]
        [SerializeField] private List<SlotSymbolItemView> _items = new List<SlotSymbolItemView>();

        [Header("Spin")]
        [SerializeField] private float _cellsPerSecond = 24f;
        [SerializeField] private int _blurSteps = 10;

        [Header("Stop deceleration")]
        [SerializeField] private float _stepFastDuration = 0.028f;
        [SerializeField] private float _stepSlowDuration = 0.16f;
        [SerializeField] private float _bounceScale = 0.18f;
        [SerializeField] private float _bounceDuration = 0.2f;

        public bool IsBuilt => _items != null && _items.Count > 0;
        public bool IsStopped { get; private set; }
        public int VisibleRows => Mathf.Max(1, _visibleRows);

        public Image GetVisibleIcon(int row)
        {
            int itemIndex = TopBufferCount + row;
            if (row < 0 ||
                row >= VisibleRows ||
                _items == null ||
                itemIndex < 0 ||
                itemIndex >= _items.Count ||
                _items[itemIndex] == null)
            {
                return null;
            }

            return _items[itemIndex].Icon;
        }

        /// <summary>
        /// Prepares the reel for spinning. Geometry (cell size) is derived from this reel's own
        /// RectTransform so it can be sized and positioned in the scene by hand; the content holder
        /// and pooled symbol items are reused if authored, otherwise created on demand.
        /// </summary>
        public void Build(Sprite[] symbolSprites, Sprite[] spinSprites)
        {
            _symbolSprites = symbolSprites;
            _spinSprites = spinSprites != null && spinSprites.Length > 0 ? spinSprites : symbolSprites;

            if (GetComponent<RectMask2D>() == null)
            {
                gameObject.AddComponent<RectMask2D>();
            }

            EnsureLayout();
            IsStopped = true;
        }

        private void Start()
        {
            // Authored reels can be resized/moved in the scene after their items were placed. Re-lay
            // them out for the current rect so they show correctly at rest, before any spin. Skip when
            // the rect has no size yet — the presenter sizes such reels then calls PrepareLayout.
            if (_items != null && _items.Count > 0 && HasUsableRect())
            {
                EnsureLayout();
            }
        }

        /// <summary>Lays the reel out for its current rect without changing symbols. Used by the
        /// presenter once it has sized authored reels, so they display at rest before any spin.</summary>
        public void PrepareLayout()
        {
            if (GetComponent<RectMask2D>() == null)
            {
                gameObject.AddComponent<RectMask2D>();
            }

            EnsureLayout();
            IsStopped = true;
        }

        private bool HasUsableRect()
        {
            Rect rect = ((RectTransform)transform).rect;
            return rect.width > 1f && rect.height > 1f;
        }

        private void EnsureLayout()
        {
            EnsureContent();
            ComputeLayout();
            BuildItems();
            LayoutItemsAtRest();
        }

        public void SetImmediate(IReadOnlyList<SlotSymbolType> finalRows)
        {
            if (_content == null || !IsBuilt)
            {
                return;
            }

            StopSpinLoop();
            KillTweens();
            _content.anchoredPosition = Vector2.zero;
            LayoutItemsAtRest();
            ApplyWindowSymbols(finalRows);
            IsStopped = true;
        }

        public void StartSpin()
        {
            if (!IsBuilt)
            {
                return;
            }

            KillTweens();
            _content.anchoredPosition = Vector2.zero;
            FillAllWithBlur();
            IsStopped = false;
            StopSpinLoop();
            _spinning = true;
            _spinRoutine = StartCoroutine(SpinLoop());
        }

        public IEnumerator StopRoutine(IReadOnlyList<SlotSymbolType> finalRows, Func<bool> shouldSkip)
        {
            if (!IsBuilt)
            {
                yield break;
            }

            StopSpinLoop();

            if (IsSkipped(shouldSkip))
            {
                SetImmediate(finalRows);
                yield break;
            }

            SnapToGrid();

            IReadOnlyList<SlotSymbolType> resultOrder = SlotReelSpinPlan.OrderResultEntries(finalRows);

            // Result symbols enter at the very top of the strip, so after the last one enters it must
            // scroll down past the top buffer to land inside the visible window. Add that many trailing
            // blur steps; otherwise the result would settle above the window and SetImmediate would slam
            // it into view, looking like every symbol changes at once after the reel stops.
            int resultStart = _blurSteps;
            int resultEnd = _blurSteps + resultOrder.Count;
            int totalSteps = resultEnd + TopBufferCount;

            for (int step = 0; step < totalSteps; step++)
            {
                float t = totalSteps <= 1 ? 1f : (float)step / (totalSteps - 1);
                float duration = Mathf.Lerp(_stepFastDuration, _stepSlowDuration, t * t);

                bool isResult = step >= resultStart && step < resultEnd;
                Sprite sprite = isResult
                    ? SymbolSprite(resultOrder[step - resultStart])
                    : RandomBlurSprite();

                yield return StepDownOneCell(sprite, duration, shouldSkip);

                if (IsSkipped(shouldSkip))
                {
                    SetImmediate(finalRows);
                    yield break;
                }
            }

            yield return Bounce(shouldSkip);
            SetImmediate(finalRows);
        }

        public void StopImmediate()
        {
            StopSpinLoop();
            KillTweens();

            if (_content != null)
            {
                _content.anchoredPosition = Vector2.zero;
            }
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        private void OnDisable()
        {
            StopSpinLoop();
            KillTweens();
        }

        private IEnumerator SpinLoop()
        {
            while (_spinning)
            {
                float delta = _cellsPerSecond * _cellSize.y * Time.unscaledDeltaTime;
                MoveItemsDown(delta, wrapWithBlur: true);
                yield return null;
            }
        }

        private void MoveItemsDown(float distance, bool wrapWithBlur)
        {
            for (int index = 0; index < _items.Count; index++)
            {
                _items[index].OffsetLocalY(-distance);
            }

            WrapItems(wrapWithBlur, null);
        }

        private void WrapItems(bool wrapWithBlur, Sprite forcedSprite)
        {
            for (int index = 0; index < _items.Count; index++)
            {
                SlotSymbolItemView item = _items[index];
                if (item.LocalY < _wrapThreshold)
                {
                    item.OffsetLocalY(_stripHeight);
                    item.SetSprite(wrapWithBlur ? RandomBlurSprite() : forcedSprite);
                }
            }
        }

        private IEnumerator StepDownOneCell(Sprite incomingTopSprite, float duration, Func<bool> shouldSkip)
        {
            float moved = 0f;
            Tween tween = DOVirtual.Float(
                    0f,
                    _cellSize.y,
                    Mathf.Max(0.001f, duration),
                    value =>
                    {
                        float delta = value - moved;
                        moved = value;
                        for (int index = 0; index < _items.Count; index++)
                        {
                            _items[index].OffsetLocalY(-delta);
                        }
                    })
                .SetEase(Ease.Linear)
                .SetUpdate(true);

            yield return WaitTween(tween, shouldSkip);

            // Guarantee a full cell of travel, then recycle the item that fell below the strip.
            float remaining = _cellSize.y - moved;
            if (Mathf.Abs(remaining) > 0.0001f)
            {
                for (int index = 0; index < _items.Count; index++)
                {
                    _items[index].OffsetLocalY(-remaining);
                }
            }

            WrapItems(false, incomingTopSprite);
        }

        private IEnumerator Bounce(Func<bool> shouldSkip)
        {
            if (_content == null || _bounceDuration <= 0f || _bounceScale <= 0f)
            {
                yield break;
            }

            SetContentY(0f);
            float distance = _cellSize.y * _bounceScale;

            // Single damped dip (down, then settle back) using core DOTween only — the UI-module
            // shortcuts (DOPunchAnchorPos) live in Assembly-CSharp and aren't referenceable here.
            Tween tween = DOVirtual.Float(
                    0f,
                    1f,
                    _bounceDuration,
                    value => SetContentY(-distance * Mathf.Sin(value * Mathf.PI) * (1f - value)))
                .SetEase(Ease.Linear)
                .SetUpdate(true);

            yield return WaitTween(tween, shouldSkip);
            SetContentY(0f);
        }

        private void SetContentY(float y)
        {
            if (_content == null)
            {
                return;
            }

            Vector2 position = _content.anchoredPosition;
            position.y = y;
            _content.anchoredPosition = position;
        }

        private void SnapToGrid()
        {
            _items.Sort((left, right) => right.LocalY.CompareTo(left.LocalY));
            for (int index = 0; index < _items.Count; index++)
            {
                _items[index].SetLocalY(_slotRestY[index]);
            }
        }

        private void LayoutItemsAtRest()
        {
            for (int index = 0; index < _items.Count; index++)
            {
                _items[index].SetLocalY(_slotRestY[index]);
            }
        }

        private void ApplyWindowSymbols(IReadOnlyList<SlotSymbolType> finalRows)
        {
            if (finalRows == null)
            {
                return;
            }

            int visible = VisibleRows;
            for (int row = 0; row < visible; row++)
            {
                int slot = TopBufferCount + row;
                if (slot < _items.Count && row < finalRows.Count)
                {
                    _items[slot].SetSprite(SymbolSprite(finalRows[row]));
                }
            }

            for (int index = 0; index < _items.Count; index++)
            {
                if (index < TopBufferCount || index >= TopBufferCount + visible)
                {
                    _items[index].SetSprite(RandomBlurSprite());
                }
            }
        }

        private void FillAllWithBlur()
        {
            for (int index = 0; index < _items.Count; index++)
            {
                _items[index].SetSprite(RandomBlurSprite());
            }
        }

        private void EnsureContent()
        {
            if (_content != null)
            {
                return;
            }

            var existing = transform.Find("Content") as RectTransform;
            if (existing != null)
            {
                _content = existing;
                return;
            }

            var go = new GameObject("Content", typeof(RectTransform));
            _content = (RectTransform)go.transform;
            _content.SetParent(transform, false);
            _content.anchorMin = Vector2.zero;
            _content.anchorMax = Vector2.one;
            _content.pivot = new Vector2(0.5f, 0.5f);
            _content.offsetMin = Vector2.zero;
            _content.offsetMax = Vector2.zero;
            _content.anchoredPosition = Vector2.zero;
        }

        private void ComputeLayout()
        {
            int visible = VisibleRows;
            Rect rect = ((RectTransform)transform).rect;
            _cellSize = new Vector2(rect.width, visible > 0 ? rect.height / visible : rect.height);

            int itemCount = visible + TopBufferCount + BottomBufferCount;
            float topRowOffset = (visible - 1) * 0.5f;
            float topSlot = TopBufferCount + topRowOffset;

            _slotRestY = new float[itemCount];
            for (int index = 0; index < itemCount; index++)
            {
                _slotRestY[index] = (topSlot - index) * _cellSize.y;
            }

            _stripHeight = itemCount * _cellSize.y;
            _wrapThreshold = _slotRestY[itemCount - 1] - (_cellSize.y * 0.5f);
        }

        private void BuildItems()
        {
            int itemCount = _slotRestY.Length;

            if (_items == null)
            {
                _items = new List<SlotSymbolItemView>(itemCount);
            }

            // Drop any missing references (e.g. an authored item was deleted).
            _items.RemoveAll(item => item == null);

            while (_items.Count < itemCount)
            {
                SlotSymbolItemView item = SlotSymbolItemView.Create(_content, $"Symbol {_items.Count}", _cellSize);
                _items.Add(item);
            }

            // Re-parent authored items and size each icon from its Sprite native size.
            for (int index = 0; index < _items.Count; index++)
            {
                RectTransform itemRect = _items[index].RectTransform;
                if (itemRect.parent != _content)
                {
                    itemRect.SetParent(_content, false);
                }

                _items[index].ApplyNativeSize();
            }
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

        private Sprite RandomBlurSprite()
        {
            if (_spinSprites == null || _spinSprites.Length == 0)
            {
                return null;
            }

            return _spinSprites[UnityEngine.Random.Range(0, _spinSprites.Length)];
        }

        private void StopSpinLoop()
        {
            _spinning = false;

            if (_spinRoutine != null)
            {
                StopCoroutine(_spinRoutine);
                _spinRoutine = null;
            }
        }

        private void KillTweens()
        {
            if (_content != null)
            {
                _content.DOKill();
            }
        }

        private IEnumerator WaitTween(Tween tween, Func<bool> shouldSkip)
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

        private float[] _slotRestY;
        private Vector2 _cellSize;
        private float _stripHeight;
        private float _wrapThreshold;
        private Sprite[] _symbolSprites;
        private Sprite[] _spinSprites;
        private bool _spinning;
        private Coroutine _spinRoutine;
    }
}
