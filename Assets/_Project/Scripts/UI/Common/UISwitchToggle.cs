using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Common
{
    /// <summary>
    /// Unity <see cref="Toggle"/>과 함께 동작하는 iOS 스타일 스위치 비주얼.
    /// 켜짐/꺼짐 배경 스프라이트를 교체하고 knob(핸들)을 좌우로 슬라이드한다.
    /// 입력·상태(isOn)는 Unity Toggle이 그대로 담당하고, 이 컴포넌트는 보이는 모습만 바꾼다.
    /// 위치/크기/스프라이트/이동 거리는 인스펙터에서 설정한다.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public sealed class UISwitchToggle : MonoBehaviour
    {
        [Tooltip("켜짐/꺼짐 스프라이트를 교체할 배경 Image. 비우면 Toggle.targetGraphic을 쓴다.")]
        [SerializeField] private Image _background;
        [Tooltip("좌우로 움직이는 핸들(원형 knob)의 RectTransform.")]
        [SerializeField] private RectTransform _knob;
        [SerializeField] private Sprite _onSprite;
        [SerializeField] private Sprite _offSprite;
        [Tooltip("켜짐일 때 knob의 anchoredPosition.x")]
        [SerializeField] private float _knobOnX = 23f;
        [Tooltip("꺼짐일 때 knob의 anchoredPosition.x")]
        [SerializeField] private float _knobOffX = -23f;
        [Tooltip("0이면 즉시 이동, >0이면 해당 시간(초, unscaled) 동안 보간한다.")]
        [SerializeField] private float _slideDuration = 0.12f;

        [SerializeField] private Toggle _toggle;
        private Coroutine _slide;

        private void Awake()
        {
            ValidateRequiredReferences();
        }

        private void OnEnable()
        {
            if (!ValidateRequiredReferences())
            {
                return;
            }

            _toggle.onValueChanged.AddListener(OnValueChanged);
            // 켜진 채 패널이 다시 열려도 현재 상태에 맞춰 즉시 정렬한다.
            Apply(_toggle.isOn, instant: true);
        }

        private void OnDisable()
        {
            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveListener(OnValueChanged);
            }

            if (_slide != null)
            {
                StopCoroutine(_slide);
                _slide = null;
            }
        }

        private void OnValueChanged(bool isOn)
        {
            Apply(isOn, instant: false);
        }

        private bool ValidateRequiredReferences()
        {
            bool hasReferences =
                _toggle != null &&
                _background != null &&
                _knob != null;
            if (!hasReferences)
            {
                Debug.LogError(
                    "[UISwitchToggle] UI references must be wired in the inspector. " +
                    $"Missing: {BuildMissingReferenceSummary()}",
                    this);
            }

            return hasReferences;
        }

        private string BuildMissingReferenceSummary()
        {
            var builder = new System.Text.StringBuilder();
            AppendMissing(builder, _toggle != null, "Toggle");
            AppendMissing(builder, _background != null, "Background");
            AppendMissing(builder, _knob != null, "Knob");
            return builder.Length > 0 ? builder.ToString() : "none";
        }

        private static void AppendMissing(
            System.Text.StringBuilder builder,
            bool hasReference,
            string label)
        {
            if (hasReference)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(label);
        }

        /// <summary>현재 상태를 비주얼에 반영한다. instant면 보간 없이 즉시 정렬한다.</summary>
        public void Apply(bool isOn, bool instant)
        {
            if (_background != null)
            {
                Sprite sprite = isOn ? _onSprite : _offSprite;
                if (sprite != null)
                {
                    _background.sprite = sprite;
                }
            }

            if (_knob == null)
            {
                return;
            }

            if (_slide != null)
            {
                StopCoroutine(_slide);
                _slide = null;
            }

            float targetX = isOn ? _knobOnX : _knobOffX;
            if (instant || _slideDuration <= 0f || !isActiveAndEnabled)
            {
                SetKnobX(targetX);
            }
            else
            {
                _slide = StartCoroutine(SlideKnob(targetX));
            }
        }

        private IEnumerator SlideKnob(float targetX)
        {
            float startX = _knob.anchoredPosition.x;
            float elapsed = 0f;
            while (elapsed < _slideDuration)
            {
                // 일시정지(설정 패널)에서 timeScale=0일 수 있어 unscaled를 쓴다.
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _slideDuration);
                SetKnobX(Mathf.Lerp(startX, targetX, t));
                yield return null;
            }

            SetKnobX(targetX);
            _slide = null;
        }

        private void SetKnobX(float x)
        {
            Vector2 pos = _knob.anchoredPosition;
            pos.x = x;
            _knob.anchoredPosition = pos;
        }
    }
}
