namespace SlotRogue.Core.Combat
{
    public interface IIncomingDamageModifier : IStatusEffectComponent
    {
        int ModifyDamage(int currentDamage, in DamageModifierContext context);
    }
}
