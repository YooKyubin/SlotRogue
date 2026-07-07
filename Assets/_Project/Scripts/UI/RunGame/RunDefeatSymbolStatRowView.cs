using SlotRogue.UI.RunGame.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace SlotRogue.UI.RunGame
{
    public sealed class RunDefeatSymbolStatRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _rowText;
        [SerializeField] private Image _iconFrame;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private Image _fillImage;

        public bool IsValid =>
            _rowText != null ||
            _nameText != null ||
            _valueText != null;

        private void Awake()
        {
            ValidateRequiredReferences();
        }

        public void Render(
            RunDefeatSymbolStatViewState stat,
            int value,
            int maxValue,
            bool animate,
            float delay,
            float duration,
            Sprite icon)
        {
            if (!ValidateRequiredReferences())
            {
                return;
            }

            gameObject.SetActive(true);
            SetIcon(icon);

            string displayName = string.IsNullOrWhiteSpace(stat.DisplayName)
                ? "-"
                : stat.DisplayName;
            bool hasAuthoredRow =
                _nameText != null ||
                _valueText != null ||
                _fillImage != null;

            if (_rowText != null)
            {
                _rowText.enabled = !hasAuthoredRow;
                if (!hasAuthoredRow)
                {
                    string prefix = _iconFrame != null ? "      " : string.Empty;
                    _rowText.text =
                        $"{prefix}{displayName,-8}  x{stat.PatternCount}  {BuildBar(value, maxValue)}  {value}";
                }
            }

            if (_nameText != null)
            {
                _nameText.text = $"{displayName}  x{stat.PatternCount}";
            }

            if (_valueText != null)
            {
                _valueText.text = value.ToString();
            }

            if (_fillImage != null)
            {
                _fillImage.type = Image.Type.Filled;
                _fillImage.fillMethod = Image.FillMethod.Horizontal;
                _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                float target = maxValue <= 0
                    ? 0f
                    : Mathf.Clamp01((float)value / maxValue);
                SetFill(target, animate, delay, duration);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetIcon(Sprite icon)
        {
            if (_iconImage == null)
            {
                return;
            }

            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
            _iconImage.preserveAspect = true;
            _iconImage.color = Color.white;
            _iconImage.gameObject.SetActive(icon != null);
        }

        public void KillFill()
        {
#if DOTWEEN
            if (_fillImage != null)
            {
                DOTween.Kill(_fillImage);
            }
#endif
        }

        private bool ValidateRequiredReferences()
        {
            if (IsValid)
            {
                return true;
            }

            Debug.LogError(
                "[RunDefeatSymbolStatRowView] UI references must be wired in the inspector. " +
                "Missing: Row Text or authored row text fields.",
                this);
            return false;
        }

        private void SetFill(float target, bool animate, float delay, float duration)
        {
            if (_fillImage == null)
            {
                return;
            }

#if DOTWEEN
            DOTween.Kill(_fillImage);
            if (animate)
            {
                Image fill = _fillImage;
                fill.fillAmount = 0f;
                DOTween.To(
                        () => fill.fillAmount,
                        value => fill.fillAmount = value,
                        target,
                        duration)
                    .SetTarget(fill)
                    .SetDelay(delay)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true);
                return;
            }
#endif
            _fillImage.fillAmount = target;
        }

        private static string BuildBar(int value, int maxValue)
        {
            const int Width = 10;
            int filled = maxValue <= 0
                ? 0
                : Mathf.RoundToInt(Mathf.Clamp01((float)value / maxValue) * Width);
            return new string('|', filled).PadRight(Width, '.');
        }
    }
}
