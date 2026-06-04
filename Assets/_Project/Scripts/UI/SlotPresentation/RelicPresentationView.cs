using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class RelicPresentationView : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image _panelImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Text _valueText;
        [SerializeField] private RectTransform _originAnchor;
        [SerializeField] private Color _panelColor = new Color(0.12f, 0.16f, 0.24f, 0.98f);
        [SerializeField] private Color _valueColor = new Color(1f, 0.82f, 0.23f, 1f);
        [SerializeField] private Vector2 _fallbackOriginPosition = new Vector2(-385f, -735f);
        [SerializeField] private float _originScale = 0.18f;
        [SerializeField] private float _slideInDuration = 0.22f;
        [SerializeField] private float _bounceDuration = 0.1f;
        [SerializeField] private float _holdDuration = 0.58f;
        [SerializeField] private float _slideOutDuration = 0.16f;

        public void Bind(
            RectTransform panel,
            Image panelImage,
            Image iconImage,
            Text nameText,
            Text descriptionText,
            Text valueText,
            RectTransform originAnchor = null)
        {
            _panel = panel;
            _panelImage = panelImage;
            _iconImage = iconImage;
            _nameText = nameText;
            _descriptionText = descriptionText;
            _valueText = valueText;
            _originAnchor = originAnchor != null ? originAnchor : _originAnchor;
        }

        public IEnumerator Play(SlotRelicTriggerPresentationResult result, Func<bool> shouldSkip)
        {
            if (result == null)
            {
                yield break;
            }

            EnsurePanel();
            CachePositionsIfNeeded();
            SetText(_nameText, result.RelicName);
            SetText(_descriptionText, result.Description);
            SetText(_valueText, result.ValueText);

            if (_valueText != null)
            {
                _valueText.color = _valueColor;
            }

            if (_panelImage != null)
            {
                _panelImage.color = _panelColor;
            }

            if (_iconImage != null)
            {
                _iconImage.sprite = result.Icon;
                _iconImage.enabled = result.Icon != null;
                _iconImage.preserveAspect = true;
            }

            gameObject.SetActive(true);

            Vector2 originPosition = ResolveOriginPosition();
            Vector3 originScale = CreateOriginScale();
            _panel.anchoredPosition = originPosition;
            _panel.localScale = originScale;

            yield return PlayMoveAndScaleTween(originPosition, _shownPosition, originScale, Vector3.one, _slideInDuration, Ease.OutBack, shouldSkip);
            yield return PlayScaleTween(Vector3.one, new Vector3(1.06f, 1.06f, 1f), _bounceDuration, Ease.OutBack, shouldSkip);
            yield return PlayScaleTween(new Vector3(1.06f, 1.06f, 1f), Vector3.one, _bounceDuration, Ease.OutCubic, shouldSkip);
            yield return WaitOrSkip(_holdDuration, shouldSkip);
            yield return PlayMoveAndScaleTween(_shownPosition, ResolveOriginPosition(), Vector3.one, originScale, _slideOutDuration, Ease.InBack, shouldSkip);

            HideImmediate();
        }

        public void HideImmediate()
        {
            EnsurePanel();
            CachePositionsIfNeeded();
            KillActiveTween();

            if (_panel != null)
            {
                _panel.anchoredPosition = ResolveOriginPosition();
                _panel.localScale = CreateOriginScale();
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

        private void CachePositionsIfNeeded()
        {
            if (_hasCachedPositions || _panel == null)
            {
                return;
            }

            _shownPosition = _panel.anchoredPosition;
            _hasCachedPositions = true;
        }

        private IEnumerator PlayMoveAndScaleTween(
            Vector2 fromPosition,
            Vector2 toPosition,
            Vector3 fromScale,
            Vector3 toScale,
            float duration,
            Ease ease,
            Func<bool> shouldSkip)
        {
            if (_panel == null)
            {
                yield break;
            }

            if (duration <= 0f || IsSkipped(shouldSkip))
            {
                _panel.anchoredPosition = toPosition;
                _panel.localScale = toScale;
                yield break;
            }

            _panel.anchoredPosition = fromPosition;
            _panel.localScale = fromScale;

            Sequence sequence = DOTween.Sequence().SetTarget(_panel).SetUpdate(true);
            sequence.Join(
                DOTween.To(
                    () => _panel.anchoredPosition,
                    value => _panel.anchoredPosition = value,
                    toPosition,
                    duration).SetEase(ease).SetTarget(_panel).SetUpdate(true));
            sequence.Join(_panel.DOScale(toScale, duration).SetEase(ease).SetUpdate(true));

            yield return PlayTween(sequence, shouldSkip);
        }

        private IEnumerator PlayScaleTween(Vector3 from, Vector3 to, float duration, Ease ease, Func<bool> shouldSkip)
        {
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

        private Vector2 ResolveOriginPosition()
        {
            if (_originAnchor == null)
            {
                return _fallbackOriginPosition;
            }

            RectTransform parent = _panel != null ? _panel.parent as RectTransform : null;

            if (parent == null)
            {
                return _originAnchor.anchoredPosition;
            }

            if (_originAnchor.parent == parent)
            {
                return _originAnchor.anchoredPosition;
            }

            Vector3 worldCenter = _originAnchor.TransformPoint(_originAnchor.rect.center);
            Vector3 localCenter = parent.InverseTransformPoint(worldCenter);
            return new Vector2(localCenter.x, localCenter.y);
        }

        private Vector3 CreateOriginScale()
        {
            return new Vector3(_originScale, _originScale, 1f);
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

        private Vector2 _shownPosition;
        private Tween _activeTween;
        private bool _hasCachedPositions;
    }
}
