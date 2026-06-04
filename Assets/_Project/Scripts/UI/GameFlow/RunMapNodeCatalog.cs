namespace SlotRogue.UI.GameFlow
{
    public static class RunMapNodeCatalog
    {
        public static RunMapNodeDefinition StartNode { get; } = new(
            "start-0",
            "시작",
            "런 시작 지점.",
            RunMapNodeType.Start,
            floor: 0,
            lane: 1);

        private static readonly RunMapNodeDefinition[] DefaultNodes =
        {
            StartNode,
            new(
                "monster-1-0",
                "몬스터 1-A",
                "비교적 안전한 첫 전투.",
                RunMapNodeType.Monster,
                floor: 1,
                lane: 0),
            new(
                "monster-1-1",
                "몬스터 1-B",
                "균형 잡힌 첫 전투.",
                RunMapNodeType.Monster,
                floor: 1,
                lane: 1),
            new(
                "elite-1-2",
                "엘리트 1-C",
                "위험한 첫 엘리트 경로.",
                RunMapNodeType.Elite,
                floor: 1,
                lane: 2),
            new(
                "monster-2-0",
                "몬스터 2-A",
                "좌측 몬스터 경로.",
                RunMapNodeType.Monster,
                floor: 2,
                lane: 0),
            new(
                "elite-2-1",
                "엘리트 2-B",
                "강력한 중앙 경로.",
                RunMapNodeType.Elite,
                floor: 2,
                lane: 1),
            new(
                "monster-2-2",
                "몬스터 2-C",
                "우측 몬스터 경로.",
                RunMapNodeType.Monster,
                floor: 2,
                lane: 2),
            new(
                "monster-3-0",
                "몬스터 3-A",
                "안정적인 좌측 경로.",
                RunMapNodeType.Monster,
                floor: 3,
                lane: 0),
            new(
                "monster-3-1",
                "몬스터 3-B",
                "안정적인 중앙 경로.",
                RunMapNodeType.Monster,
                floor: 3,
                lane: 1),
            new(
                "elite-3-2",
                "엘리트 3-C",
                "압박이 강한 우측 경로.",
                RunMapNodeType.Elite,
                floor: 3,
                lane: 2),
            new(
                "monster-4-0",
                "몬스터 4-A",
                "비교적 안전한 후반 경로.",
                RunMapNodeType.Monster,
                floor: 4,
                lane: 0),
            new(
                "elite-4-1",
                "엘리트 4-B",
                "위험한 후반 경로.",
                RunMapNodeType.Elite,
                floor: 4,
                lane: 1),
            new(
                "monster-4-2",
                "몬스터 4-C",
                "비교적 안전한 후반 경로.",
                RunMapNodeType.Monster,
                floor: 4,
                lane: 2),
            new(
                "monster-5-0",
                "몬스터 5-A",
                "보스 직전 마지막 준비 전투.",
                RunMapNodeType.Monster,
                floor: 5,
                lane: 0),
            new(
                "monster-5-1",
                "몬스터 5-B",
                "보스 직전 마지막 준비 전투.",
                RunMapNodeType.Monster,
                floor: 5,
                lane: 1),
            new(
                "elite-5-2",
                "엘리트 5-C",
                "보스 직전 마지막 엘리트.",
                RunMapNodeType.Elite,
                floor: 5,
                lane: 2),
            new(
                "boss-6-1",
                "보스 관문",
                "강력한 보스가 앞을 막고 있다.",
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
