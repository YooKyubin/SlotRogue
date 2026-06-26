namespace SlotRogue.Core.Combat
{
    public interface IAfterStatusApplied : IStatusEffectComponent
    {
        void OnAfterStatusApplied(StatusEffectContext context);
    }
}
