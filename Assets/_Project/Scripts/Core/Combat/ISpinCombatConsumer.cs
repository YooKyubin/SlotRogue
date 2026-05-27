namespace SlotRogue.Core.Combat
{
    /// <summary>
    /// Slot calls once per resolved spin. Implemented by BattleResolver.
    /// </summary>
    public interface ISpinCombatConsumer
    {
        void OnSpinResolved(CombatSpinOutcome outcome);
    }
}
