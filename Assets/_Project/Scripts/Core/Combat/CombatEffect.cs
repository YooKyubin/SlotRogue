namespace SlotRogue.Core.Combat
{
    public readonly struct CombatEffect
    {
        public CombatEffect(CombatEffectKind kind, int amount, CombatEffectTarget target)
            : this(kind, amount, target, StatusEffectSpec.None)
        {
        }

        public CombatEffect(
            CombatEffectKind kind,
            int amount,
            CombatEffectTarget target,
            StatusEffectSpec statusEffect)
        {
            Kind = kind;
            Amount = amount;
            Target = target;
            StatusEffect = statusEffect;
        }

        public CombatEffectKind Kind { get; }

        public int Amount { get; }

        public CombatEffectTarget Target { get; }

        public StatusEffectSpec StatusEffect { get; }

        public static CombatEffect ApplyStatus(StatusEffectSpec statusEffect, CombatEffectTarget target) =>
            new(CombatEffectKind.ApplyStatus, 0, target, statusEffect);
    }
}
