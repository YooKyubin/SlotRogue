using System;
using System.Collections.Generic;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunMapGraphDefinition
    {
        private readonly Dictionary<string, RunMapNodeDefinition> nodeById;

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

            var outgoingNodes = new List<RunMapNodeDefinition>();

            for (int index = 0; index < Edges.Length; index++)
            {
                RunMapEdgeDefinition edge = Edges[index];

                if (edge.FromNodeId != nodeId)
                {
                    continue;
                }

                RunMapNodeDefinition node = GetNode(edge.ToNodeId);
                if (node != null)
                {
                    outgoingNodes.Add(node);
                }
            }

            return outgoingNodes.ToArray();
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
