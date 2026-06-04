namespace SlotRogue.UI.GameFlow
{
    public sealed class RunMapNodeDefinition
    {
        public RunMapNodeDefinition(
            string nodeId,
            string displayName,
            string description,
            RunMapNodeType nodeType,
            int floor,
            int lane,
            int enemyCount = 1)
        {
            NodeId = nodeId;
            DisplayName = displayName;
            Description = description;
            NodeType = nodeType;
            Floor = floor;
            Lane = lane;
            EnemyCount = enemyCount < 1 ? 1 : enemyCount;
        }

        public string NodeId { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public RunMapNodeType NodeType { get; }

        public int Floor { get; }

        public int Lane { get; }

        public int EnemyCount { get; }
    }
}
