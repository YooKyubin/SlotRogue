using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotRogue.Data.GameFlow
{
    [CreateAssetMenu(menuName = "SlotRogue/Game Flow/Run Map Graph")]
    public sealed class RunMapGraphDefinition : ScriptableObject
    {
        public RunMapNodeDefinition[] nodes = Array.Empty<RunMapNodeDefinition>();

        public RunMapGraphEdge[] edges = Array.Empty<RunMapGraphEdge>();

        private Dictionary<string, RunMapNodeDefinition> nodeById = null!;

        private int maxFloor = -1;

        public RunMapNodeDefinition[] Nodes => nodes ?? Array.Empty<RunMapNodeDefinition>();

        public RunMapGraphEdge[] Edges => edges ?? Array.Empty<RunMapGraphEdge>();

        public int MaxFloor
        {
            get
            {
                EnsureCache();
                return maxFloor;
            }
        }

        public RunMapNodeDefinition GetNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return null!;
            }

            EnsureCache();
            return nodeById.TryGetValue(nodeId, out RunMapNodeDefinition node) ? node : null!;
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
                RunMapGraphEdge edge = Edges[index];

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
                RunMapGraphEdge edge = Edges[index];

                if (edge.FromNodeId == fromNodeId && edge.ToNodeId == toNodeId)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureCache()
        {
            if (nodeById != null)
            {
                return;
            }

            nodeById = new Dictionary<string, RunMapNodeDefinition>(Nodes.Length);
            maxFloor = 0;

            for (int index = 0; index < Nodes.Length; index++)
            {
                RunMapNodeDefinition node = Nodes[index];

                if (node == null)
                {
                    continue;
                }

                nodeById[node.NodeId] = node;
                maxFloor = Math.Max(maxFloor, node.Floor);
            }
        }
    }
}
