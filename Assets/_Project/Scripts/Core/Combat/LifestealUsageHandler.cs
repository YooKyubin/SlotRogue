namespace SlotRogue.Core.Combat
{
    public sealed class LifestealUsageHandler : StatusEffectComponent, IAfterHealthDamageUsageHandler
    {
        public void OnAfterHealthDamageUsed(StatusEffectContext context)
        {
            if (context.Instance.StackCount <= 0)
            {
                return;
            }

            context.Instance.StackCount--;
            if (context.Instance.StackCount > 0)
            {
                return;
            }

            for (int index = 0; index < context.Instance.Components.Count; index++)
            {
                context.Instance.Components[index].OnExpired(context);
            }

            context.Participant.RemoveStatusEffect(context.Instance);
            context.EmitExpired();
        }
    }
}
