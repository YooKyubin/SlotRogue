namespace SlotRogue.Core.Combat
{
    public sealed class WeakenDamageModifier : StatusEffectComponent, IOutgoingDamageModifier
    {
        public int ModifyDamage(int currentDamage, in DamageModifierContext context)
        {
            if (currentDamage <= 0)
            {
                return 0;
            }

            return (currentDamage * 4 + 4) / 5;
        }
    }
}
