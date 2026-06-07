using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.Relics.Data
{
    /// <summary>
    /// 심볼 ↔ 그룹 매핑을 정의하는 단일 출처(single source of truth).
    /// <para>
    /// 스펙상 보물 그룹은 (Bell, Diamond)지만, 현재 프로젝트의 <see cref="SlotSymbolType"/>에는
    /// Diamond가 없고 그 자리에 Grape가 있다. 추후 심볼을 Diamond로 교체할 경우
    /// <b>이 클래스만 수정</b>하면 유물 시스템 전체가 따라간다.
    /// </para>
    /// </summary>
    public static class RelicSymbolGroups
    {
        // 그룹 정의: 과일(Cherry, Lemon) / 행운(Seven, Clover) / 보물(Bell, Grape=Diamond 자리).
        private static readonly Dictionary<SlotSymbolType, RelicSymbolGroup> GroupBySymbol =
            new Dictionary<SlotSymbolType, RelicSymbolGroup>
            {
                { SlotSymbolType.Cherry, RelicSymbolGroup.Fruit },
                { SlotSymbolType.Lemon, RelicSymbolGroup.Fruit },
                { SlotSymbolType.Seven, RelicSymbolGroup.Luck },
                { SlotSymbolType.Clover, RelicSymbolGroup.Luck },
                { SlotSymbolType.Bell, RelicSymbolGroup.Treasure },
                { SlotSymbolType.Grape, RelicSymbolGroup.Treasure },
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
