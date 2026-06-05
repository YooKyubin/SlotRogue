namespace SlotRogue.Core.Combat
{
    public abstract class StatusEffectComponent : IStatusEffectComponent
    {
        public virtual void OnApplied(StatusEffectContext context)
        {
        }

        public virtual void OnTurnStart(StatusEffectContext context)
        {
        }

        public virtual bool ShouldSkipAction(StatusEffectContext context) => false;

        public virtual void OnTurnEnd(StatusEffectContext context)
        {
        }

        public virtual void OnExpired(StatusEffectContext context)
        {
        }
    }
}
