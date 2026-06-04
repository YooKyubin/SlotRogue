using SlotRogue.Data.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public static class RunMapNodeCatalog
    {
        private const string DefaultGraphResourcePath = "DefaultRunMapGraph";

        private static RunMapGraphDefinition s_configuredGraph;

        public static void ConfigureGraph(RunMapGraphDefinition graph)
        {
            s_configuredGraph = graph;
        }

        public static RunMapGraphDefinition BuildDefaultGraph()
        {
            if (s_configuredGraph != null)
            {
                return s_configuredGraph;
            }

            return Resources.Load<RunMapGraphDefinition>(DefaultGraphResourcePath);
        }

        public static RunMapNodeDefinition StartNode
        {
            get
            {
                RunMapGraphDefinition graph = BuildDefaultGraph();
                return graph != null ? graph.GetNode("start-0") : null!;
            }
        }

        public static RunMapNodeDefinition[] BuildNextChoices(RunMapNodeDefinition currentNode)
        {
            return BuildNextChoices(BuildDefaultGraph(), currentNode);
        }

        public static RunMapNodeDefinition[] BuildNextChoices(
            RunMapGraphDefinition mapGraph,
            RunMapNodeDefinition currentNode)
        {
            if (mapGraph == null || currentNode == null)
            {
                return System.Array.Empty<RunMapNodeDefinition>();
            }

            return mapGraph.GetOutgoingNodes(currentNode.NodeId);
        }

        public static RunMapNodeDefinition FindChoice(
            RunMapNodeDefinition[] choices,
            string nodeId)
        {
            if (choices == null)
            {
                return null!;
            }

            for (int index = 0; index < choices.Length; index++)
            {
                if (choices[index].NodeId == nodeId)
                {
                    return choices[index];
                }
            }

            return null!;
        }
    }
}
