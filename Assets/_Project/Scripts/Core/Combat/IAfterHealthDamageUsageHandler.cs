namespace SlotRogue.Core.Combat
{
    public interface IAfterHealthDamageUsageHandler : IStatusEffectComponent
    {
        void OnAfterHealthDamageUsed(StatusEffectContext context);
    }
}
