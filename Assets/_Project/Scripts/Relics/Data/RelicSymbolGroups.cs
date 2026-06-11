using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.Relics.Data
{
    /// <summary>
    /// 심볼 ↔ 그룹 매핑을 정의하는 단일 출처(single source of truth).
    /// <para>
    /// 보물 그룹은 (Bell, Diamond)다. (v20.3에서 Grape를 Diamond로 전면 교체)
    /// </para>
    /// </summary>
    public static class RelicSymbolGroups
    {
        // 그룹 정의: 과일(Cherry, Lemon) / 행운(Seven, Clover) / 보물(Bell, Diamond).
        private static readonly Dictionary<SlotSymbolType, RelicSymbolGroup> GroupBySymbol =
            new Dictionary<SlotSymbolType, RelicSymbolGroup>
            {
                { SlotSymbolType.Cherry, RelicSymbolGroup.Fruit },
                { SlotSymbolType.Lemon, RelicSymbolGroup.Fruit },
                { SlotSymbolType.Seven, RelicSymbolGroup.Luck },
                { SlotSymbolType.Clover, RelicSymbolGroup.Luck },
                { SlotSymbolType.Bell, RelicSymbolGroup.Treasure },
                { SlotSymbolType.Diamond, RelicSymbolGroup.Treasure },
            };

        /// <summary>심볼이 속한 그룹을 반환한다. 매핑이 없으면 None.</summary>
        public static RelicSymbolGroup GetGroup(SlotSymbolType symbol)
        {
            return GroupBySymbol.TryGetValue(symbol, out RelicSymbolGroup group)
                ? group
                : RelicSymbolGroup.None;
        }

        /// <summary>심볼이 지정한 그룹에 속하는지 여부.</summary>
        public static bool IsInGroup(SlotSymbolType symbol, RelicSymbolGroup group)
        {
            return group != RelicSymbolGroup.None && GetGroup(symbol) == group;
        }

        /// <summary>지정한 그룹에 속한 심볼 목록을 반환한다.</summary>
        public static IReadOnlyList<SlotSymbolType> GetSymbols(RelicSymbolGroup group)
        {
            var result = new List<SlotSymbolType>();

            foreach (KeyValuePair<SlotSymbolType, RelicSymbolGroup> pair in GroupBySymbol)
            {
                if (pair.Value == group)
                {
                    result.Add(pair.Key);
                }
            }

            return result;
        }
    }
}
