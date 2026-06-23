using System;
using SlotRogue.Data.GameFlow;

namespace SlotRogue.UI.GameFlow
{
    public readonly struct EncounterSelectionRequest
    {
        public EncounterTable Table { get; }

        public EncounterTier Tier { get; }

        public int ThemeGroupIndex { get; }

        public int RunSeed { get; }

        public int BattleNumber { get; }

        public EncounterSelectionRequest(
            EncounterTable table,
            EncounterTier tier,
            int themeGroupIndex,
            int runSeed,
            int battleNumber)
        {
            Table = table != null ? table : throw new ArgumentNullException(nameof(table));
            Tier = tier;
            ThemeGroupIndex = themeGroupIndex;
            RunSeed = runSeed;
            BattleNumber = battleNumber;
        }
    }
}
