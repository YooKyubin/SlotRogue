using NUnit.Framework;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.Tests.Relics
{
    /// <summary>부품 시스템 조건 평가기(<see cref="RelicConditionEvaluator"/>) 결정론 검증.</summary>
    public sealed class RelicConditionEvaluatorTests
    {
        private static RelicRuntimeContext Ctx(
            bool swappedThisSpin = false,
            bool swappedThisBattle = false,
            int coins = 0,
            int turn = 1,
            bool firstSpin = false,
            int patternCount = 1)
            => new(swappedThisSpin, swappedThisBattle, coins, turn, firstSpin, patternCount);

        private static RelicPatternView Pat(
            SlotSymbolType symbol = SlotSymbolType.Cherry,
            int size = 3,
            bool madeBySwap = false,
            bool wholeLine = false)
            => new(symbol, size, madeBySwap, wholeLine);

        private static bool Passes(RelicCondition condition, RelicRuntimeContext ctx, RelicPatternView pat)
            => RelicConditionEvaluator.Passes(new[] { condition }, ctx, pat);

        [Test]
        public void NoConditions_AlwaysPasses()
        {
            Assert.That(RelicConditionEvaluator.Passes(null, Ctx(), Pat()), Is.True);
            Assert.That(RelicConditionEvaluator.Passes(new RelicCondition[0], Ctx(), Pat()), Is.True);
        }

        [Test]
        public void CoinsAtLeast_PassesWhenEnough()
        {
            var c = new RelicCondition(RelicConditionKind.CoinsAtLeast, 6);
            Assert.That(Passes(c, Ctx(coins: 6), Pat()), Is.True);
            Assert.That(Passes(c, Ctx(coins: 5), Pat()), Is.False);
        }

        [Test]
        public void NoSwapThisBattle_PassesWhenNotSwapped()
        {
            var c = new RelicCondition(RelicConditionKind.NoSwapThisBattle);
            Assert.That(Passes(c, Ctx(swappedThisBattle: false), Pat()), Is.True);
            Assert.That(Passes(c, Ctx(swappedThisBattle: true), Pat()), Is.False);
        }

        [Test]
        public void SwapUsedThisSpin_PassesWhenSwapped()
        {
            var c = new RelicCondition(RelicConditionKind.SwapUsedThisSpin);
            Assert.That(Passes(c, Ctx(swappedThisSpin: true), Pat()), Is.True);
            Assert.That(Passes(c, Ctx(swappedThisSpin: false), Pat()), Is.False);
        }

        [Test]
        public void PatternSizeEquals_MatchesSize()
        {
            var c = new RelicCondition(RelicConditionKind.PatternSizeEquals, 3);
            Assert.That(Passes(c, Ctx(), Pat(size: 3)), Is.True);
            Assert.That(Passes(c, Ctx(), Pat(size: 4)), Is.False);
        }

        [Test]
        public void PatternContainsSymbol_MatchesSymbol()
        {
            var c = new RelicCondition(
                RelicConditionKind.PatternContainsSymbol,
                0,
                new[] { SlotSymbolType.Cherry, SlotSymbolType.Lemon });
            Assert.That(Passes(c, Ctx(), Pat(symbol: SlotSymbolType.Cherry)), Is.True);
            Assert.That(Passes(c, Ctx(), Pat(symbol: SlotSymbolType.Bell)), Is.False);
        }

        [Test]
        public void PatternMadeBySwap_RequiresPatternAndFlag()
        {
            var c = new RelicCondition(RelicConditionKind.PatternMadeBySwap);
            Assert.That(Passes(c, Ctx(), Pat(madeBySwap: true)), Is.True);
            Assert.That(Passes(c, Ctx(), Pat(madeBySwap: false)), Is.False);
            Assert.That(Passes(c, Ctx(), RelicPatternView.None), Is.False);
        }

        [Test]
        public void TurnIndexEquals_MatchesTurn()
        {
            var c = new RelicCondition(RelicConditionKind.TurnIndexEquals, 5);
            Assert.That(Passes(c, Ctx(turn: 5), Pat()), Is.True);
            Assert.That(Passes(c, Ctx(turn: 4), Pat()), Is.False);
        }

        [Test]
        public void ActivePatternCountAtLeast_Threshold()
        {
            var c = new RelicCondition(RelicConditionKind.ActivePatternCountAtLeast, 3);
            Assert.That(Passes(c, Ctx(patternCount: 3), Pat()), Is.True);
            Assert.That(Passes(c, Ctx(patternCount: 2), Pat()), Is.False);
        }

        [Test]
        public void MultipleConditions_AllMustPass()
        {
            var conditions = new[]
            {
                new RelicCondition(RelicConditionKind.PatternContainsSymbol, 0, new[] { SlotSymbolType.Seven }),
                new RelicCondition(RelicConditionKind.NoSwapThisBattle),
            };

            // 심볼 일치 + 무스왑 → 통과
            Assert.That(
                RelicConditionEvaluator.Passes(conditions, Ctx(swappedThisBattle: false), Pat(symbol: SlotSymbolType.Seven)),
                Is.True);
            // 심볼 일치 + 스왑함 → 실패
            Assert.That(
                RelicConditionEvaluator.Passes(conditions, Ctx(swappedThisBattle: true), Pat(symbol: SlotSymbolType.Seven)),
                Is.False);
            // 무스왑 + 심볼 불일치 → 실패
            Assert.That(
                RelicConditionEvaluator.Passes(conditions, Ctx(swappedThisBattle: false), Pat(symbol: SlotSymbolType.Bell)),
                Is.False);
        }
    }
}
