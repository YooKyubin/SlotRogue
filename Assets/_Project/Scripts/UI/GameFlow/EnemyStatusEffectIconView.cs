using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyStatusEffectIconView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Text _valueText;
        [SerializeField] private StatusEffectIconSet _iconSet;

        public void Set(StatusEffectViewData status)
        {
            if (_icon == null || _valueText == null || _iconSet == null)
            {
                Debug.LogError(
                    "[EnemyStatusEffectIconView] Icon, value text, and icon set references are required.",
                    this);
                return;
            }

            _icon.sprite = _iconSet.GetIcon(status.Kind);
            _valueText.text = status.ShowValue
                ? status.DisplayValue.ToString()
                : string.Empty;
        }
    }
}
