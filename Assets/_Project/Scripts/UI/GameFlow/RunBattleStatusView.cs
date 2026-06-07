using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleStatusView : MonoBehaviour
    {
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _enemyIntentText;

        public Text StatusText => _statusText;

        public void Bind(Text statusText, Text enemyIntentText)
        {
            _statusText = statusText;
            _enemyIntentText = enemyIntentText;
        }

        public void Render(RunBattleScreenState state)
        {
            SetText(_statusText, state.StatusText);
            SetText(_enemyIntentText, state.EnemyIntentText);
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
