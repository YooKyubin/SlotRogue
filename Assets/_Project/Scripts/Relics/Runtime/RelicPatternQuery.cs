using System.Collections.Generic;
using SlotRogue.Relics.Data;
using SlotRogue.Slot.Data;

namespace SlotRogue.Relics
{
    /// <summary>발동한 족보 목록에 대한 집계 헬퍼. 조건 체커와 효과 실행기가 공용으로 쓴다.</summary>
    public static class RelicPatternQuery
    {
        public static int CountSymbol(IReadOnlyList<SlotPatternMatch> patterns, SlotSymbolType symbol)
        {
            if (patterns == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < patterns.Count; index++)
            {
                if (patterns[index].Symbol == symbol)
                {
                    count++;
                }
            }

            return count;
        }

        public static int CountGroup(IReadOnlyList<SlotPatternMatch> patterns, RelicSymbolGroup group)
        {
            if (patterns == null || group == RelicSymbolGroup.None)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < patterns.Count; index++)
            {
                if (RelicSymbolGroups.IsInGroup(patterns[index].Symbol, group))
                {
                    count++;
                }
            }

            return count;
        }

        public static int CountRanks(IReadOnlyList<SlotPatternMatch> patterns, IReadOnlyList<SlotPatternRank> ranks)
        {
            if (patterns == null || ranks == null || ranks.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < patterns.Count; index++)
            {
                if (ContainsRank(ranks, patterns[index].Definition.Rank))
                {
                    count++;
                }
            }

            return count;
        }

        public static int DistinctSymbolCount(IReadOnlyList<SlotPatternMatch> patterns)
        {
            if (patterns == null || patterns.Count == 0)
            {
                return 0;
            }

            var seen = new HashSet<SlotSymbolType>();
            for (int index = 0; index < patterns.Count; index++)
            {
                seen.Add(patterns[index].Symbol);
            }

            return seen.Count;
        }

        /// <summary>같은 심볼 족보가 반복된 최대 개수.</summary>
        public static int MaxSameSymbolCount(IReadOnlyList<SlotPatternMatch> patterns)
        {
            if (patterns == null || patterns.Count == 0)
            {
                return 0;
            }

            var counts = new Dictionary<SlotSymbolType, int>();
            int max = 0;
            for (int index = 0; index < patterns.Count; index++)
            {
                SlotSymbolType symbol = patterns[index].Symbol;
                counts.TryGetValue(symbol, out int current);
                current++;
                counts[symbol] = current;
                if (current > max)
                {
                    max = current;
                }
            }

            return max;
        }

        /// <summary>가장 높은 피해(CalculatedValue) 족보의 피해량.</summary>
        public static int HighestPatternDamage(IReadOnlyList<SlotPatternMatch> patterns)
        {
            if (patterns == null || patterns.Count == 0)
            {
                return 0;
            }

            int max = 0;
            for (int index = 0; index < patterns.Count; index++)
            {
                int value = patterns[index].CalculatedValue;
                if (value > max)
                {
                    max = value;
                }
            }

            return max;
        }

        private static bool ContainsRank(IReadOnlyList<SlotPatternRank> ranks, SlotPatternRank rank)
        {
            for (int index = 0; index < ranks.Count; index++)
            {
                if (ranks[index] == rank)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
