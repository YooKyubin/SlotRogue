namespace SlotRogue.Core.Combat
{
    public interface ITeamTurnEnded : IStatusEffectComponent
    {
        void OnTeamTurnEnded(StatusEffectContext context);
    }
}
