namespace SlotRogue.Core.Combat
{
    public interface IAfterHealthDamageDealt : IStatusEffectComponent
    {
        bool OnAfterHealthDamageDealt(AfterHealthDamageContext context);
    }
}
