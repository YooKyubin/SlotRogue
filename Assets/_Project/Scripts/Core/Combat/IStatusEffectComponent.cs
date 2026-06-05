namespace SlotRogue.Core.Combat
{
    public interface IStatusEffectComponent
    {
        void OnApplied(StatusEffectContext context);

        void OnTurnStart(StatusEffectContext context);

        bool ShouldSkipAction(StatusEffectContext context);

        void OnTurnEnd(StatusEffectContext context);

        void OnExpired(StatusEffectContext context);
    }
}
