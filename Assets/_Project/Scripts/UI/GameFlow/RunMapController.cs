using System.Text;
using SlotRogue.Data.GameFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunMapController : MonoBehaviour
    {
        private static readonly Color32 EdgeLockedColor = new(82, 91, 104, 180);
        private static readonly Color32 EdgeSelectableColor = new(226, 185, 83, 255);
        private static readonly Color32 EdgeVisitedColor = new(84, 187, 153, 255);
        private static readonly Color32 NodeLockedColor = new(60, 65, 78, 255);
        private static readonly Color32 NodeMonsterColor = new(78, 105, 91, 255);
        private static readonly Color32 NodeEliteColor = new(105, 79, 126, 255);
        private static readonly Color32 NodeBossColor = new(137, 62, 66, 255);
        private static readonly Color32 NodeCurrentColor = new(232, 193, 83, 255);
        private static readonly Color32 NodeVisitedColor = new(62, 145, 121, 255);
        private static readonly Color32 NodeSelectableColor = new(218, 157, 67, 255);

        [SerializeField] private RunMapView _view;

        private void Awake()
        {
            GameFlowSession.EnsureRunStarted();

            if (!GameFlowSession.HasStarterArtifact)
            {
                SceneManager.LoadScene(GameFlowSceneNames.StartArtifactSelection);
                return;
            }

            if (_view == null)
            {
                _view = GetComponent<RunMapView>();
            }

            if (_view == null)
            {
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            RunMapGraphDefinition graph = GameFlowSession.CurrentMapGraph;

            for (int index = 0; index < _view.EdgeViews.Length; index++)
            {
                RunMapEdgeView edgeView = _view.EdgeViews[index];
                edgeView.SetColor(ResolveEdgeColor(edgeView.FromNodeId, edgeView.ToNodeId));
            }

            for (int index = 0; index < _view.NodeViews.Length; index++)
            {
                RunMapNodeView nodeView = _view.NodeViews[index];
                RunMapNodeDefinition node = graph.GetNode(nodeView.NodeId);

                if (node == null)
                {
                    nodeView.gameObject.SetActive(false);
                    continue;
                }

                bool selectable = GameFlowSession.IsMapNodeSelectable(node.NodeId);
                nodeView.SetState(BuildNodeLabel(node), ResolveNodeColor(node), selectable);
                nodeView.Button.onClick.RemoveAllListeners();

                if (selectable)
                {
                    string nodeId = node.NodeId;
                    nodeView.Button.onClick.AddListener(() => SelectNode(nodeId));
                }
            }

            _view.SetSummary(BuildCurrentNodeText());
        }

        private static void SelectNode(string nodeId)
        {
            if (!GameFlowSession.SelectNextMapNode(nodeId))
            {
                return;
            }

            SceneManager.LoadScene(GameFlowSceneNames.RunBattle);
        }

        private static Color32 ResolveEdgeColor(string fromNodeId, string toNodeId)
        {
            bool selectedPath = GameFlowSession.IsMapNodeVisited(fromNodeId) &&
                GameFlowSession.IsMapNodeVisited(toNodeId);
            bool selectablePath = GameFlowSession.CurrentMapNode.NodeId == fromNodeId &&
                GameFlowSession.IsMapNodeSelectable(toNodeId);

            if (selectedPath)
            {
                return EdgeVisitedColor;
            }

            return selectablePath ? EdgeSelectableColor : EdgeLockedColor;
        }

        private static Color32 ResolveNodeColor(RunMapNodeDefinition node)
        {
            if (GameFlowSession.CurrentMapNode.NodeId == node.NodeId)
            {
                return NodeCurrentColor;
            }

            if (GameFlowSession.IsMapNodeSelectable(node.NodeId))
            {
                return NodeSelectableColor;
            }

            if (GameFlowSession.IsMapNodeVisited(node.NodeId))
            {
                return NodeVisitedColor;
            }

            switch (node.NodeType)
            {
                case RunMapNodeType.Monster:
                    return NodeMonsterColor;
                case RunMapNodeType.Elite:
                    return NodeEliteColor;
                case RunMapNodeType.Boss:
                    return NodeBossColor;
                default:
                    return NodeLockedColor;
            }
        }

        private static string BuildCurrentNodeText()
        {
            RunMapNodeDefinition currentNode = GameFlowSession.CurrentMapNode;
            RunMapNodeDefinition[] choices = GameFlowSession.GetNextMapChoices();
            var builder = new StringBuilder();

            builder.AppendLine($"Current location: {currentNode.DisplayName}");
            builder.AppendLine($"Floor: {currentNode.Floor}, Lane: {currentNode.Lane}, Type: {currentNode.NodeType}");
            builder.AppendLine($"Next selectable nodes: {choices.Length}");

            for (int index = 0; index < choices.Length; index++)
            {
                builder.AppendLine($"- {choices[index].DisplayName} ({choices[index].NodeType})");
            }

            if (choices.Length == 0)
            {
                builder.AppendLine("- Route cleared");
            }

            builder.AppendLine();
            builder.Append(GameFlowSession.BuildSummary());
            return builder.ToString();
        }

        private static string BuildNodeLabel(RunMapNodeDefinition node)
        {
            switch (node.NodeType)
            {
                case RunMapNodeType.Start:
                    return "START";
                case RunMapNodeType.Boss:
                    return "BOSS";
                case RunMapNodeType.Elite:
                    return $"ELITE\nF{node.Floor}";
                default:
                    return $"MONSTER\nF{node.Floor}";
            }
        }
    }
}
