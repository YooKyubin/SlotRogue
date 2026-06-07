using SlotRogue.Data.GameFlow;
using SlotRogue.UI.RunGame;
using SlotRogue.UI.RunGame.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunMapView : MonoBehaviour, IRunMapView
    {
        // ── 노드/엣지 색상 ───────────────────────────────────────────────
        private static readonly Color32 EdgeLocked     = new(82,  91,  104, 180);
        private static readonly Color32 EdgeSelectable = new(226, 185, 83,  255);
        private static readonly Color32 EdgeVisited    = new(84,  187, 153, 255);
        private static readonly Color32 NodeLocked     = new(60,  65,  78,  255);
        private static readonly Color32 NodeMonster    = new(78,  105, 91,  255);
        private static readonly Color32 NodeElite      = new(105, 79,  126, 255);
        private static readonly Color32 NodeBoss       = new(137, 62,  66,  255);
        private static readonly Color32 NodeCurrent    = new(232, 193, 83,  255);
        private static readonly Color32 NodeVisited    = new(62,  145, 121, 255);
        private static readonly Color32 NodeSelectable = new(218, 157, 67,  255);

        [SerializeField] private Text _summaryText;
        [SerializeField] private RunMapNodeView[] _nodeViews;
        [SerializeField] private RunMapEdgeView[] _edgeViews;

        public RunMapNodeView[] NodeViews => _nodeViews;
        public RunMapEdgeView[] EdgeViews => _edgeViews;

        private RunMapViewModel _viewModel;

        // ── 기존 Editor 빌더용 Bind (유지) ──────────────────────────────

        public void Bind(Text summaryText, RunMapNodeView[] nodeViews, RunMapEdgeView[] edgeViews)
        {
            _summaryText = summaryText;
            _nodeViews   = nodeViews;
            _edgeViews   = edgeViews;
        }

        // ── IRunMapView (MVVM) ───────────────────────────────────────────

        public void Bind(RunMapViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void OnEnter()
        {
            gameObject.SetActive(true);
            if (_viewModel == null) return;

            _viewModel.Refresh();
            Render();
        }

        public void OnExit()
        {
            gameObject.SetActive(false);
        }

        // ── 내부 렌더링 ──────────────────────────────────────────────────

        public void SetSummary(string value)
        {
            if (_summaryText != null)
                _summaryText.text = value;
        }

        private void Render()
        {
            if (_viewModel == null) return;

            RunMapGraphDefinition graph = _viewModel.Graph;

            for (int i = 0; i < _edgeViews.Length; i++)
            {
                RunMapEdgeView edge = _edgeViews[i];
                edge.SetColor(ResolveEdgeColor(edge.FromNodeId, edge.ToNodeId));
            }

            for (int i = 0; i < _nodeViews.Length; i++)
            {
                RunMapNodeView nodeView = _nodeViews[i];
                RunMapNodeDefinition node = graph.GetNode(nodeView.NodeId);

                if (node == null)
                {
                    nodeView.gameObject.SetActive(false);
                    continue;
                }

                bool selectable = _viewModel.IsNodeSelectable(node.NodeId);
                nodeView.SetState(BuildNodeLabel(node), ResolveNodeColor(node), selectable);
                nodeView.Button.onClick.RemoveAllListeners();

                if (selectable)
                {
                    string nodeId = node.NodeId;
                    nodeView.Button.onClick.AddListener(() => _viewModel.SelectNode(nodeId));
                }
            }

            SetSummary(_viewModel.Summary);
        }

        private Color32 ResolveEdgeColor(string fromId, string toId)
        {
            bool visited   = _viewModel.IsNodeVisited(fromId) && _viewModel.IsNodeVisited(toId);
            bool selectable = _viewModel.CurrentNode.NodeId == fromId &&
                              _viewModel.IsNodeSelectable(toId);

            if (visited) return EdgeVisited;
            return selectable ? EdgeSelectable : EdgeLocked;
        }

        private Color32 ResolveNodeColor(RunMapNodeDefinition node)
        {
            if (_viewModel.CurrentNode.NodeId == node.NodeId) return NodeCurrent;
            if (_viewModel.IsNodeSelectable(node.NodeId))     return NodeSelectable;
            if (_viewModel.IsNodeVisited(node.NodeId))        return NodeVisited;

            return node.NodeType switch
            {
                RunMapNodeType.Monster => NodeMonster,
                RunMapNodeType.Elite   => NodeElite,
                RunMapNodeType.Boss    => NodeBoss,
                _                     => NodeLocked,
            };
        }

        private static string BuildNodeLabel(RunMapNodeDefinition node)
        {
            return node.NodeType switch
            {
                RunMapNodeType.Start   => "START",
                RunMapNodeType.Boss    => "BOSS",
                RunMapNodeType.Elite   => $"ELITE\nF{node.Floor}",
                _                     => $"MON\nF{node.Floor}",
            };
        }
    }
}
