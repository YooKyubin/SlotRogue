using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyIntentIconView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _amountText;

        private bool _missingIntentIconWarningLogged;

        public void Set(EnemyUpcomingActionViewData action)
        {
            if (_iconImage != null)
            {
                if (action.IntentIcon == null)
                {
                    LogMissingIntentIconWarning(action);
                }

                _iconImage.sprite = action.IntentIcon;
                _iconImage.enabled = _iconImage.sprite != null;
            }

            if (_amountText != null)
            {
                int amount = Mathf.Max(0, action.Amount);
                _amountText.text = amount > 0 ? amount.ToString() : string.Empty;
            }
        }

        private void LogMissingIntentIconWarning(EnemyUpcomingActionViewData action)
        {
            if (_missingIntentIconWarningLogged)
            {
                return;
            }

            _missingIntentIconWarningLogged = true;
            Debug.LogWarning(
                $"[EnemyIntentIconView] Intent icon is missing for action '{action.DisplayName}' " +
                $"({action.Kind}, amount {action.Amount}). " +
                "Assign an intent icon on the Monster Turn Pattern action.",
                this);
        }
    }
}
