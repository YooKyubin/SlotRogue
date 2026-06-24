using SlotRogue.Core.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyStatusEffectIconView : MonoBehaviour
    {
        [SerializeField] private Text _leftTurnText;
        [SerializeField] private Text _stackText;

        public void Set(StatusEffectViewData status)
        {
            if (_leftTurnText != null)
            {
                int remainingTurns = Mathf.Max(0, status.RemainingTurns);
                _leftTurnText.text = remainingTurns > 0 ? remainingTurns.ToString() : string.Empty;
            }

            if (_stackText != null)
            {
                int stackCount = Mathf.Max(0, status.StackCount);
                bool shouldShowStack = stackCount > 1 || status.Kind == StatusEffectKind.Infection;
                _stackText.text = shouldShowStack && stackCount > 0 ? stackCount.ToString() : string.Empty;
            }
        }
    }
}
