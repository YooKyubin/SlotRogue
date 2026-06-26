namespace SlotRogue.Core.Combat
{
    public sealed class VulnerableUsageHandler : StatusEffectComponent, IDamageModifierUsageHandler
    {
        public void OnDamageModifierUsed(StatusEffectContext context)
        {
            if (context.Instance.StackCount <= 0)
            {
                return;
            }

            context.Instance.StackCount--;
            if (context.Instance.StackCount > 0)
            {
                context.EmitValueChanged();
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
