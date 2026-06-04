using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class PatternPresentationView : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image _panelImage;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Text _bonusText;
        [SerializeField] private Text[] _slotCells;
        [SerializeField] private Image[] _slotCellIcons;
        [SerializeField] private Color _highlightColor = new Color(1f, 0.82f, 0.23f, 1f);
        [SerializeField] private Color _panelColor = new Color(0.16f, 0.2f, 0.28f, 0.96f);
        [SerializeField] private Color _finalePanelColor = new Color(0.45f, 0.32f, 0.08f, 0.98f);
        [SerializeField] private float _highlightScale = 1.08f;
        [SerializeField] private float _highlightDuration = 0.12f;
        [SerializeField] private float _introDuration = 0.12f;
        [SerializeField] private float _holdDuration = 0.38f;
        [SerializeField] private float _finaleHoldDuration = 0.62f;
        [SerializeField] private float _outroDuration = 0.08f;

        public void Bind(
            Text[] slotCells,
            RectTransform panel,
            Image panelImage,
            Text titleText,
            Text descriptionText,
            Text bonusText,
            Image[] slotCellIcons = null)
        {
            _slotCells = slotCells;
            _slotCellIcons = slotCellIcons;
            _panel = panel;
            _panelImage = panelImage;
            _titleText = titleText;
            _descriptionText = descriptionText;
            _bonusText = bonusText;
        }

        public IEnumerator Play(SlotPatternPresentationResult result, Func<bool> shouldSkip)
        {
            if (result == null)
            {
                yield break;
            }

            EnsurePanel();
            CacheSlotCellDefaultsIfNeeded();
            SetText(_titleText, result.PatternName);
            SetText(_descriptionText, result.Description);
            SetText(_bonusText, result.BonusText);

            if (_panelImage != null)
            {
                _panelImage.color = result.IsFinale ? _finalePanelColor : _panelColor;
            }

            gameObject.SetActive(true);
            ApplyHighlights(result.HighlightedCellIndices);

            yield return PlayScaleTween(new Vector3(0.9f, 0.9f, 1f), Vector3.one, _introDuration, Ease.OutBack, shouldSkip);
            yield return WaitOrSkip(result.IsFinale ? _finaleHoldDuration : _holdDuration, shouldSkip);
            yield return PlayScaleTween(Vector3.one, new Vector3(0.96f, 0.96f, 1f), _outroDuration, Ease.OutCubic, shouldSkip);

            HideImmediate();
        }

        public void HideImmediate()
        {
            EnsurePanel();
            KillActiveTween();
            ResetHighlights();

            if (_panel != null)
            {
                _panel.localScale = Vector3.one;
            }

            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            KillActiveTween();
        }

        private void EnsurePanel()
        {
            if (_panel == null)
            {
                _panel = transform as RectTransform;
            }
        }

        private IEnumerator PlayScaleTween(Vector3 from, Vector3 to, float duration, Ease ease, Func<bool> shouldSkip)
        {
            EnsurePanel();

            if (_panel == null)
            {
                yield break;
            }

            if (duration <= 0f || IsSkipped(shouldSkip))
            {
                _panel.localScale = to;
                yield break;
            }

            _panel.localScale = from;
            yield return PlayTween(_panel.DOScale(to, duration).SetEase(ease).SetUpdate(true), shouldSkip);
        }

        private IEnumerator WaitOrSkip(float duration, Func<bool> shouldSkip)
        {
            if (duration <= 0f || IsSkipped(shouldSkip))
            {
                yield break;
            }

            yield return PlayTween(DOVirtual.DelayedCall(duration, NoOp).SetUpdate(true), shouldSkip);
        }

        private void ApplyHighlights(int[] indices)
        {
            if ((_slotCells == null && _slotCellIcons == null) || indices == null)
            {
                return;
            }

            for (int index = 0; index < indices.Length; index++)
            {
                int cellIndex = indices[index];

                if (_slotCells != null && cellIndex >= 0 && cellIndex < _slotCells.Length && _slotCells[cellIndex] != null)
                {
                    _slotCells[cellIndex].color = _highlightColor;
                    PlayHighlightScale(_slotCells[cellIndex].transform, _highlightScale);
                }

                if (_slotCellIcons != null && cellIndex >= 0 && cellIndex < _slotCellIcons.Length && _slotCellIcons[cellIndex] != null)
                {
                    _slotCellIcons[cellIndex].color = _highlightColor;
                    PlayHighlightScale(_slotCellIcons[cellIndex].transform, _highlightScale);
                }
            }
        }

        private void ResetHighlights()
        {
            if (_slotCells == null && _slotCellIcons == null)
            {
                return;
            }

            if (_slotCells != null)
            {
                for (int index = 0; index < _slotCells.Length; index++)
                {
                    if (_slotCells[index] == null)
                    {
                        continue;
                    }

                    _slotCells[index].color = _slotCellDefaultColors != null && index < _slotCellDefaultColors.Length
                        ? _slotCellDefaultColors[index]
                        : Color.white;
                    _slotCells[index].transform.DOKill();
                    _slotCells[index].transform.localScale =
                        _slotCellDefaultScales != null && index < _slotCellDefaultScales.Length
                            ? _slotCellDefaultScales[index]
                            : Vector3.one;
                }
            }

            if (_slotCellIcons != null)
            {
                for (int index = 0; index < _slotCellIcons.Length; index++)
                {
                    if (_slotCellIcons[index] == null)
                    {
                        continue;
                    }

                    _slotCellIcons[index].color = _slotCellIconDefaultColors != null && index < _slotCellIconDefaultColors.Length
                        ? _slotCellIconDefaultColors[index]
                        : Color.white;
                    _slotCellIcons[index].transform.DOKill();
                    _slotCellIcons[index].transform.localScale =
                        _slotCellIconDefaultScales != null && index < _slotCellIconDefaultScales.Length
                            ? _slotCellIconDefaultScales[index]
                            : Vector3.one;
                }
            }
        }

        private void PlayHighlightScale(Transform target, float scale)
        {
            if (target == null)
            {
                return;
            }

            float appliedScale = target.gameObject.name.StartsWith("Slot Cell Icon")
                ? Mathf.Min(scale, 1.08f)
                : scale;
            target.DOKill();
            target
                .DOScale(new Vector3(appliedScale, appliedScale, 1f), _highlightDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        private void CacheSlotCellDefaultsIfNeeded()
        {
            if (_hasCachedSlotCellDefaults)
            {
                return;
            }

            if (_slotCells != null)
            {
                _slotCellDefaultColors = new Color[_slotCells.Length];
                _slotCellDefaultScales = new Vector3[_slotCells.Length];

                for (int index = 0; index < _slotCells.Length; index++)
                {
                    _slotCellDefaultColors[index] = _slotCells[index] != null ? _slotCells[index].color : Color.white;
                    _slotCellDefaultScales[index] = _slotCells[index] != null ? _slotCells[index].transform.localScale : Vector3.one;
                }
            }

            if (_slotCellIcons != null)
            {
                _slotCellIconDefaultColors = new Color[_slotCellIcons.Length];
                _slotCellIconDefaultScales = new Vector3[_slotCellIcons.Length];

                for (int index = 0; index < _slotCellIcons.Length; index++)
                {
                    _slotCellIconDefaultColors[index] = _slotCellIcons[index] != null ? _slotCellIcons[index].color : Color.white;
                    _slotCellIconDefaultScales[index] = _slotCellIcons[index] != null ? _slotCellIcons[index].transform.localScale : Vector3.one;
                }
            }

            _hasCachedSlotCellDefaults = true;
        }

        private IEnumerator PlayTween(Tween tween, Func<bool> shouldSkip)
        {
            if (tween == null)
            {
                yield break;
            }

            _activeTween = tween;

            while (tween.IsActive() && !tween.IsComplete() && !IsSkipped(shouldSkip))
            {
                yield return null;
            }

            if (tween.IsActive() && !tween.IsComplete())
            {
                tween.Complete();
            }

            if (_activeTween == tween)
            {
                _activeTween = null;
            }
        }

        private static bool IsSkipped(Func<bool> shouldSkip)
        {
            return shouldSkip != null && shouldSkip();
        }

        private void KillActiveTween()
        {
            if (_activeTween != null && _activeTween.IsActive())
            {
                _activeTween.Kill();
            }

            _activeTween = null;

            if (_panel != null)
            {
                _panel.DOKill();
            }
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void NoOp()
        {
        }

        private Color[] _slotCellDefaultColors;
        private Color[] _slotCellIconDefaultColors;
        private Vector3[] _slotCellDefaultScales;
        private Vector3[] _slotCellIconDefaultScales;
        private Tween _activeTween;
        private bool _hasCachedSlotCellDefaults;
    }
}
