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
            GameFlowSession.RecordSlotSymbolContributions(result.SlotSymbolContributions);

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

                GameFlowSession.ApplyKillRelicHeal();
                GameFlowSession.TickRelicWaveLifetimes();
                return;
            }

            GameFlowSession.BeginBattleDefeat();
            _pendingDefeatSnapshot = LeaderboardRunSnapshot.Capture(
                GameFlowSession.Victories,
                GameFlowSession.CurrentBattleNumber,
                GameFlowSession.OwnedRelics,
                GameFlowSession.SlotPool);

            if (!GameFlowSession.CanRevive)
            {
                FinalizePendingDefeat();
                return;
            }

            // 부활 제안이 뜬 경우, 대기 중 강제 종료/광고 전환으로 최종 정산(FinalizePendingDefeat)이
            // 실행되지 않아도 기록이 사라지지 않도록 사망 시점에 점수를 즉시 제출한다.
            // 부활해서 더 나아가면 다음 사망의 정산에서 더 높은 점수로 다시 제출된다
            // (리더보드/로컬 최고기록은 최고값만 유지하므로 중복 제출은 무해하다).
            SlotRogueLeaderboardService.QueueRunSubmission(_pendingDefeatSnapshot.Value);
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
                    GameFlowSession.OwnedRelics,
                    GameFlowSession.SlotPool);

            GameFlowSession.CompleteBattleDefeat();
            SlotRogueLeaderboardService.QueueRunSubmission(snapshot);
            _pendingDefeatSnapshot = null;
        }
    }
}
