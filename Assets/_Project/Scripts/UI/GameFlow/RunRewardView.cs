using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunRewardView : MonoBehaviour
    {
        [SerializeField] private Text _summaryText;
        [SerializeField] private GameFlowOptionView[] _rewardOptions;

        public GameFlowOptionView[] RewardOptions => _rewardOptions;

        public void Bind(Text summaryText, GameFlowOptionView[] rewardOptions)
        {
            _summaryText = summaryText;
            _rewardOptions = rewardOptions;
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
