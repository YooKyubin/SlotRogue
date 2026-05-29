using System;
namespace SlotRogue.Core.Combat
{
    public sealed class BattlePresenter
    {
        public event Action<TurnResult> TurnReceived;

        public event Action<BattleStateSnapshot> TurnCompleted;

        public void Consume(TurnResult result)
        {
            if (result == null)
            {
                return;
            }

            TurnReceived?.Invoke(result);
            TurnCompleted?.Invoke(result.FinalState);
        }
    }
}
