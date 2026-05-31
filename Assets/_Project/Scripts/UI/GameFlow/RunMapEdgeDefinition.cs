namespace SlotRogue.UI.GameFlow
{
    public sealed class RunMapEdgeDefinition
    {
        public RunMapEdgeDefinition(string fromNodeId, string toNodeId)
        {
            FromNodeId = fromNodeId;
            ToNodeId = toNodeId;
        }

        public string FromNodeId { get; }

        public string ToNodeId { get; }
    }
}
