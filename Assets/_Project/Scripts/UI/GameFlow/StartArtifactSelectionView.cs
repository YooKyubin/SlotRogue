using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class StartArtifactSelectionView : MonoBehaviour
    {
        [SerializeField] private Text _summaryText;
        [SerializeField] private GameFlowOptionView[] _artifactOptions;

        public GameFlowOptionView[] ArtifactOptions => _artifactOptions;

        public void Bind(Text summaryText, GameFlowOptionView[] artifactOptions)
        {
            _summaryText = summaryText;
            _artifactOptions = artifactOptions;
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
