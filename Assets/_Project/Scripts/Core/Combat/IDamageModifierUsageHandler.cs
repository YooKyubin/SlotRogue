namespace SlotRogue.Core.Combat
{
    public interface IDamageModifierUsageHandler : IStatusEffectComponent
    {
        void OnDamageModifierUsed(StatusEffectContext context);
    }
}
