using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.Relics.Pool
{
    /// <summary>
    /// 유물 발동에 필요한 런타임 상태(족보 무관). 조건 평가기가 읽는 값 묶음이다.
    /// 상위(전투/경제) 레이어가 매 판정 시점에 채워 넘긴다.
    /// </summary>
    public readonly struct RelicRuntimeContext
    {
        public RelicRuntimeContext(
            bool swappedThisSpin,
            bool swappedThisBattle,
            int coinCount,
            int turnIndex,
            bool isFirstSpinOfBattle,
            int activePatternCount)
        {
            SwappedThisSpin = swappedThisSpin;
            SwappedThisBattle = swappedThisBattle;
            CoinCount = coinCount;
            TurnIndex = turnIndex;
            IsFirstSpinOfBattle = isFirstSpinOfBattle;
            ActivePatternCount = activePatternCount;
        }

        public bool SwappedThisSpin { get; }
        public bool SwappedThisBattle { get; }
        public int CoinCount { get; }

        /// <summary>이번 전투에서 몇 번째 플레이어 턴인지(1부터).</summary>
        public int TurnIndex { get; }

        public bool IsFirstSpinOfBattle { get; }

        /// <summary>이번 스핀에 발동한 족보 수.</summary>
        public int ActivePatternCount { get; }
    }

    /// <summary>
    /// 조건 평가에 필요한 족보 요약. 상위 레이어가 <c>SlotPatternMatch</c>에서 뽑아 만든다.
    /// (평가기가 슬롯 내부 타입에 직접 의존하지 않도록 얇은 뷰로 분리.)
    /// </summary>
    public readonly struct RelicPatternView
    {
        public static readonly RelicPatternView None = default;

        public RelicPatternView(
            SlotSymbolType symbol,
            int size,
            bool madeBySwap,
            bool wholeLineSameSymbol,
            int baseDamage = 0)
        {
            Symbol = symbol;
            Size = size;
            MadeBySwap = madeBySwap;
            WholeLineSameSymbol = wholeLineSameSymbol;
            BaseDamage = baseDamage;
            HasPattern = size > 0;
        }

        public bool HasPattern { get; }
        public SlotSymbolType Symbol { get; }

        /// <summary>일치 칸 수(3/4/5…).</summary>
        public int Size { get; }

        public bool MadeBySwap { get; }
        public bool WholeLineSameSymbol { get; }

        /// <summary>이 족보의 기본 피해(배율 적용 전). 배율 피해 모델(P2)에서 족보별로 곱한다.</summary>
        public int BaseDamage { get; }
    }

    /// <summary>유물 조건(AND)을 런타임 상태·족보에 대해 평가한다. 순수 함수(부작용 없음).</summary>
    public static class RelicConditionEvaluator
    {
        public static bool Passes(
            IReadOnlyList<RelicCondition> conditions,
            in RelicRuntimeContext context,
            in RelicPatternView pattern)
        {
            if (conditions == null || conditions.Count == 0)
            {
                return true;
            }

            for (int index = 0; index < conditions.Count; index++)
            {
                if (!EvaluateOne(conditions[index], context, pattern))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool EvaluateOne(
            RelicCondition condition,
            in RelicRuntimeContext context,
            in RelicPatternView pattern)
        {
            switch (condition.Kind)
            {
                case RelicConditionKind.None:
                    return true;
                case RelicConditionKind.SwapUsedThisSpin:
                    return context.SwappedThisSpin;
                case RelicConditionKind.NoSwapThisSpin:
                    return !context.SwappedThisSpin;
                case RelicConditionKind.NoSwapThisBattle:
                    return !context.SwappedThisBattle;
                case RelicConditionKind.PatternMadeBySwap:
                    return pattern.HasPattern && pattern.MadeBySwap;
                case RelicConditionKind.PatternSizeEquals:
                    return pattern.HasPattern && pattern.Size == condition.Value;
                case RelicConditionKind.PatternSizeAtLeast:
                    return pattern.HasPattern && pattern.Size >= condition.Value;
                case RelicConditionKind.PatternContainsSymbol:
                    return pattern.HasPattern && ContainsSymbol(condition.Symbols, pattern.Symbol);
                case RelicConditionKind.WholeLineSameSymbol:
                    return pattern.HasPattern && pattern.WholeLineSameSymbol;
                case RelicConditionKind.IsFirstSpinOfBattle:
                    return context.IsFirstSpinOfBattle;
                case RelicConditionKind.TurnIndexEquals:
                    return context.TurnIndex == condition.Value;
                case RelicConditionKind.CoinsAtLeast:
                    return context.CoinCount >= condition.Value;
                case RelicConditionKind.ActivePatternCountAtLeast:
                    return context.ActivePatternCount >= condition.Value;
                default:
                    return false;
            }
        }

        private static bool ContainsSymbol(IReadOnlyList<SlotSymbolType> symbols, SlotSymbolType target)
        {
            if (symbols == null || symbols.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < symbols.Count; index++)
            {
                if (symbols[index] == target)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
