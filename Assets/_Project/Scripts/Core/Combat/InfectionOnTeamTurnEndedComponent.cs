namespace SlotRogue.Core.Combat
{
    public sealed class InfectionOnTeamTurnEndedComponent : StatusEffectComponent, ITeamTurnEnded
    {
        public void OnTeamTurnEnded(StatusEffectContext context)
        {
            int stackCount = context.Instance.StackCount;
            if (stackCount <= 0)
            {
                return;
            }

            context.DealDamage(stackCount);
            context.Instance.StackCount = stackCount - 1;

            if (context.Instance.StackCount == 0)
            {
                context.RequestExpiration();
            }
        }
    }
}
