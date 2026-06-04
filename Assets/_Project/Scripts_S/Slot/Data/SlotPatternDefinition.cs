using System.Collections.Generic;

namespace SlotRogue.Slot.Data
{
    public sealed class SlotPatternDefinition
    {
        public SlotPatternDefinition(
            string patternId,
            string displayName,
            int orderIndex,
            float multiplier,
            SlotPatternRank rank,
            bool isJackpot,
            IReadOnlyList<SlotCell> cells)
        {
            PatternId = patternId;
            DisplayName = displayName;
            OrderIndex = orderIndex;
            Multiplier = multiplier;
            Rank = rank;
            IsJackpot = isJackpot;
            Cells = cells;
        }

        public string PatternId { get; }
        public string DisplayName { get; }
        public int OrderIndex { get; }
        public float Multiplier { get; }
        public SlotPatternRank Rank { get; }
        public bool IsJackpot { get; }
        public IReadOnlyList<SlotCell> Cells { get; }
    }
}
