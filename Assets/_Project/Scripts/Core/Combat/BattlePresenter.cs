using System;
namespace SlotRogue.Core.Combat
{
    public sealed class BattlePresenter
    {
        public event Action<CombatEvent> CombatEventEmitted;

        public event Action<BattleStateSnapshot> TurnCompleted;

        public void Consume(TurnResult result)
        {
            if (result == null)
            {
                return;
            }

            foreach (CombatEvent combatEvent in result.Events)
            {
                CombatEventEmitted?.Invoke(combatEvent);
            }

            TurnCompleted?.Invoke(result.FinalState);
        }
    }
}
