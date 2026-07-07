using System.Text;
using SlotRogue.Relics.Pool;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 진행 중인 런 상태(<see cref="GameFlowSession"/>)를 화면 표시용 문자열로 포매팅합니다.
    /// 도메인 세션은 상태만 보유하고, 한국어/레이아웃 같은 프레젠테이션 책임은 여기로 모읍니다.
    /// </summary>
    public static class RunSummaryPresenter
    {
        public static string BuildRunSummary()
        {
            return
                $"HP {GameFlowSession.PlayerCurrentHp}/{GameFlowSession.PlayerMaxHp}\n" +
                $"진입 전투: {GameFlowSession.BattleIndex}\n" +
                $"승리: {GameFlowSession.Victories}\n" +
                $"보상: {GameFlowSession.RewardsClaimed}\n" +
                $"COIN: {GameFlowSession.RunCoins}\n" +
                $"현재 전투: {GameFlowSession.CurrentBattleNumber}\n" +
                $"전투 등급: {GameFlowSession.CurrentTier}\n" +
                $"부활 사용: {GameFlowSession.HasRevivedThisRun}\n" +
                $"보유 유물: {BuildRelicSummary()}\n" +
                $"심볼 한 칸 확률: {GameFlowSession.SlotPool.BuildSummary()}";
        }

        private static string BuildRelicSummary()
        {
            var relics = GameFlowSession.OwnedRelics;
            if (relics.Count == 0)
            {
                return $"0/{GameFlowSession.RelicSlotCapacity}";
            }

            var builder = new StringBuilder();
            builder.Append(relics.Count);
            builder.Append('/');
            builder.Append(GameFlowSession.RelicSlotCapacity);
            builder.Append(" - ");
            for (int index = 0; index < relics.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(relics[index].Name);
            }

            return builder.ToString();
        }
    }
}
