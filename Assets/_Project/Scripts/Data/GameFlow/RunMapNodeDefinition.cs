using UnityEngine;

namespace SlotRogue.Data.GameFlow
{
    [CreateAssetMenu(menuName = "SlotRogue/Game Flow/Run Map Node")]
    public sealed class RunMapNodeDefinition : ScriptableObject
    {
        [SerializeField] private string _nodeID = string.Empty;

        [SerializeField] private string _displayName = string.Empty;

        [SerializeField] private string _description = string.Empty;

        [SerializeField] private RunMapNodeType _nodeType;

        [SerializeField] private int _floor;

        [SerializeField] private int _lane;

        [SerializeField] private RunEncounterDefinition _encounter = null!;

        public string NodeId => _nodeID;

        public string DisplayName => _displayName;

        public string Description => _description;

        public RunMapNodeType NodeType => _nodeType;

        public int Floor => _floor;

        public int Lane => _lane;

        public RunEncounterDefinition Encounter => _encounter;

        public bool HasEncounter => _encounter != null && _encounter.HasEntries;
    }
}
