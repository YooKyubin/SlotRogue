namespace SlotRogue.Core.Combat
{
    public readonly struct EffectApplyResult
    {
        public static readonly EffectApplyResult None = new(0, 0, 0, 0);

        public EffectApplyResult(int damageDealt, int shieldConsumed, int shieldGained, int healApplied)
        {
            DamageDealt = damageDealt;
            ShieldConsumed = shieldConsumed;
            ShieldGained = shieldGained;
            HealApplied = healApplied;
        }

        public int DamageDealt { get; }

        public int ShieldConsumed { get; }

        public int ShieldGained { get; }

        public int HealApplied { get; }
    }
}
