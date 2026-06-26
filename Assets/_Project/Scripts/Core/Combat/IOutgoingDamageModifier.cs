namespace SlotRogue.Core.Combat
{
    public interface IOutgoingDamageModifier : IStatusEffectComponent
    {
        int ModifyDamage(int currentDamage, in DamageModifierContext context);
    }
}
