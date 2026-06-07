using System.Collections.Generic;
using SlotRogue.Relics.Data;
using SlotRogue.Slot.Data;

namespace SlotRogue.Relics
{
    /// <summary>유물 조건 데이터를 읽어 현재 컨텍스트에서 발동 가능한지 판정한다.</summary>
    public sealed class RelicConditionChecker
    {
        /// <summary>조건이 충족되는지 여부.</summary>
        public bool IsMet(RelicConditionData condition, RelicContext context)
        {
            if (condition == null || context == null)
            {
                return false;
            }

            if (!EvaluateBase(condition, context))
            {
                return false;
            }

            // 추가 게이트(모든 조건에 AND).
            if (condition.HpBelowPercentGate > 0 && !IsHpBelow(context, condition.HpBelowPercentGate))
            {
                return false;
            }

            if (condition.EnemyStatusGate != RelicStatusType.None &&
                !context.HasEnemyStatus(condition.EnemyStatusGate))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// ApplyMode가 PerMatchedPattern일 때 사용할 "일치 족보 수"를 계산한다.
        /// </summary>
        public int GetMatchedPatternCount(RelicConditionData condition, RelicContext context)
        {
            if (condition == null || context == null)
            {
                return 0;
            }

            switch (condition.Type)
            {
                case RelicConditionType.SpecificSymbol:
                    return RelicPatternQuery.CountSymbol(context.Patterns, condition.TargetSymbol);
                case RelicConditionType.SymbolGroup:
                    return RelicPatternQuery.CountGroup(context.Patterns, condition.Group);
                case RelicConditionType.SpecificPattern:
                    return RelicPatternQuery.CountRanks(context.Patterns, condition.PatternRanks);
                default:
                    return context.Patterns?.Count ?? 0;
            }
        }

        private bool EvaluateBase(RelicConditionData condition, RelicContext context)
        {
            int minCount = condition.MinCount < 1 ? 1 : condition.MinCount;

            switch (condition.Type)
            {
                case RelicConditionType.Always:
                    return true;

                case RelicConditionType.AnyPattern:
                    return PatternCount(context) >= minCount;

                case RelicConditionType.SpecificSymbol:
                    return RelicPatternQuery.CountSymbol(context.Patterns, condition.TargetSymbol) >= minCount;

                case RelicConditionType.SymbolGroup:
                    return RelicPatternQuery.CountGroup(context.Patterns, condition.Group) >= minCount;

                case RelicConditionType.SpecificPattern:
                    return RelicPatternQuery.CountRanks(context.Patterns, condition.PatternRanks) >= minCount;

                case RelicConditionType.PatternCount:
                    return EvaluatePatternCount(condition, context);

                case RelicConditionType.MultipleSpecificSymbolsInSameTurn:
                    return EvaluateMultipleSymbols(condition, context);

                case RelicConditionType.NoPattern:
                    return PatternCount(context) == 0;

                case RelicConditionType.HpBelowPercent:
                    return IsHpBelow(context, condition.HpPercentThreshold);

                case RelicConditionType.EnemyHasStatus:
                    return PatternCount(context) >= 1 &&
                        context.HasEnemyStatus(condition.RequiredEnemyStatus);

                case RelicConditionType.SymbolCountInPool:
                    return EvaluatePool(condition, context);

                default:
                    return false;
            }
        }

        private bool EvaluatePatternCount(RelicConditionData condition, RelicContext context)
        {
            switch (condition.PatternCountMode)
            {
                case PatternCountMode.Total:
                    return PatternCount(context) >= condition.MinCount;
                case PatternCountMode.DistinctSymbols:
                    return RelicPatternQuery.DistinctSymbolCount(context.Patterns) >= condition.MinCount;
                case PatternCountMode.SameSymbolRepeat:
                    return RelicPatternQuery.MaxSameSymbolCount(context.Patterns) >= condition.MinCount;
                default:
                    return false;
            }
        }

        private bool EvaluateMultipleSymbols(RelicConditionData condition, RelicContext context)
        {
            if (condition.Symbols == null || condition.Symbols.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < condition.Symbols.Count; index++)
            {
                if (RelicPatternQuery.CountSymbol(context.Patterns, condition.Symbols[index]) == 0)
                {
                    return false;
                }
            }

            return true;
        }

        private bool EvaluatePool(RelicConditionData condition, RelicContext context)
        {
            var pool = context.PoolSymbols;
            if (pool == null)
            {
                return false;
            }

            switch (condition.PoolQueryMode)
            {
                case PoolQueryMode.GroupSymbolCountAtLeast:
                {
                    int count = 0;
                    for (int index = 0; index < pool.Count; index++)
                    {
                        if (RelicSymbolGroups.IsInGroup(pool[index], condition.Group))
                        {
                            count++;
                        }
                    }

                    return count >= condition.MinCount;
                }

                case PoolQueryMode.DistinctSymbolTypesAtMost:
                {
                    var seen = new HashSet<SlotSymbolType>();
                    for (int index = 0; index < pool.Count; index++)
                    {
                        seen.Add(pool[index]);
                    }

                    return seen.Count <= condition.MinCount;
                }

                case PoolQueryMode.TotalSymbolCountAtLeast:
                    return pool.Count >= condition.MinCount;

                default:
                    return false;
            }
        }

        private static int PatternCount(RelicContext context)
        {
            return context.Patterns?.Count ?? 0;
        }

        private static bool IsHpBelow(RelicContext context, int percent)
        {
            if (context.PlayerMaxHp <= 0 || percent <= 0)
            {
                return false;
            }

            return context.PlayerCurrentHp * 100 <= context.PlayerMaxHp * percent;
        }
    }
}
