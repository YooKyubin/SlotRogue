using SlotRogue.Data.GameFlow;

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
            int enemyCount = 1,
            RunEncounterDefinition encounter = null)
        {
            NodeId = nodeId;
            DisplayName = displayName;
            Description = description;
            NodeType = nodeType;
            Floor = floor;
            Lane = lane;
            Encounter = encounter;
            EnemyCount = ResolveEnemyCount(enemyCount, encounter);
        }

        public string NodeId { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public RunMapNodeType NodeType { get; }

        public int Floor { get; }

        public int Lane { get; }

        /// <summary>Used when <see cref="Encounter"/> is null or has no entries.</summary>
        public int EnemyCount { get; }

        public RunEncounterDefinition Encounter { get; }

        private static int ResolveEnemyCount(int enemyCount, RunEncounterDefinition encounter)
        {
            if (encounter != null && encounter.HasEntries)
            {
                return encounter.EntryCount;
            }

            return enemyCount < 1 ? 1 : enemyCount;
        }
    }
}
