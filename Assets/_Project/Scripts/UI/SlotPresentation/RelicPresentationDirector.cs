using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation
{
    /// <summary>
    /// 유물 발동 연출: 유물 아이콘이 인벤토리 버튼(앵커) 위치에서 튀어나와 흔들리고 다시 들어간다.
    /// 아이콘만 다루며 이름/설명/값 패널이나 비행 연출은 없다.
    /// </summary>
    public sealed class RelicPresentationDirector : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;

        [Header("Burst")]
        [Tooltip("앵커에서 위로 튀어나오는 거리(px)")]
        [SerializeField] private float _popRise = 44f;
        [SerializeField] private float _popDuration = 0.16f;
        [SerializeField] private float _shakeDuration = 0.28f;
        [SerializeField] private float _shakeAngle = 14f;
        [SerializeField] private float _returnDuration = 0.14f;
        [Tooltip("아이콘이 여러 개일 때 가로 간격(px)")]
        [SerializeField] private float _iconSpacing = 20f;
        [Tooltip("아이콘 사이 순차 등장 간격(초)")]
        [SerializeField] private float _stagger = 0.06f;

        private Tween _activeTween;
        private readonly List<Image> _iconClones = new();
        private Vector2 _iconDefaultAnchoredPosition;
        private Vector3 _iconDefaultScale = Vector3.one;
        private Quaternion _iconDefaultRotation = Quaternion.identity;
        private Sprite _iconDefaultSprite;
        private bool _iconDefaultEnabled;
        private bool _hasCachedIconDefaults;

        /// <summary>유물 아이콘들이 앵커(인벤토리 버튼) 위치에서 튀어나와 흔들리고 들어간다.</summary>
        public IEnumerator PlayBurstAtAnchor(
            IReadOnlyList<SlotRelicTriggerPresentationResult> results,
            RectTransform anchor,
            Action<SlotRelicTriggerPresentationResult> onImpact,
            Func<bool> shouldSkip)
        {
            if (results == null || results.Count == 0 || !ValidateRequiredReferences())
            {
                yield break;
            }

            List<RelicIconFlight> flights = CollectIconFlights(results);
            if (flights.Count == 0)
            {
                yield break;
            }

            CacheIconDefaultsIfNeeded();
            RestoreState();
            gameObject.SetActive(true);

            RectTransform animationParent = ResolveAnimationParent();
            Vector2 anchorPosition = anchor != null
                ? ResolveRectCenter(anchor, animationParent)
                : Vector2.zero;

            Sequence masterSequence = DOTween.Sequence().SetTarget(this).SetUpdate(true);
            for (int index = 0; index < flights.Count; index++)
            {
                Sequence burst = CreateBurstSequence(
                    flights[index],
                    index,
                    flights.Count,
                    animationParent,
                    anchorPosition,
                    onImpact);
                if (burst != null)
                {
                    masterSequence.Insert(index * Mathf.Max(0f, _stagger), burst);
                }
            }

            yield return PlayTween(masterSequence, shouldSkip);
            RestoreState();
            gameObject.SetActive(false);
        }

        public void HideImmediate()
        {
            KillActiveTween();
            RestoreState();
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            KillActiveTween();
        }

        private bool ValidateRequiredReferences()
        {
            if (_iconImage != null)
            {
                return true;
            }

            Debug.LogError(
                "[RelicPresentationDirector] Icon Image must be wired in the inspector.", this);
            return false;
        }

        private Sequence CreateBurstSequence(
            RelicIconFlight flight,
            int index,
            int count,
            RectTransform animationParent,
            Vector2 anchorPosition,
            Action<SlotRelicTriggerPresentationResult> onImpact)
        {
            Image icon = CreateIconClone(animationParent);
            if (icon == null || icon.transform is not RectTransform rt)
            {
                return null;
            }

            icon.sprite = flight.Sprite;
            icon.enabled = true;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            float offset = (index - (count - 1) * 0.5f) * _iconSpacing;
            Vector2 popPosition = anchorPosition + new Vector2(offset, _popRise);

            Color visible = _iconImage.color;
            if (visible.a <= 0.01f)
            {
                visible.a = 1f;
            }

            Color hidden = visible;
            hidden.a = 0f;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchorPosition;
            rt.localScale = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            icon.color = hidden;

            Sequence seq = DOTween.Sequence().SetTarget(rt).SetUpdate(true);

            // 튀어나오기
            seq.Append(
                DOTween.To(
                    () => rt.anchoredPosition,
                    value => rt.anchoredPosition = value,
                    popPosition,
                    _popDuration).SetEase(Ease.OutBack).SetTarget(rt).SetUpdate(true));
            seq.Join(rt.DOScale(Vector3.one, _popDuration).SetEase(Ease.OutBack).SetUpdate(true));
            seq.Join(CreateColorTween(icon, visible, Mathf.Min(0.08f, _popDuration)));

            // 흔들기
            seq.Append(
                rt.DOShakeRotation(_shakeDuration, new Vector3(0f, 0f, _shakeAngle), 12, 90f, false)
                    .SetUpdate(true));

            seq.AppendCallback(() => onImpact?.Invoke(flight.Result));

            // 다시 들어가기
            seq.Append(
                DOTween.To(
                    () => rt.anchoredPosition,
                    value => rt.anchoredPosition = value,
                    anchorPosition,
                    _returnDuration).SetEase(Ease.InBack).SetTarget(rt).SetUpdate(true));
            seq.Join(rt.DOScale(Vector3.zero, _returnDuration).SetEase(Ease.InBack).SetUpdate(true));
            seq.Join(CreateColorTween(icon, hidden, _returnDuration));

            return seq;
        }

        private static List<RelicIconFlight> CollectIconFlights(
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

        private RectTransform ResolveAnimationParent()
        {
            if (_iconImage != null && _iconImage.transform.parent is RectTransform iconParent)
            {
                return iconParent;
            }

            return transform as RectTransform;
        }

        private static Vector2 ResolveRectCenter(RectTransform source, RectTransform targetParent)
        {
            if (source == null)
            {
                return Vector2.zero;
            }

            if (targetParent == null)
            {
                return source.anchoredPosition;
            }

            Vector3 worldCenter = source.TransformPoint(source.rect.center);
            Vector3 localCenter = targetParent.InverseTransformPoint(worldCenter);
            return new Vector2(localCenter.x, localCenter.y);
        }

        private Image CreateIconClone(RectTransform parent)
        {
            Image clone = Instantiate(
                _iconImage,
                parent != null ? parent : _iconImage.transform.parent,
                false);
            _iconClones.Add(clone);
            return clone;
        }

        private void DestroyIconClones()
        {
            for (int index = 0; index < _iconClones.Count; index++)
            {
                if (_iconClones[index] != null)
                {
                    Destroy(_iconClones[index].gameObject);
                }
            }

            _iconClones.Clear();
        }

        private void CacheIconDefaultsIfNeeded()
        {
            if (_hasCachedIconDefaults || _iconImage == null)
            {
                return;
            }

            _iconDefaultSprite = _iconImage.sprite;
            _iconDefaultEnabled = _iconImage.enabled;

            if (_iconImage.transform is RectTransform rt)
            {
                _iconDefaultAnchoredPosition = rt.anchoredPosition;
                _iconDefaultScale = rt.localScale;
                _iconDefaultRotation = rt.localRotation;
            }

            _hasCachedIconDefaults = true;
        }

        private void RestoreState()
        {
            DestroyIconClones();

            if (!_hasCachedIconDefaults || _iconImage == null)
            {
                return;
            }

            _iconImage.sprite = _iconDefaultSprite;
            _iconImage.enabled = _iconDefaultEnabled;

            if (_iconImage.transform is RectTransform rt)
            {
                rt.anchoredPosition = _iconDefaultAnchoredPosition;
                rt.localScale = _iconDefaultScale;
                rt.localRotation = _iconDefaultRotation;
            }
        }

        private IEnumerator PlayTween(Tween tween, Func<bool> shouldSkip)
        {
            if (tween == null)
            {
                yield break;
            }

            _activeTween = tween;

            while (tween.IsActive() && !tween.IsComplete() &&
                !(shouldSkip != null && shouldSkip()))
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

        private static Tween CreateColorTween(Image image, Color targetColor, float duration)
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

        private void KillActiveTween()
        {
            if (_activeTween != null && _activeTween.IsActive())
            {
                _activeTween.Kill();
            }

            _activeTween = null;
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
    }
}
