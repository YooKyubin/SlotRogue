namespace SlotRogue.Core.Combat
{
    public sealed class ThornsComponent : StatusEffectComponent, IAfterDirectDamageReceived, IExpireOnOpponentTeamTurnEnd
    {
        private const int TriggerPercent = 100;

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
