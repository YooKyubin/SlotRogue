using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.Relics.Pool
{
    /// <summary>
    /// 심볼 ↔ 태그 매핑의 단일 출처(v20.3 HTML 기준).
    /// 체리=[과일,붉은] 레몬=[과일,노란] 클로버=[행운,푸른] 7=[행운,붉은] 다이아=[보물,푸른] 종=[보물,노란].
    /// </summary>
    public static class SymbolTagMap
    {
        private static readonly Dictionary<SlotSymbolType, SymbolTag[]> TagsBySymbol =
            new Dictionary<SlotSymbolType, SymbolTag[]>
            {
                { SlotSymbolType.Cherry,  new[] { SymbolTag.Fruit,    SymbolTag.Red } },
                { SlotSymbolType.Lemon,   new[] { SymbolTag.Fruit,    SymbolTag.Yellow } },
                { SlotSymbolType.Clover,  new[] { SymbolTag.Luck,     SymbolTag.Blue } },
                { SlotSymbolType.Seven,   new[] { SymbolTag.Luck,     SymbolTag.Red } },
                { SlotSymbolType.Diamond, new[] { SymbolTag.Treasure, SymbolTag.Blue } },
                { SlotSymbolType.Bell,    new[] { SymbolTag.Treasure, SymbolTag.Yellow } },
            };

        private static readonly SymbolTag[] Empty = System.Array.Empty<SymbolTag>();

        /// <summary>심볼이 가진 태그 목록.</summary>
        public static SymbolTag[] TagsOf(SlotSymbolType symbol) =>
            TagsBySymbol.TryGetValue(symbol, out SymbolTag[] tags) ? tags : Empty;

        /// <summary>심볼이 특정 태그를 가지는지.</summary>
        public static bool HasTag(SlotSymbolType symbol, SymbolTag tag)
        {
            SymbolTag[] tags = TagsOf(symbol);
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == tag)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>특정 태그를 가진 모든 심볼.</summary>
        public static IReadOnlyList<SlotSymbolType> SymbolsWithTag(SymbolTag tag)
        {
            var result = new List<SlotSymbolType>();
            foreach (KeyValuePair<SlotSymbolType, SymbolTag[]> pair in TagsBySymbol)
            {
                for (int i = 0; i < pair.Value.Length; i++)
                {
                    if (pair.Value[i] == tag)
                    {
                        result.Add(pair.Key);
                        break;
                    }
                }
            }

            return result;
        }

        public static string ToKorean(SymbolTag tag) => tag switch
        {
            SymbolTag.Fruit => "과일",
            SymbolTag.Luck => "행운",
            SymbolTag.Treasure => "보물",
            SymbolTag.Red => "붉은",
            SymbolTag.Yellow => "노란",
            SymbolTag.Blue => "푸른",
            _ => tag.ToString(),
        };
    }
}
