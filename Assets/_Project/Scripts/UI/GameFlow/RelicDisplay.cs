using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 유물 선택/보상 카드에 표시할 텍스트를 만든다.
    /// 카드 설명은 유물 원문 설명만 표시하고, 선택/보상 카드의 심볼 이름만 아이콘으로 치환한다.
    /// </summary>
    public static class RelicDisplay
    {
        /// <summary>카드 본문(설명) 텍스트. 유물 원문 설명만 반환한다.</summary>
        public static string BuildDescription(RelicDefinition relic)
        {
            if (relic == null)
            {
                return string.Empty;
            }

            return relic.Description ?? string.Empty;
        }

        public static string BuildSelectionDescription(RelicDefinition relic)
        {
            if (relic == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(relic.Description))
            {
                return string.Empty;
            }

            return relic.TriggerSymbol.HasValue
                ? ReplaceFirstSymbolName(
                    relic.Description,
                    relic.TriggerSymbol.Value)
                : relic.Description;
        }

        private static string ReplaceFirstSymbolName(
            string text,
            SlotSymbolType symbol)
        {
            string[] names = SymbolDescriptionNames(symbol);
            for (int index = 0; index < names.Length; index++)
            {
                string name = names[index];
                int nameIndex = text.IndexOf(name, System.StringComparison.Ordinal);
                if (nameIndex < 0)
                {
                    continue;
                }

                return text.Substring(0, nameIndex) +
                    SymbolInlineTag(symbol, name) +
                    text.Substring(nameIndex + name.Length);
            }

            return text;
        }

        private static string[] SymbolDescriptionNames(SlotSymbolType symbol) => symbol switch
        {
            SlotSymbolType.Cherry => new[] { "체리" },
            SlotSymbolType.Seven => new[] { "세븐" },
            SlotSymbolType.Diamond => new[] { "다이아" },
            SlotSymbolType.Bell => new[] { "종" },
            SlotSymbolType.Clover => new[] { "클로버" },
            SlotSymbolType.Lemon => new[] { "레몬" },
            _ => new[] { symbol.ToString() },
        };

        private static string SymbolInlineTag(SlotSymbolType symbol, string label)
        {
            return "<sprite index=" +
                SlotSymbolIconKeys.TmpSpriteIndexFor(symbol) +
                "> <color=" +
                SymbolColorHex(symbol) +
                ">" +
                label +
                "</color>";
        }

        public static string SymbolColorHex(SlotSymbolType symbol) => symbol switch
        {
            SlotSymbolType.Cherry => "#e52b2b",
            SlotSymbolType.Seven => "#bb1d1d",
            SlotSymbolType.Diamond => "#49e6f1",
            SlotSymbolType.Bell => "#d79837",
            SlotSymbolType.Clover => "#66e168",
            SlotSymbolType.Lemon => "#eef352",
            _ => "#FFFFFF",
        };

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
