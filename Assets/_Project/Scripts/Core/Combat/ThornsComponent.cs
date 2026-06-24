namespace SlotRogue.Core.Combat
{
    public sealed class ThornsComponent : StatusEffectComponent, IAfterDirectDamageReceived
    {
        private const int TriggerPercent = 50;

        public void OnAfterDirectDamageReceived(AfterDirectDamageReceivedContext context)
        {
            if (context.DamageOrigin != DamageOrigin.DirectAction ||
                context.StatusSnapshot.Magnitude <= 0 ||
                !context.RollPercent(TriggerPercent))
            {
                return;
            }

            context.ReflectDamage(context.StatusSnapshot.Magnitude);
        }
    }
}
