using System;
using System.Collections.Generic;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunMapGraphDefinition
    {
        private readonly Dictionary<string, RunMapNodeDefinition> nodeById;
        private readonly Dictionary<string, RunMapNodeDefinition[]> outgoingByNodeId;

        public RunMapGraphDefinition(
            RunMapNodeDefinition[] nodes,
            RunMapEdgeDefinition[] edges)
        {
            Nodes = nodes ?? Array.Empty<RunMapNodeDefinition>();
            Edges = edges ?? Array.Empty<RunMapEdgeDefinition>();
            nodeById = new Dictionary<string, RunMapNodeDefinition>(Nodes.Length);

            for (int index = 0; index < Nodes.Length; index++)
            {
                RunMapNodeDefinition node = Nodes[index];

                if (node == null)
                {
                    continue;
                }

                nodeById[node.NodeId] = node;
                MaxFloor = Math.Max(MaxFloor, node.Floor);
            }

            outgoingByNodeId = BuildOutgoingMap();
        }

        private Dictionary<string, RunMapNodeDefinition[]> BuildOutgoingMap()
        {
            var lists = new Dictionary<string, List<RunMapNodeDefinition>>();

            for (int index = 0; index < Edges.Length; index++)
            {
                RunMapEdgeDefinition edge = Edges[index];
                RunMapNodeDefinition node = GetNode(edge.ToNodeId);

                if (node == null)
                {
                    continue;
                }

                if (!lists.TryGetValue(edge.FromNodeId, out List<RunMapNodeDefinition> list))
                {
                    list = new List<RunMapNodeDefinition>();
                    lists[edge.FromNodeId] = list;
                }

                list.Add(node);
            }

            var map = new Dictionary<string, RunMapNodeDefinition[]>(lists.Count);

            foreach (KeyValuePair<string, List<RunMapNodeDefinition>> entry in lists)
            {
                map[entry.Key] = entry.Value.ToArray();
            }

            return map;
        }

        public RunMapNodeDefinition[] Nodes { get; }

        public RunMapEdgeDefinition[] Edges { get; }

        public int MaxFloor { get; }

        public RunMapNodeDefinition GetNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return null;
            }

            return nodeById.TryGetValue(nodeId, out RunMapNodeDefinition node)
                ? node
                : null;
        }

        public RunMapNodeDefinition[] GetOutgoingNodes(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return Array.Empty<RunMapNodeDefinition>();
            }

            return outgoingByNodeId.TryGetValue(nodeId, out RunMapNodeDefinition[] nodes)
                ? nodes
                : Array.Empty<RunMapNodeDefinition>();
        }

        public bool HasEdge(string fromNodeId, string toNodeId)
        {
            for (int index = 0; index < Edges.Length; index++)
            {
                RunMapEdgeDefinition edge = Edges[index];

                if (edge.FromNodeId == fromNodeId && edge.ToNodeId == toNodeId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
