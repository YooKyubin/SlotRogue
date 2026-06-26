using SlotRogue.UI.RunGame.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace SlotRogue.UI.RunGame
{
    // 결과 화면의 심볼 기여도 막대 한 줄을 렌더링하는 서브 컴포넌트입니다.
    // RunDefeatView의 직렬화 필드를 건드리지 않으므로 partial 파일로 분리해도 씬 배선에 영향이 없습니다.
    public sealed partial class RunDefeatView
    {
        private sealed class SymbolStatRow
        {
            private readonly GameObject _root;
            private readonly TMP_Text _rowText;
            private readonly Image _iconFrame;
            private readonly Image _iconImage;
            private readonly TMP_Text _nameText;
            private readonly TMP_Text _valueText;
            private readonly Image _fillImage;

            private SymbolStatRow(
                GameObject root,
                TMP_Text rowText,
                Image iconFrame,
                Image iconImage,
                TMP_Text nameText,
                TMP_Text valueText,
                Image fillImage)
            {
                _root = root;
                _rowText = rowText;
                _iconFrame = iconFrame;
                _iconImage = iconImage;
                _nameText = nameText;
                _valueText = valueText;
                _fillImage = fillImage;
            }

            internal bool IsValid =>
                _root != null &&
                (_rowText != null ||
                    _nameText != null ||
                    _valueText != null);

            internal static SymbolStatRow Resolve(Transform row)
            {
                if (row == null)
                {
                    return null;
                }

                TMP_Text rowTmpText = row.GetComponent<TMP_Text>();
                Transform iconFrameTransform = FindDeepChild(row, "Symbol Icon Frame");
                Image iconFrame = iconFrameTransform != null
                    ? iconFrameTransform.GetComponent<Image>()
                    : null;
                Image iconImage = ResolveIconImage(iconFrameTransform, iconFrame);
                TMP_Text nameTmpText = FindNestedComponent<TMP_Text>(row, "Symbol Name");
                TMP_Text valueTmpText = FindNestedComponent<TMP_Text>(row, "Symbol Value Text");
                Image fillImage = FindNestedComponent<Image>(row, "Symbol Value Bar Fill");
                return new SymbolStatRow(
                    row.gameObject,
                    rowTmpText,
                    iconFrame,
                    iconImage,
                    nameTmpText,
                    valueTmpText,
                    fillImage);
            }

            internal void Render(
                RunDefeatSymbolStatViewState stat,
                int value,
                int maxValue,
                bool animate,
                float delay,
                float duration,
                Sprite icon)
            {
                _root.SetActive(true);

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

            internal void SetIcon(Sprite icon)
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

            // 화면 진입 시 0→목표로 차오르고, 그 외(탭 전환)엔 즉시 반영한다.
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

            internal void KillFill()
            {
#if DOTWEEN
                if (_fillImage != null)
                {
                    DOTween.Kill(_fillImage);
                }
#endif
            }

            private static string BuildBar(int value, int maxValue)
            {
                const int Width = 10;
                int filled = maxValue <= 0
                    ? 0
                    : Mathf.RoundToInt(Mathf.Clamp01((float)value / maxValue) * Width);
                return new string('|', filled).PadRight(Width, '.');
            }

            private static T FindNestedComponent<T>(Transform root, string objectName)
                where T : Component
            {
                Transform found = FindDeepChild(root, objectName);
                return found != null ? found.GetComponent<T>() : null;
            }

            private static Image ResolveIconImage(Transform iconFrame, Image frameImage)
            {
                if (iconFrame == null)
                {
                    return null;
                }

                for (int index = 0; index < iconFrame.childCount; index++)
                {
                    Image image = iconFrame.GetChild(index).GetComponent<Image>();
                    if (image != null)
                    {
                        return image;
                    }
                }

                for (int index = 0; index < iconFrame.childCount; index++)
                {
                    Image image = iconFrame.GetChild(index).GetComponentInChildren<Image>(true);
                    if (image != null)
                    {
                        return image;
                    }
                }

                return frameImage;
            }
        }
    }
}
