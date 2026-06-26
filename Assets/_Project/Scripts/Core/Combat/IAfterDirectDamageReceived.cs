namespace SlotRogue.Core.Combat
{
    public interface IAfterDirectDamageReceived : IStatusEffectComponent
    {
        void OnAfterDirectDamageReceived(AfterDirectDamageReceivedContext context);
    }
}
