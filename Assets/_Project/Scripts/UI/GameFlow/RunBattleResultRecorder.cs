using SlotRogue.Core.Combat;

namespace SlotRogue.UI.GameFlow
{
    internal sealed class RunBattleResultRecorder
    {
        internal void Record(BattleFlowResult result)
        {
            if (result.EndReason == BattleEndReason.Victory)
            {
                if (GameFlowSession.IsInfiniteMode)
                {
                    GameFlowSession.CompleteInfiniteVictory(result.RemainingPlayerHp);
                }
                else
                {
                    GameFlowSession.CompleteBattleVictory(result.RemainingPlayerHp);
                }

                return;
            }

            GameFlowSession.CompleteBattleDefeat();
        }
    }
}
