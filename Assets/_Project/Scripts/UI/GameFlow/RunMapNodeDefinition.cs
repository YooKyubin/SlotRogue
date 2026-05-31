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
            int lane)
        {
            NodeId = nodeId;
            DisplayName = displayName;
            Description = description;
            NodeType = nodeType;
            Floor = floor;
            Lane = lane;
        }

        public string NodeId { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public RunMapNodeType NodeType { get; }

        public int Floor { get; }

        public int Lane { get; }
    }
}
