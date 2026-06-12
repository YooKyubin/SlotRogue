using System.Collections.Generic;
using SlotRogue.Data.GameFlow;
using SlotRogue.Relics.Pool;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 전투 보상 풀(v23). 보상은 유물 획득으로 통일한다.
    /// 등급은 전투 등급(Tier)에 따라 분리한다:
    ///  - 일반: Common
    ///  - 엘리트: Common + Uncommon
    ///  - 보스: Uncommon + Rare 이상
    /// 단, Phase 1에서 미구현(<see cref="RelicDefinition.Phase1"/> == false)인 유물은
    /// <see cref="RelicCatalog.RewardByGrade"/>가 이미 제외하므로 보상풀에 등장하지 않는다.
    /// (현재 Rare/Legendary/Curse는 전부 Phase 2라 보스 풀은 Uncommon 중심으로 구성된다.)
    /// </summary>
    public static class RunRewardCatalog
    {
        private static readonly RelicGrade[] NormalGrades = { RelicGrade.Common };
        private static readonly RelicGrade[] EliteGrades = { RelicGrade.Common, RelicGrade.Uncommon };
        private static readonly RelicGrade[] BossGrades =
        {
            RelicGrade.Uncommon, RelicGrade.Rare, RelicGrade.Legendary, RelicGrade.Curse,
        };

        /// <summary>현재 전투 등급에 맞는 유물 보상 후보 목록.</summary>
        public static IReadOnlyList<RunRewardDefinition> ForTier(EncounterTier tier)
        {
            RelicGrade[] grades = tier switch
            {
                EncounterTier.Boss => BossGrades,
                EncounterTier.Elite => EliteGrades,
                _ => NormalGrades,
            };

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
    }
}
