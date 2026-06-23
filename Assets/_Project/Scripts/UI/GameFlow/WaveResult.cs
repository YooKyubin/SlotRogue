using SlotRogue.Data.GameFlow;

namespace SlotRogue.UI.GameFlow
{
    public readonly struct WaveResult
    {
        public EncounterTier EncounterTier { get; }

        public int ThemeSectionIndex { get; }

        public int PositionInWave { get; }

        public WaveResult(EncounterTier encounterTier, int themeSectionIndex, int positionInWave)
        {
            EncounterTier = encounterTier;
            ThemeSectionIndex = themeSectionIndex;
            PositionInWave = positionInWave;
        }
    }
}
