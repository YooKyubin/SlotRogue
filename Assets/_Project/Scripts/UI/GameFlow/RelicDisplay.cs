using System.Text;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 유물 선택/보상 카드에 표시할 텍스트를 만든다.
    /// 이름·등급·역할·대상 심볼/태그·조건·효과 설명을 한 곳에서 포맷한다.
    /// </summary>
    public static class RelicDisplay
    {
        /// <summary>카드 본문(설명) 텍스트. 등급/역할/대상 + 조건→효과 설명.</summary>
        public static string BuildDescription(RelicDefinition relic)
        {
            if (relic == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.Append('[').Append(GradeKorean(relic.Grade)).Append(" · ")
                .Append(RoleKorean(relic.Role)).Append(']');

            string target = BuildTarget(relic);
            if (!string.IsNullOrEmpty(target))
            {
                builder.Append("  ").Append(target);
            }

            if (!string.IsNullOrEmpty(relic.Description))
            {
                builder.Append('\n').Append(relic.Description);
            }

            return builder.ToString();
        }

        private static string BuildTarget(RelicDefinition relic)
        {
            if (relic.TriggerSymbol.HasValue)
            {
                return SymbolKorean(relic.TriggerSymbol.Value);
            }

            if (relic.TriggerTag.HasValue)
            {
                return SymbolTagMap.ToKorean(relic.TriggerTag.Value) + " 태그";
            }

            return "공통";
        }

        public static string GradeKorean(RelicGrade grade) => grade switch
        {
            RelicGrade.Starter => "시작",
            RelicGrade.Common => "일반",
            RelicGrade.Uncommon => "비일반",
            RelicGrade.Rare => "레어",
            RelicGrade.Legendary => "전설",
            RelicGrade.Curse => "저주",
            _ => grade.ToString(),
        };

        public static string RoleKorean(RelicRole role) => role switch
        {
            RelicRole.Damage => "피해",
            RelicRole.Defense => "방어",
            RelicRole.Heal => "회복",
            RelicRole.Status => "상태이상",
            RelicRole.Utility => "유틸",
            _ => role.ToString(),
        };

        public static string SymbolKorean(SlotSymbolType symbol) => symbol switch
        {
            SlotSymbolType.Cherry => "체리",
            SlotSymbolType.Seven => "세븐",
            SlotSymbolType.Diamond => "다이아",
            SlotSymbolType.Bell => "종",
            SlotSymbolType.Clover => "네잎클로버",
            SlotSymbolType.Lemon => "레몬",
            _ => symbol.ToString(),
        };
    }
}
