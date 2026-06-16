using SlotRogue.Core.Combat;
using SlotRogue.UI.Leaderboard;

namespace SlotRogue.UI.GameFlow
{
    internal sealed class RunBattleResultRecorder
    {
        private LeaderboardRunSnapshot? _pendingDefeatSnapshot;

        internal void Record(BattleFlowResult result)
        {
            GameFlowSession.RecordRelicContributions(result.RelicContributions);

            if (result.EndReason == BattleEndReason.Victory)
            {
                _pendingDefeatSnapshot = null;
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

            GameFlowSession.BeginBattleDefeat();
            _pendingDefeatSnapshot = LeaderboardRunSnapshot.Capture(
                GameFlowSession.Victories,
                GameFlowSession.CurrentBattleNumber,
                GameFlowSession.OwnedRelics);

            if (!GameFlowSession.CanRevive)
            {
                FinalizePendingDefeat();
            }
        }

        internal void CancelPendingDefeat()
        {
            _pendingDefeatSnapshot = null;
        }

        internal void FinalizePendingDefeat()
        {
            if (!GameFlowSession.IsDefeatPending)
            {
                return;
            }

            LeaderboardRunSnapshot snapshot = _pendingDefeatSnapshot ??
                LeaderboardRunSnapshot.Capture(
                    GameFlowSession.Victories,
                    GameFlowSession.CurrentBattleNumber,
                    GameFlowSession.OwnedRelics);

            GameFlowSession.CompleteBattleDefeat();
            SlotRogueLeaderboardService.QueueRunSubmission(snapshot);
            _pendingDefeatSnapshot = null;
        }
    }
}
