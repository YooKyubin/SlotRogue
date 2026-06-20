namespace SlotRogue.Core.Combat
{
    public readonly struct EncounterScaleResult
    {
        public EncounterScaleResult(int maxHp)
        {
            MaxHp = maxHp;
        }

        public int MaxHp { get; }
    }
}
