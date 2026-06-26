using SlotRogue.Relics.Pool;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 보상 슬롯의 테두리/배경 색을 결정하는 UI용 등급. 레퍼런스(흰/초록/파랑/보라/금/빨강)에 대응.
    /// 데이터 등급(<see cref="RelicGrade"/>)을 표시용으로 매핑한 값이다.
    /// </summary>
    public enum RewardRarity
    {
        Common = 0,     // 흰색
        Uncommon = 1,   // 초록
        Rare = 2,       // 파랑
        Epic = 3,       // 보라
        Legendary = 4,  // 금
        Curse = 5,      // 빨강
    }

    public static class RewardRarityMap
    {
        /// <summary>유물 등급 → 표시용 보상 등급.</summary>
        public static RewardRarity FromGrade(RelicGrade grade)
        {
            switch (grade)
            {
                case RelicGrade.Uncommon:
                    return RewardRarity.Uncommon;
                case RelicGrade.Rare:
                    return RewardRarity.Rare;
                case RelicGrade.Legendary:
                    return RewardRarity.Legendary;
                case RelicGrade.Curse:
                    return RewardRarity.Curse;
                case RelicGrade.Starter:
                case RelicGrade.Common:
                default:
                    return RewardRarity.Common;
            }
        }
    }
}
