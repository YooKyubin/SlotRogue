namespace SlotRogue.Core.Combat
{
    public sealed class BurnOnTeamTurnEndedComponent : StatusEffectComponent, ITeamTurnEnded
    {
        public void OnTeamTurnEnded(StatusEffectContext context)
        {
            if (context.Instance.Magnitude > 0)
            {
                context.DealDamage(context.Instance.Magnitude);
            }

            context.RequestExpiration();
        }
    }
}
