namespace SlotRogue.Core.Combat
{
    public sealed class WeakenDamageModifier : StatusEffectComponent, IOutgoingDamageModifier
    {
        private const int DamageReductionPercent = 20;
        private const int PercentScale = 100;

        public int ModifyDamage(int currentDamage, in DamageModifierContext context)
        {
            if (currentDamage <= 0)
            {
                return 0;
            }

            int multiplierPercent = PercentScale - DamageReductionPercent;
            return (currentDamage * multiplierPercent + PercentScale - 1) / PercentScale;
        }
    }
}
