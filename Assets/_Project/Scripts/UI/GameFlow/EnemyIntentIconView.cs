using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyIntentIconView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _amountText;

        [SerializeField] private Sprite _damageSprite;
        [SerializeField] private Sprite _shieldSprite;
        [SerializeField] private Sprite _healSprite;
        [SerializeField] private Sprite _statusSprite;
        [SerializeField] private Sprite _specialSprite;

        public void Set(EnemyUpcomingActionViewData action)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = action.IntentIcon != null
                    ? action.IntentIcon
                    : ResolveSprite(action.Kind);
                _iconImage.enabled = _iconImage.sprite != null;
            }

            if (_amountText != null)
            {
                int amount = Mathf.Max(0, action.Amount);
                _amountText.text = amount > 0 ? amount.ToString() : string.Empty;
            }
        }

        private Sprite ResolveSprite(EnemyUpcomingActionKind kind)
        {
            switch (kind)
            {
                case EnemyUpcomingActionKind.Damage:
                    return _damageSprite;
                case EnemyUpcomingActionKind.Shield:
                    return _shieldSprite;
                case EnemyUpcomingActionKind.Heal:
                    return _healSprite;
                case EnemyUpcomingActionKind.ApplyStatus:
                    return _statusSprite;
                case EnemyUpcomingActionKind.Special:
                default:
                    return _specialSprite;
            }
        }
    }
}
