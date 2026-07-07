using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation
{
    /// <summary>
    /// 패턴 발동 연출: 성립한 슬롯 셀 아이콘을 강조(색+스케일 팝)하고 잠시 유지한 뒤 원복한다.
    /// 강조 전용이라 자기 GameObject를 토글하지 않아 어느 오브젝트에 두어도(공유해도) 안전하다.
    /// 강조 대상 아이콘은 매 스핀마다 SetSlotCellIcons로 주입된다.
    /// </summary>
    public sealed class PatternPresentationDirector : MonoBehaviour
    {
        // 강조 대상 아이콘은 매 스핀마다 SlotPresentationManager가 SetSlotCellIcons로 주입한다(인스펙터 배선 불필요).
        private Image[] _slotCellIcons;

        [SerializeField] private float _highlightScale = 1.08f;
        [SerializeField] private float _highlightDuration = 0.12f;
        [SerializeField] private float _holdDuration = 0.38f;
        [SerializeField] private float _finaleHoldDuration = 0.62f;

        public void SetSlotCellIcons(Image[] slotCellIcons)
        {
            if (ReferenceEquals(_slotCellIcons, slotCellIcons))
            {
                return;
            }

            ResetHighlights();
            _slotCellIcons = slotCellIcons;
            _slotCellIconDefaultScales = null;
            _hasCachedSlotCellDefaults = false;
        }

        public IEnumerator Play(SlotPatternPresentationResult result, Func<bool> shouldSkip)
        {
            if (result == null)
            {
                yield break;
            }

            CacheSlotCellDefaultsIfNeeded();
            ApplyHighlights(result.HighlightedCellIndices);

            yield return WaitOrSkip(result.IsFinale ? _finaleHoldDuration : _holdDuration, shouldSkip);
            yield return RestoreHighlights(result.HighlightedCellIndices, shouldSkip);
        }

        public void HideImmediate()
        {
            KillActiveTween();
            ResetHighlights();
        }

        private void OnDisable()
        {
            KillActiveTween();
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
            if (_slotCellIcons == null || indices == null)
            {
                return;
            }

            for (int index = 0; index < indices.Length; index++)
            {
                int cellIndex = indices[index];

                if (cellIndex >= 0 && cellIndex < _slotCellIcons.Length && _slotCellIcons[cellIndex] != null)
                {
                    PlayHighlightScale(
                        _slotCellIcons[cellIndex].transform,
                        GetDefaultScale(_slotCellIconDefaultScales, cellIndex));
                }
            }
        }

        private IEnumerator RestoreHighlights(int[] indices, Func<bool> shouldSkip)
        {
            if (_slotCellIcons == null || indices == null || indices.Length == 0)
            {
                yield break;
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            bool hasTarget = false;

            for (int index = 0; index < indices.Length; index++)
            {
                int cellIndex = indices[index];

                if (cellIndex >= 0 &&
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
            if (_slotCellIcons == null)
            {
                return;
            }

            for (int index = 0; index < _slotCellIcons.Length; index++)
            {
                if (_slotCellIcons[index] == null)
                {
                    continue;
                }

                _slotCellIcons[index].transform.DOKill();
                _slotCellIcons[index].transform.localScale =
                    _slotCellIconDefaultScales != null && index < _slotCellIconDefaultScales.Length
                        ? _slotCellIconDefaultScales[index]
                        : Vector3.one;
            }
        }

        private void PlayHighlightScale(Transform target, Vector3 defaultScale)
        {
            if (target == null)
            {
                return;
            }

            target.DOKill();
            Vector3 targetScale = new(
                defaultScale.x * _highlightScale,
                defaultScale.y * _highlightScale,
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
            if (_hasCachedSlotCellDefaults || _slotCellIcons == null)
            {
                return;
            }

            _slotCellIconDefaultScales = new Vector3[_slotCellIcons.Length];

            for (int index = 0; index < _slotCellIcons.Length; index++)
            {
                _slotCellIconDefaultScales[index] = _slotCellIcons[index] != null ? _slotCellIcons[index].transform.localScale : Vector3.one;
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
        }

        private static void NoOp()
        {
        }

        private Vector3[] _slotCellIconDefaultScales;
        private Tween _activeTween;
        private bool _hasCachedSlotCellDefaults;
    }
}
