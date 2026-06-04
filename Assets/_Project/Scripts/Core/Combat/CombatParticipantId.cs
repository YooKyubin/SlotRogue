namespace SlotRogue.Core.Combat
{
    public readonly struct CombatParticipantId
    {
        public CombatParticipantId(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public bool IsValid => Value > 0;

        public override string ToString() => Value.ToString();
    }
}
