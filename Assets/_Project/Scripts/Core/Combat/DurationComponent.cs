namespace SlotRogue.Core.Combat
{
    public sealed class DurationComponent : StatusEffectComponent
    {
        public override void OnTurnEnd(StatusEffectContext context)
        {
            if (context.Instance.RemainingTurns > 0)
            {
                context.Instance.RemainingTurns--;
                if (context.Instance.RemainingTurns > 0)
                {
                    context.EmitValueChanged();
                }
            }
        }
    }
}
