using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation
{
    /// <summary>
    /// Reuses one hierarchy icon for relic trigger presentation.
    /// The icon is positioned above the inventory anchor, shaken, and hidden.
    /// </summary>
    public sealed class RelicPresentationDirector : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;

        [Header("Burst")]
        [Tooltip("앵커에서 위로 튀어나오는 거리(px)")]
        [SerializeField] private float _popRise = 44f;
        [SerializeField] private float _shakeDuration = 0.28f;
        [SerializeField] private float _shakeAngle = 14f;
        [Tooltip("아이콘 사이 순차 등장 간격(초)")]
        [SerializeField] private float _stagger = 0.06f;

        private Tween _activeTween;
        private Vector2 _iconDefaultAnchoredPosition;
        private Vector2 _iconDefaultAnchorMin;
        private Vector2 _iconDefaultAnchorMax;
        private Vector2 _iconDefaultPivot;
        private Vector3 _iconDefaultScale = Vector3.one;
        private Quaternion _iconDefaultRotation = Quaternion.identity;
        private Sprite _iconDefaultSprite;
        private Color _iconDefaultColor = Color.white;
        private bool _iconDefaultEnabled;
        private bool _iconDefaultPreserveAspect;
        private bool _iconDefaultRaycastTarget;
        private bool _hasCachedIconDefaults;

        /// <summary>Shows each triggered relic sequentially with the same hierarchy icon.</summary>
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
                    anchorPosition,
                    onImpact);
                if (burst != null)
                {
                    if (masterSequence.Duration() > 0f && _stagger > 0f)
                    {
                        masterSequence.AppendInterval(_stagger);
                    }

                    masterSequence.Append(burst);
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
            Vector2 anchorPosition,
            Action<SlotRelicTriggerPresentationResult> onImpact)
        {
            if (_iconImage == null || _iconImage.transform is not RectTransform rt)
            {
                return null;
            }

            Vector2 presentationPosition = anchorPosition + new Vector2(0f, _popRise);

            Color visible = _iconDefaultColor;
            if (visible.a <= 0.01f)
            {
                visible.a = 1f;
            }

            Sequence seq = DOTween.Sequence().SetTarget(rt).SetUpdate(true);
            seq.AppendCallback(() =>
            {
                _iconImage.gameObject.SetActive(true);
                _iconImage.sprite = flight.Sprite;
                _iconImage.enabled = true;
                _iconImage.preserveAspect = true;
                _iconImage.raycastTarget = false;

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = presentationPosition;
                rt.localScale = _iconDefaultScale;
                rt.localRotation = _iconDefaultRotation;
                _iconImage.color = visible;
            });

            // Shake.
            seq.Append(
                rt.DOShakeRotation(_shakeDuration, new Vector3(0f, 0f, _shakeAngle), 12, 90f, false)
                    .SetUpdate(true));

            seq.AppendCallback(() => onImpact?.Invoke(flight.Result));
            seq.AppendCallback(() => _iconImage.gameObject.SetActive(false));

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

        private void CacheIconDefaultsIfNeeded()
        {
            if (_hasCachedIconDefaults || _iconImage == null)
            {
                return;
            }

            _iconDefaultSprite = _iconImage.sprite;
            _iconDefaultColor = _iconImage.color;
            _iconDefaultEnabled = _iconImage.enabled;
            _iconDefaultPreserveAspect = _iconImage.preserveAspect;
            _iconDefaultRaycastTarget = _iconImage.raycastTarget;

            if (_iconImage.transform is RectTransform rt)
            {
                _iconDefaultAnchoredPosition = rt.anchoredPosition;
                _iconDefaultAnchorMin = rt.anchorMin;
                _iconDefaultAnchorMax = rt.anchorMax;
                _iconDefaultPivot = rt.pivot;
                _iconDefaultScale = rt.localScale;
                _iconDefaultRotation = rt.localRotation;
            }

            _hasCachedIconDefaults = true;
        }

        private void RestoreState()
        {
            if (!_hasCachedIconDefaults || _iconImage == null)
            {
                return;
            }

            _iconImage.transform.DOKill(complete: true);
            _iconImage.DOKill(complete: true);
            _iconImage.sprite = _iconDefaultSprite;
            _iconImage.color = _iconDefaultColor;
            _iconImage.enabled = _iconDefaultEnabled;
            _iconImage.preserveAspect = _iconDefaultPreserveAspect;
            _iconImage.raycastTarget = _iconDefaultRaycastTarget;

            if (_iconImage.transform is RectTransform rt)
            {
                rt.anchorMin = _iconDefaultAnchorMin;
                rt.anchorMax = _iconDefaultAnchorMax;
                rt.pivot = _iconDefaultPivot;
                rt.anchoredPosition = _iconDefaultAnchoredPosition;
                rt.localScale = _iconDefaultScale;
                rt.localRotation = _iconDefaultRotation;
            }

            _iconImage.gameObject.SetActive(false);
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
