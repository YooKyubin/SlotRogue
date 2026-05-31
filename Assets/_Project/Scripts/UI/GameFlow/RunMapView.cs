using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunMapView : MonoBehaviour
    {
        [SerializeField] private Text _summaryText;
        [SerializeField] private RunMapNodeView[] _nodeViews;
        [SerializeField] private RunMapEdgeView[] _edgeViews;

        public RunMapNodeView[] NodeViews => _nodeViews;

        public RunMapEdgeView[] EdgeViews => _edgeViews;

        public void Bind(Text summaryText, RunMapNodeView[] nodeViews, RunMapEdgeView[] edgeViews)
        {
            _summaryText = summaryText;
            _nodeViews = nodeViews;
            _edgeViews = edgeViews;
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
