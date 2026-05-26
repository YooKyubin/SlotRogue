namespace SlotRogue.Core.Combat
{
    /// <summary>
    /// Slot calls once per resolved spin. Phase B: implemented by BattleResolver.
    /// </summary>
    public interface ISpinCombatConsumer
    {
        void OnSpinResolved(CombatSpinOutcome outcome);
    }
}
