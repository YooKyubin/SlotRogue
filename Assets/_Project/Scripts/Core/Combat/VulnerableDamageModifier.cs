namespace SlotRogue.Core.Combat
{
    public sealed class VulnerableDamageModifier : StatusEffectComponent, IIncomingDamageModifier
    {
        private const int DamageIncreasePercent = 20;
        private const int PercentScale = 100;

        public int ModifyDamage(int currentDamage, in DamageModifierContext context)
        {
            if (currentDamage <= 0)
            {
                return 0;
            }

            int multiplierPercent = PercentScale + DamageIncreasePercent;
            return (currentDamage * multiplierPercent + PercentScale - 1) / PercentScale;
        }
    }
}
