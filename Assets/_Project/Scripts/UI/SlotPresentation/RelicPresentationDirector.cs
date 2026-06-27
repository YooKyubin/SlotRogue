using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using SlotRogue.UI.GameFlow;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class RelicPresentationDirector : MonoBehaviour
    {
        private const string DefaultOriginAnchorName = "Relic Inventory Origin";

        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image _panelImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Text _valueText;
        [SerializeField] private RectTransform _originAnchor;
        [SerializeField] private string _originAnchorName = DefaultOriginAnchorName;
        [SerializeField] private Color _panelColor = new Color(0.12f, 0.16f, 0.24f, 0.98f);
        [SerializeField] private Color _valueColor = new Color(1f, 0.82f, 0.23f, 1f);
        [SerializeField] private Vector2 _fallbackOriginPosition = new Vector2(-385f, -735f);
        [SerializeField] private float _originScale = 0.18f;
        [SerializeField] private float _slideInDuration = 0.22f;
        [SerializeField] private float _bounceDuration = 0.1f;
        [SerializeField] private float _holdDuration = 0.58f;
        [SerializeField] private float _slideOutDuration = 0.16f;
        [SerializeField] private float _iconOnlySpread = 58f;
        [SerializeField] private float _iconOnlyShakeDuration = 0.18f;
        [SerializeField] private float _iconOnlyShakeAngle = 8f;
        [SerializeField] private float _iconFlightDuration = 0.34f;
        [SerializeField] private float _iconFlightStagger = 0.06f;
        [SerializeField] private float _iconFlightArcHeight = 128f;
        [SerializeField] private float _iconLaunchScale = 0.62f;
        [SerializeField] private float _iconCruiseScale = 1.12f;
        [SerializeField] private float _iconAbsorbScale = 0.24f;
        [SerializeField] private float _iconAbsorbDuration = 0.08f;

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
            RestoreIconOnlyState();
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

        public IEnumerator PlayIconOnly(
            IReadOnlyList<SlotRelicTriggerPresentationResult> results,
            Func<bool> shouldSkip)
        {
            yield return PlayIconFlyToResult(results, null, null, shouldSkip);
        }

        public IEnumerator PlayIconFlyToResult(
            IReadOnlyList<SlotRelicTriggerPresentationResult> results,
            RectTransform targetAnchor,
            Action<SlotRelicTriggerPresentationResult> onImpact,
            Func<bool> shouldSkip)
        {
            if (results == null || results.Count == 0 || _iconImage == null)
            {
                yield break;
            }

            List<RelicIconFlight> flights = CollectIconFlights(results);
            if (flights.Count == 0)
            {
                yield break;
            }

            EnsurePanel();
            CachePositionsIfNeeded();
            CacheIconDefaultsIfNeeded();
            RestoreIconOnlyState();
            EnterIconOnlyMode();
            SetGraphicEnabled(_iconImage, false);

            gameObject.SetActive(true);

            RectTransform animationParent = ResolveAnimationParent();
            Vector2 originPosition = ResolveOriginPosition(animationParent);
            Vector2 targetPosition = ResolveTargetPosition(animationParent, targetAnchor);

            Sequence masterSequence = DOTween.Sequence().SetTarget(this).SetUpdate(true);
            for (int index = 0; index < flights.Count; index++)
            {
                Sequence flightSequence = CreateIconFlightSequence(
                    flights[index],
                    index,
                    flights.Count,
                    animationParent,
                    originPosition,
                    targetPosition,
                    onImpact);
                if (flightSequence != null)
                {
                    masterSequence.Insert(index * Mathf.Max(0f, _iconFlightStagger), flightSequence);
                }
            }

            yield return PlayTween(masterSequence, shouldSkip);
            RestoreIconOnlyState();
            gameObject.SetActive(false);
        }

        public void HideImmediate()
        {
            EnsurePanel();
            CachePositionsIfNeeded();
            KillActiveTween();
            RestoreIconOnlyState();

            if (_panel != null)
            {
                _panel.anchoredPosition = ResolveOriginPosition();
                _panel.localScale = CreateOriginScale();
                _panel.localRotation = Quaternion.identity;
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

        private IEnumerator PlayRotationShakeTween(Func<bool> shouldSkip)
        {
            if (_panel == null)
            {
                yield break;
            }

            if (_iconOnlyShakeDuration <= 0f || IsSkipped(shouldSkip))
            {
                _panel.localRotation = Quaternion.identity;
                yield break;
            }

            Tween tween = _panel
                .DOShakeRotation(
                    _iconOnlyShakeDuration,
                    new Vector3(0f, 0f, _iconOnlyShakeAngle),
                    12,
                    90f,
                    false)
                .SetUpdate(true);
            yield return PlayTween(tween, shouldSkip);
            _panel.localRotation = Quaternion.identity;
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
            ResolveOriginAnchorIfNeeded();

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

        private Vector2 ResolveOriginPosition(RectTransform targetParent)
        {
            ResolveOriginAnchorIfNeeded();

            if (_originAnchor == null)
            {
                return ConvertPanelParentPosition(_fallbackOriginPosition, targetParent);
            }

            return ResolveRectCenter(_originAnchor, targetParent, _fallbackOriginPosition);
        }

        private void ResolveOriginAnchorIfNeeded()
        {
            if (_originAnchor != null)
            {
                return;
            }

            string originAnchorName = string.IsNullOrWhiteSpace(_originAnchorName)
                ? DefaultOriginAnchorName
                : _originAnchorName;
            Transform searchRoot = transform.root != null ? transform.root : transform;
            Transform found = SceneComponentResolver.FindDeepChild(searchRoot, originAnchorName);
            if (found is RectTransform rectTransform)
            {
                _originAnchor = rectTransform;
            }
        }

        private Vector2 ResolveTargetPosition(RectTransform targetParent, RectTransform targetAnchor)
        {
            if (targetAnchor != null)
            {
                return ResolveRectCenter(targetAnchor, targetParent, _shownPosition);
            }

            return ConvertPanelParentPosition(_shownPosition, targetParent);
        }

        private RectTransform ResolveAnimationParent()
        {
            if (_panel != null && _panel.parent is RectTransform panelParent)
            {
                return panelParent;
            }

            if (_iconImage != null && _iconImage.transform.parent is RectTransform iconParent)
            {
                return iconParent;
            }

            return transform as RectTransform;
        }

        private Vector2 ResolveRectCenter(
            RectTransform source,
            RectTransform targetParent,
            Vector2 fallbackPosition)
        {
            if (source == null)
            {
                return ConvertPanelParentPosition(fallbackPosition, targetParent);
            }

            if (targetParent == null)
            {
                return source.anchoredPosition;
            }

            Vector3 worldCenter = source.TransformPoint(source.rect.center);
            Vector3 localCenter = targetParent.InverseTransformPoint(worldCenter);
            return new Vector2(localCenter.x, localCenter.y);
        }

        private Vector2 ConvertPanelParentPosition(Vector2 position, RectTransform targetParent)
        {
            RectTransform panelParent = _panel != null ? _panel.parent as RectTransform : null;
            if (panelParent == null || targetParent == null || panelParent == targetParent)
            {
                return position;
            }

            Vector3 worldPosition = panelParent.TransformPoint(new Vector3(position.x, position.y, 0f));
            Vector3 localPosition = targetParent.InverseTransformPoint(worldPosition);
            return new Vector2(localPosition.x, localPosition.y);
        }

        private Vector3 CreateOriginScale()
        {
            return new Vector3(_originScale, _originScale, 1f);
        }

        private Sequence CreateIconFlightSequence(
            RelicIconFlight flight,
            int index,
            int count,
            RectTransform animationParent,
            Vector2 originPosition,
            Vector2 targetPosition,
            Action<SlotRelicTriggerPresentationResult> onImpact)
        {
            Image icon = CreateIconClone(animationParent);
            if (icon == null || icon.transform is not RectTransform iconTransform)
            {
                return null;
            }

            icon.sprite = flight.Sprite;
            icon.enabled = true;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            float centerOffset = index - (count - 1) * 0.5f;
            Vector2 startPosition = originPosition + new Vector2(centerOffset * 16f, 0f);
            Vector2 arrivalPosition = targetPosition + new Vector2(centerOffset * 18f, (index % 2 == 0 ? 1f : -1f) * 10f);
            Vector2 controlPosition = (startPosition + arrivalPosition) * 0.5f +
                new Vector2(centerOffset * 24f, Mathf.Max(0f, _iconFlightArcHeight));

            Color visibleColor = _iconImage.color;
            if (visibleColor.a <= 0.01f)
            {
                visibleColor.a = 1f;
            }

            Color hiddenColor = visibleColor;
            hiddenColor.a = 0f;

            icon.color = hiddenColor;
            iconTransform.anchorMin = new Vector2(0.5f, 0.5f);
            iconTransform.anchorMax = new Vector2(0.5f, 0.5f);
            iconTransform.pivot = new Vector2(0.5f, 0.5f);
            iconTransform.anchoredPosition = startPosition;
            iconTransform.localScale = CreateIconScale(_iconLaunchScale);
            iconTransform.localRotation = Quaternion.Euler(0f, 0f, centerOffset * -5f);

            float flightDuration = Mathf.Max(0.01f, _iconFlightDuration);
            float absorbDuration = Mathf.Max(0.01f, _iconAbsorbDuration);

            Sequence sequence = DOTween.Sequence().SetTarget(iconTransform).SetUpdate(true);
            sequence.Append(
                DOTween.To(
                        () => 0f,
                        value =>
                        {
                            float easedValue = Mathf.SmoothStep(0f, 1f, value);
                            iconTransform.anchoredPosition = EvaluateQuadraticBezier(
                                startPosition,
                                controlPosition,
                                arrivalPosition,
                                easedValue);
                        },
                        1f,
                        flightDuration)
                    .SetEase(Ease.Linear)
                    .SetTarget(iconTransform)
                    .SetUpdate(true));
            sequence.Join(CreateColorTween(icon, visibleColor, Mathf.Min(0.08f, flightDuration)));
            sequence.Insert(
                0f,
                iconTransform
                    .DOScale(CreateIconScale(_iconCruiseScale), flightDuration * 0.48f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true));
            sequence.Insert(
                flightDuration * 0.48f,
                iconTransform
                    .DOScale(CreateIconScale(0.88f), flightDuration * 0.52f)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true));
            sequence.Insert(
                0f,
                iconTransform
                    .DOLocalRotate(new Vector3(0f, 0f, centerOffset * 9f), flightDuration)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true));
            sequence.AppendCallback(() => onImpact?.Invoke(flight.Result));
            sequence.Append(
                DOTween.To(
                        () => iconTransform.anchoredPosition,
                        value => iconTransform.anchoredPosition = value,
                        targetPosition,
                        absorbDuration)
                    .SetEase(Ease.InCubic)
                    .SetTarget(iconTransform)
                    .SetUpdate(true));
            sequence.Join(
                iconTransform
                    .DOScale(CreateIconScale(_iconAbsorbScale), absorbDuration)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true));
            sequence.Join(CreateColorTween(icon, hiddenColor, absorbDuration));

            return sequence;
        }

        private List<RelicIconFlight> CollectIconFlights(
            IReadOnlyList<SlotRelicTriggerPresentationResult> results)
        {
            var flights = new List<RelicIconFlight>(results.Count);
            for (int index = 0; index < results.Count; index++)
            {
                SlotRelicTriggerPresentationResult result = results[index];
                if (result?.Icon != null)
                {
                    flights.Add(new RelicIconFlight(result, result.Icon));
                }
            }

            return flights;
        }

        private Vector3 CreateIconScale(float factor)
        {
            Vector3 scale = _hasCachedIconDefaults ? _iconDefaultScale : Vector3.one;
            return new Vector3(scale.x * factor, scale.y * factor, scale.z);
        }

        private Tween CreateColorTween(Image image, Color targetColor, float duration)
        {
            if (image == null)
            {
                return null;
            }

            return DOTween.To(
                    () => image.color,
                    value => image.color = value,
                    targetColor,
                    duration)
                .SetTarget(image)
                .SetUpdate(true);
        }

        private static Vector2 EvaluateQuadraticBezier(
            Vector2 start,
            Vector2 control,
            Vector2 end,
            float value)
        {
            float inverseValue = 1f - value;
            return inverseValue * inverseValue * start +
                2f * inverseValue * value * control +
                value * value * end;
        }

        private List<Sprite> CollectIconSprites(
            IReadOnlyList<SlotRelicTriggerPresentationResult> results)
        {
            var sprites = new List<Sprite>(results.Count);
            for (int index = 0; index < results.Count; index++)
            {
                Sprite sprite = results[index]?.Icon;
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
            }

            return sprites;
        }

        private void ApplyIconSprites(IReadOnlyList<Sprite> sprites)
        {
            DestroyIconClones();

            for (int index = 0; index < sprites.Count; index++)
            {
                Image image = index == 0 ? _iconImage : CreateIconClone();
                if (image == null)
                {
                    continue;
                }

                image.sprite = sprites[index];
                image.enabled = sprites[index] != null;
                image.preserveAspect = true;

                if (image.transform is RectTransform rectTransform)
                {
                    float xOffset = (index - (sprites.Count - 1) * 0.5f) * _iconOnlySpread;
                    rectTransform.anchoredPosition =
                        _iconDefaultAnchoredPosition + new Vector2(xOffset, 0f);
                    rectTransform.localScale = _iconDefaultScale;
                    rectTransform.localRotation = _iconDefaultRotation;
                }
            }
        }

        private Image CreateIconClone()
        {
            Image clone = CreateIconClone(_iconImage.transform.parent as RectTransform);
            return clone;
        }

        private Image CreateIconClone(RectTransform parent)
        {
            Image clone = Instantiate(
                _iconImage,
                parent != null ? parent : _iconImage.transform.parent,
                false);
            _iconOnlyClones.Add(clone);
            return clone;
        }

        private void DestroyIconClones()
        {
            for (int index = 0; index < _iconOnlyClones.Count; index++)
            {
                Image clone = _iconOnlyClones[index];
                if (clone != null)
                {
                    Destroy(clone.gameObject);
                }
            }

            _iconOnlyClones.Clear();
        }

        private void CacheIconDefaultsIfNeeded()
        {
            if (_hasCachedIconDefaults || _iconImage == null)
            {
                return;
            }

            _iconDefaultSprite = _iconImage.sprite;
            _iconDefaultEnabled = _iconImage.enabled;

            if (_iconImage.transform is RectTransform rectTransform)
            {
                _iconDefaultAnchoredPosition = rectTransform.anchoredPosition;
                _iconDefaultScale = rectTransform.localScale;
                _iconDefaultRotation = rectTransform.localRotation;
            }

            _hasCachedIconDefaults = true;
        }

        private void EnterIconOnlyMode()
        {
            CacheContentStateIfNeeded();
            SetGraphicEnabled(_panelImage, false);
            SetTextActive(_nameText, false);
            SetTextActive(_descriptionText, false);
            SetTextActive(_valueText, false);
        }

        private void RestoreIconOnlyState()
        {
            DestroyIconClones();

            if (_hasCachedIconDefaults && _iconImage != null)
            {
                _iconImage.sprite = _iconDefaultSprite;
                _iconImage.enabled = _iconDefaultEnabled;

                if (_iconImage.transform is RectTransform rectTransform)
                {
                    rectTransform.anchoredPosition = _iconDefaultAnchoredPosition;
                    rectTransform.localScale = _iconDefaultScale;
                    rectTransform.localRotation = _iconDefaultRotation;
                }
            }

            if (!_hasCachedContentState)
            {
                return;
            }

            SetGraphicEnabled(_panelImage, _panelImageWasEnabled);
            SetTextActive(_nameText, _nameTextWasActive);
            SetTextActive(_descriptionText, _descriptionTextWasActive);
            SetTextActive(_valueText, _valueTextWasActive);
            _hasCachedContentState = false;
        }

        private void CacheContentStateIfNeeded()
        {
            if (_hasCachedContentState)
            {
                return;
            }

            _panelImageWasEnabled = _panelImage != null && _panelImage.enabled;
            _nameTextWasActive = IsTextActive(_nameText);
            _descriptionTextWasActive = IsTextActive(_descriptionText);
            _valueTextWasActive = IsTextActive(_valueText);
            _hasCachedContentState = true;
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

        private static void SetGraphicEnabled(Graphic graphic, bool enabled)
        {
            if (graphic != null)
            {
                graphic.enabled = enabled;
            }
        }

        private static bool IsTextActive(Text text)
        {
            return text != null && text.gameObject.activeSelf;
        }

        private static void SetTextActive(Text text, bool active)
        {
            if (text != null)
            {
                text.gameObject.SetActive(active);
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

        private readonly struct RelicIconFlight
        {
            internal RelicIconFlight(SlotRelicTriggerPresentationResult result, Sprite sprite)
            {
                Result = result;
                Sprite = sprite;
            }

            internal SlotRelicTriggerPresentationResult Result { get; }

            internal Sprite Sprite { get; }
        }

        private Vector2 _shownPosition;
        private Tween _activeTween;
        private readonly List<Image> _iconOnlyClones = new();
        private Vector2 _iconDefaultAnchoredPosition;
        private Vector3 _iconDefaultScale = Vector3.one;
        private Quaternion _iconDefaultRotation = Quaternion.identity;
        private Sprite _iconDefaultSprite;
        private bool _iconDefaultEnabled;
        private bool _hasCachedPositions;
        private bool _hasCachedIconDefaults;
        private bool _hasCachedContentState;
        private bool _panelImageWasEnabled;
        private bool _nameTextWasActive;
        private bool _descriptionTextWasActive;
        private bool _valueTextWasActive;
    }
}
