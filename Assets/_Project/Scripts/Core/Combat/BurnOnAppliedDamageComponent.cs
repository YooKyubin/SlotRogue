namespace SlotRogue.Core.Combat
{
    public sealed class BurnOnAppliedDamageComponent : StatusEffectComponent, IAfterStatusApplied
    {
        public void OnAfterStatusApplied(StatusEffectContext context)
        {
            if (context.Instance.Magnitude > 0)
            {
                context.DealDamage(context.Instance.Magnitude);
            }
        }
    }
}
