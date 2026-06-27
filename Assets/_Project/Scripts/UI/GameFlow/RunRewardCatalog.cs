using System.Collections.Generic;
using SlotRogue.Data.GameFlow;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 전투 보상 풀(v23). 보상 종류는 전투 등급(Tier)으로 분리한다:
    ///  - 일반: 심볼 추가/제거 (슬롯 풀 조작 — 유물 없음)
    ///  - 엘리트: 유물 Common + Uncommon
    ///  - 보스: 유물 Uncommon + Rare 이상
    /// 단, Phase 1에서 미구현(<see cref="RelicDefinition.Phase1"/> == false)인 유물은
    /// <see cref="RelicCatalog.RewardByGrade"/>가 이미 제외하므로 보상풀에 등장하지 않는다.
    /// (보스 풀은 Uncommon 중심이며, 전투 코어 없이 성립하는 흡혈/방어전환 전설
    ///  L-04 수호 천칭·L-05 흡혈 왕관이 Phase 1로 추가되어 보스 풀에 함께 등장한다.
    ///  나머지 Rare/Legendary/Curse는 상태이상·배율 의존이라 아직 Phase 2.)
    /// </summary>
    public static class RunRewardCatalog
    {
        /// <summary>심볼 제거 보상이 유지해야 하는 최소 개수. 풀에서 심볼이 완전히 사라지는 것을 막는다.</summary>
        public const int MinSymbolCountAfterRemove = 1;

        private static readonly RelicGrade[] EliteGrades = { RelicGrade.Common, RelicGrade.Uncommon };
        private static readonly RelicGrade[] BossGrades =
        {
            RelicGrade.Uncommon, RelicGrade.Rare, RelicGrade.Legendary, RelicGrade.Curse,
        };

        /// <summary>현재 전투 등급에 맞는 보상 후보 목록.</summary>
        public static IReadOnlyList<RunRewardDefinition> ForTier(EncounterTier tier)
        {
            if (tier is EncounterTier.Elite or EncounterTier.Boss)
            {
                return BuildRelicOptions(tier == EncounterTier.Boss ? BossGrades : EliteGrades);
            }

            return BuildSymbolOptions(GameFlowSession.SlotPool);
        }

        private static IReadOnlyList<RunRewardDefinition> BuildRelicOptions(RelicGrade[] grades)
        {
            var list = new List<RunRewardDefinition>();
            for (int gradeIndex = 0; gradeIndex < grades.Length; gradeIndex++)
            {
                IReadOnlyList<RelicDefinition> relics = RelicCatalog.RewardByGrade(grades[gradeIndex]);
                for (int relicIndex = 0; relicIndex < relics.Count; relicIndex++)
                {
                    list.Add(new RunRewardDefinition(relics[relicIndex]));
                }
            }

            return list;
        }

        /// <summary>
        /// 일반 전투 보상: 심볼 6종 각각의 추가(+1)와, 현재 개수가 바닥보다 큰 심볼의 제거(-1).
        /// 추가는 그 심볼 빌드를 키우고, 제거는 나머지 심볼의 상대 확률을 올린다(덱 압축).
        /// </summary>
        private static IReadOnlyList<RunRewardDefinition> BuildSymbolOptions(SlotSymbolPool pool)
        {
            var list = new List<RunRewardDefinition>();
            IReadOnlyList<SlotSymbolType> symbols = SlotSymbolPool.Symbols;

            for (int index = 0; index < symbols.Count; index++)
            {
                SlotSymbolType symbol = symbols[index];
                string korean = RelicDisplay.SymbolKorean(symbol);
                int count = pool != null ? pool.GetCount(symbol) : 0;

                list.Add(new RunRewardDefinition(
                    symbol,
                    1,
                    $"{korean} 추가",
                    $"슬롯 풀에 {korean} 심볼 1개를 추가합니다. (현재 {count}개)"));

                if (count > MinSymbolCountAfterRemove)
                {
                    list.Add(new RunRewardDefinition(
                        symbol,
                        -1,
                        $"{korean} 제거",
                        $"슬롯 풀에서 {korean} 심볼 1개를 제거합니다. (현재 {count}개)"));
                }
            }

            return list;
        }
    }
}
