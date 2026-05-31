using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class GameStartView : MonoBehaviour
    {
        [SerializeField] private Text _summaryText;
        [SerializeField] private Button _startButton;

        public Button StartButton => _startButton;

        public void Bind(Text summaryText, Button startButton)
        {
            _summaryText = summaryText;
            _startButton = startButton;
        }

        public void SetSummary(string value)
        {
            if (_summaryText != null)
            {
                _summaryText.text = value;
            }
        }
    }
}
