namespace SlotRogue.Core.Combat
{
    public readonly struct CombatSpinOutcome
    {
        public CombatSpinOutcome(int attack, int defense)
        {
            Attack = attack;
            Defense = defense;
        }

        public int Attack { get; }

        public int Defense { get; }
    }
}
