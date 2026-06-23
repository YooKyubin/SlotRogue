namespace SlotRogue.Core.Combat
{
    public readonly struct CombatEffect
    {
        public CombatEffect(CombatEffectKind kind, int amount, CombatEffectTarget target)
            : this(kind, amount, target, StatusEffectSpec.None, DamageOrigin.DirectAction)
        {
        }

        public CombatEffect(
            CombatEffectKind kind,
            int amount,
            CombatEffectTarget target,
            DamageOrigin damageOrigin)
            : this(kind, amount, target, StatusEffectSpec.None, damageOrigin)
        {
        }

        public CombatEffect(
            CombatEffectKind kind,
            int amount,
            CombatEffectTarget target,
            StatusEffectSpec statusEffect)
            : this(kind, amount, target, statusEffect, DamageOrigin.DirectAction)
        {
        }

        public CombatEffect(
            CombatEffectKind kind,
            int amount,
            CombatEffectTarget target,
            StatusEffectSpec statusEffect,
            DamageOrigin damageOrigin)
        {
            Kind = kind;
            Amount = amount;
            Target = target;
            StatusEffect = statusEffect;
            DamageOrigin = damageOrigin;
        }

        public CombatEffectKind Kind { get; }

        public int Amount { get; }

        public CombatEffectTarget Target { get; }

        public StatusEffectSpec StatusEffect { get; }

        public DamageOrigin DamageOrigin { get; }

        public static CombatEffect ApplyStatus(StatusEffectSpec statusEffect, CombatEffectTarget target) =>
            new(CombatEffectKind.ApplyStatus, 0, target, statusEffect);
    }
}
