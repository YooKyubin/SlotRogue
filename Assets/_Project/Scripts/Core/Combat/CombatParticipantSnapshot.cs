namespace SlotRogue.Core.Combat
{
    public readonly struct CombatParticipantSnapshot
    {
        public CombatParticipantSnapshot(int hp, int shield)
        {
            Hp = hp;
            Shield = shield;
        }

        public int Hp { get; }

        public int Shield { get; }
    }
}
