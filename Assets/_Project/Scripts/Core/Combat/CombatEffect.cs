namespace SlotRogue.Core.Combat
{
    public readonly struct CombatEffect
    {
        public CombatEffect(CombatEffectKind kind, int amount, CombatEffectTarget target)
        {
            Kind = kind;
            Amount = amount;
            Target = target;
        }

        public CombatEffectKind Kind { get; }

        public int Amount { get; }

        public CombatEffectTarget Target { get; }
    }
}
