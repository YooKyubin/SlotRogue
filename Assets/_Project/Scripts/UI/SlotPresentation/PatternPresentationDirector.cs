using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class PatternPresentationDirector : MonoBehaviour
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

        // 정보 패널(이름/설명/보너스) 시각 요소가 하나라도 연결돼 있으면 '패널 모드'.
        // 강조 전용(패널 미사용)이면 자기 GameObject를 토글하지 않아, 어느 오브젝트에 둬도(공유해도) 안전하다.
        private bool HasInfoPanel =>
            _panelImage != null ||
            _titleText != null ||
            _descriptionText != null ||
            _bonusText != null;

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
            _hasCachedSlotCellDefaults = false;
        }

        public void SetSlotCellIcons(Image[] slotCellIcons)
        {
            if (ReferenceEquals(_slotCellIcons, slotCellIcons))
            {
                return;
            }

            ResetHighlights();
            _slotCellIcons = slotCellIcons;
            _slotCellIconDefaultColors = null;
            _slotCellIconDefaultScales = null;
            _hasCachedSlotCellDefaults = false;
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

            // 패널을 쓸 때만 자기 GameObject를 켠다. 강조 전용이면 토글하지 않는다
            // (강조는 외부 셀을 조작하므로 자기 활성 상태와 무관하고, 자기 토글은 같은 오브젝트의
            //  다른 컴포넌트를 죽일 수 있어 위험하다).
            if (HasInfoPanel)
            {
                gameObject.SetActive(true);
            }

            ApplyHighlights(result.HighlightedCellIndices);

            yield return PlayScaleTween(new Vector3(0.9f, 0.9f, 1f), Vector3.one, _introDuration, Ease.OutBack, shouldSkip);
            yield return WaitOrSkip(result.IsFinale ? _finaleHoldDuration : _holdDuration, shouldSkip);
            yield return RestoreHighlights(result.HighlightedCellIndices, shouldSkip);
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

            // 패널을 쓸 때만 자기 GameObject를 끈다(강조 전용이면 토글 불필요·위험).
            if (HasInfoPanel)
            {
                gameObject.SetActive(false);
            }
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
                    PlayHighlightScale(
                        _slotCells[cellIndex].transform,
                        _highlightScale,
                        GetDefaultScale(_slotCellDefaultScales, cellIndex));
                }

                if (_slotCellIcons != null && cellIndex >= 0 && cellIndex < _slotCellIcons.Length && _slotCellIcons[cellIndex] != null)
                {
                    _slotCellIcons[cellIndex].color = _highlightColor;
                    PlayHighlightScale(
                        _slotCellIcons[cellIndex].transform,
                        _highlightScale,
                        GetDefaultScale(_slotCellIconDefaultScales, cellIndex));
                }
            }
        }

        private IEnumerator RestoreHighlights(int[] indices, Func<bool> shouldSkip)
        {
            if (indices == null || indices.Length == 0)
            {
                yield break;
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            bool hasTarget = false;

            for (int index = 0; index < indices.Length; index++)
            {
                int cellIndex = indices[index];

                if (_slotCells != null &&
                    cellIndex >= 0 &&
                    cellIndex < _slotCells.Length &&
                    _slotCells[cellIndex] != null)
                {
                    Transform target = _slotCells[cellIndex].transform;
                    target.DOKill();
                    sequence.Join(
                        target
                            .DOScale(GetDefaultScale(_slotCellDefaultScales, cellIndex), _highlightDuration)
                            .SetEase(Ease.OutCubic)
                            .SetUpdate(true));
                    hasTarget = true;
                }

                if (_slotCellIcons != null &&
                    cellIndex >= 0 &&
                    cellIndex < _slotCellIcons.Length &&
                    _slotCellIcons[cellIndex] != null)
                {
                    Transform target = _slotCellIcons[cellIndex].transform;
                    target.DOKill();
                    sequence.Join(
                        target
                            .DOScale(GetDefaultScale(_slotCellIconDefaultScales, cellIndex), _highlightDuration)
                            .SetEase(Ease.OutCubic)
                            .SetUpdate(true));
                    hasTarget = true;
                }
            }

            if (hasTarget)
            {
                yield return PlayTween(sequence, shouldSkip);
            }
            else
            {
                sequence.Kill();
            }

            ResetHighlights();
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

        private void PlayHighlightScale(Transform target, float scale, Vector3 defaultScale)
        {
            if (target == null)
            {
                return;
            }

            float appliedScale = target.gameObject.name.StartsWith("Slot Cell Icon")
                ? Mathf.Min(scale, 1.08f)
                : scale;
            target.DOKill();
            Vector3 targetScale = new(
                defaultScale.x * appliedScale,
                defaultScale.y * appliedScale,
                defaultScale.z);
            target
                .DOScale(targetScale, _highlightDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        private static Vector3 GetDefaultScale(Vector3[] scales, int index)
        {
            return scales != null && index >= 0 && index < scales.Length
                ? scales[index]
                : Vector3.one;
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
