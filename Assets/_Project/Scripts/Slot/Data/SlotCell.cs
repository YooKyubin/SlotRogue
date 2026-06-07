using System;

namespace SlotRogue.Slot.Data
{
    public readonly struct SlotCell : IEquatable<SlotCell>
    {
        public readonly int Col;
        public readonly int Row;

        public SlotCell(int col, int row)
        {
            Col = col;
            Row = row;
        }

        public bool Equals(SlotCell other) => Col == other.Col && Row == other.Row;
        public override bool Equals(object obj) => obj is SlotCell other && Equals(other);
        public override int GetHashCode() => Col * 31 + Row;
        public override string ToString() => $"({Col},{Row})";

        public static bool operator ==(SlotCell a, SlotCell b) => a.Equals(b);
        public static bool operator !=(SlotCell a, SlotCell b) => !a.Equals(b);

        public int SortKey => Row * SlotSpinResult.Columns + Col;
    }
}
