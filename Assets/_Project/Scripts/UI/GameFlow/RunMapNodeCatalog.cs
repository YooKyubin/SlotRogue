namespace SlotRogue.UI.GameFlow
{
    public static class RunMapNodeCatalog
    {
        public static RunMapNodeDefinition StartNode { get; } = new(
            "start-0",
            "Start",
            "Run starting point.",
            RunMapNodeType.Start,
            floor: 0,
            lane: 1);

        private static readonly RunMapNodeDefinition[] DefaultNodes =
        {
            StartNode,
            new(
                "monster-1-0",
                "Monster 1-A",
                "A safer opening fight.",
                RunMapNodeType.Monster,
                floor: 1,
                lane: 0),
            new(
                "monster-1-1",
                "Monster 1-B",
                "A balanced opening fight.",
                RunMapNodeType.Monster,
                floor: 1,
                lane: 1),
            new(
                "elite-1-2",
                "Elite 1-C",
                "A risky opening elite route.",
                RunMapNodeType.Elite,
                floor: 1,
                lane: 2),
            new(
                "monster-2-0",
                "Monster 2-A",
                "A left-side monster route.",
                RunMapNodeType.Monster,
                floor: 2,
                lane: 0),
            new(
                "elite-2-1",
                "Elite 2-B",
                "A stronger center route.",
                RunMapNodeType.Elite,
                floor: 2,
                lane: 1),
            new(
                "monster-2-2",
                "Monster 2-C",
                "A right-side monster route.",
                RunMapNodeType.Monster,
                floor: 2,
                lane: 2),
            new(
                "monster-3-0",
                "Monster 3-A",
                "A steady left branch.",
                RunMapNodeType.Monster,
                floor: 3,
                lane: 0),
            new(
                "monster-3-1",
                "Monster 3-B",
                "A steady center branch.",
                RunMapNodeType.Monster,
                floor: 3,
                lane: 1),
            new(
                "elite-3-2",
                "Elite 3-C",
                "A high-pressure right branch.",
                RunMapNodeType.Elite,
                floor: 3,
                lane: 2),
            new(
                "monster-4-0",
                "Monster 4-A",
                "A safer late route.",
                RunMapNodeType.Monster,
                floor: 4,
                lane: 0),
            new(
                "elite-4-1",
                "Elite 4-B",
                "A dangerous late route.",
                RunMapNodeType.Elite,
                floor: 4,
                lane: 1),
            new(
                "monster-4-2",
                "Monster 4-C",
                "A safer late route.",
                RunMapNodeType.Monster,
                floor: 4,
                lane: 2),
            new(
                "monster-5-0",
                "Monster 5-A",
                "A final preparation fight.",
                RunMapNodeType.Monster,
                floor: 5,
                lane: 0),
            new(
                "monster-5-1",
                "Monster 5-B",
                "A final preparation fight.",
                RunMapNodeType.Monster,
                floor: 5,
                lane: 1),
            new(
                "elite-5-2",
                "Elite 5-C",
                "A final elite before the boss.",
                RunMapNodeType.Elite,
                floor: 5,
                lane: 2),
            new(
                "boss-6-1",
                "Boss Gate",
                "The boss blocks the route.",
                RunMapNodeType.Boss,
                floor: 6,
                lane: 1),
        };

        private static readonly RunMapEdgeDefinition[] DefaultEdges =
        {
            new("start-0", "monster-1-0"),
            new("start-0", "monster-1-1"),
            new("start-0", "elite-1-2"),
            new("monster-1-0", "monster-2-0"),
            new("monster-1-0", "elite-2-1"),
            new("monster-1-1", "monster-2-0"),
            new("monster-1-1", "elite-2-1"),
            new("monster-1-1", "monster-2-2"),
            new("elite-1-2", "elite-2-1"),
            new("elite-1-2", "monster-2-2"),
            new("monster-2-0", "monster-3-0"),
            new("monster-2-0", "monster-3-1"),
            new("elite-2-1", "monster-3-0"),
            new("elite-2-1", "monster-3-1"),
            new("elite-2-1", "elite-3-2"),
            new("monster-2-2", "monster-3-1"),
            new("monster-2-2", "elite-3-2"),
            new("monster-3-0", "monster-4-0"),
            new("monster-3-0", "elite-4-1"),
            new("monster-3-1", "monster-4-0"),
            new("monster-3-1", "elite-4-1"),
            new("monster-3-1", "monster-4-2"),
            new("elite-3-2", "elite-4-1"),
            new("elite-3-2", "monster-4-2"),
            new("monster-4-0", "monster-5-0"),
            new("monster-4-0", "monster-5-1"),
            new("elite-4-1", "monster-5-0"),
            new("elite-4-1", "monster-5-1"),
            new("elite-4-1", "elite-5-2"),
            new("monster-4-2", "monster-5-1"),
            new("monster-4-2", "elite-5-2"),
            new("monster-5-0", "boss-6-1"),
            new("monster-5-1", "boss-6-1"),
            new("elite-5-2", "boss-6-1"),
        };

        public static RunMapGraphDefinition BuildDefaultGraph()
        {
            return new RunMapGraphDefinition(DefaultNodes, DefaultEdges);
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
                return null;
            }

            for (int index = 0; index < choices.Length; index++)
            {
                if (choices[index].NodeId == nodeId)
                {
                    return choices[index];
                }
            }

            return null;
        }
    }
}
