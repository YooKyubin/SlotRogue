namespace SlotRogue.Core.Combat
{
    public sealed class VulnerableDamageModifier : StatusEffectComponent, IIncomingDamageModifier
    {
        public int ModifyDamage(int currentDamage, in DamageModifierContext context)
        {
            if (currentDamage <= 0)
            {
                return 0;
            }

            return (currentDamage * 6 + 4) / 5;
        }
    }
}
