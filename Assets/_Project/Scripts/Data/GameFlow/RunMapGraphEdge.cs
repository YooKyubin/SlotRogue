using System;

namespace SlotRogue.Data.GameFlow
{
    [Serializable]
    public struct RunMapGraphEdge
    {
        public string fromNodeId;

        public string toNodeId;

        public string FromNodeId => fromNodeId;

        public string ToNodeId => toNodeId;
    }
}
