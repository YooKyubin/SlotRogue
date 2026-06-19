using SlotRogue.Data.GameFlow;

namespace SlotRogue.UI.GameFlow
{
    public readonly struct WaveResult
    {
        public EncounterTier Tier { get; }

        public int Cycle { get; }

        public int PositionInCycle { get; }

        public WaveResult(EncounterTier tier, int cycle, int positionInCycle)
        {
            Tier = tier;
            Cycle = cycle;
            PositionInCycle = positionInCycle;
        }
    }
}
