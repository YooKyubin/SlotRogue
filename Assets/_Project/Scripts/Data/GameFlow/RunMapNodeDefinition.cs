using UnityEngine;

namespace SlotRogue.Data.GameFlow
{
    [CreateAssetMenu(menuName = "SlotRogue/Game Flow/Run Map Node")]
    public sealed class RunMapNodeDefinition : ScriptableObject
    {
        public string nodeId = string.Empty;

        public string displayName = string.Empty;

        public string description = string.Empty;

        public RunMapNodeType nodeType;

        public int floor;

        public int lane;

        public RunEncounterDefinition encounter = null!;

        public string NodeId => nodeId;

        public string DisplayName => displayName;

        public string Description => description;

        public RunMapNodeType NodeType => nodeType;

        public int Floor => floor;

        public int Lane => lane;

        public RunEncounterDefinition Encounter => encounter;

        public bool HasEncounter => encounter != null && encounter.HasEntries;
    }
}
