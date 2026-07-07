using NUnit.Framework;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Tests
{
    public sealed class SlotSymbolPoolTests
    {
        [TestCase(SlotSymbolType.Cherry, SlotSymbolPool.DefaultCherryProbabilityPercent)]
        [TestCase(SlotSymbolType.Lemon, SlotSymbolPool.DefaultLemonProbabilityPercent)]
        [TestCase(SlotSymbolType.Clover, SlotSymbolPool.DefaultCloverProbabilityPercent)]
        [TestCase(SlotSymbolType.Bell, SlotSymbolPool.DefaultBellProbabilityPercent)]
        [TestCase(SlotSymbolType.Diamond, SlotSymbolPool.DefaultDiamondProbabilityPercent)]
        [TestCase(SlotSymbolType.Seven, SlotSymbolPool.DefaultSevenProbabilityPercent)]
        public void Reset_Symbols_StartAtDefaultWeight(
            SlotSymbolType symbol,
            int expectedWeight)
        {
            var pool = new SlotSymbolPool();

            // 클로버핏식 가중치(총합≠100)라 가중치 = 퍼센트가 아니다. 가중치 값만 검증한다.
            Assert.That(pool.GetWeight(symbol), Is.EqualTo(expectedWeight));
        }

        [Test]
        public void Reset_RestoresDefaultWeightsAfterChanges()
        {
            var pool = new SlotSymbolPool();
            pool.AddWeight(SlotSymbolType.Cherry, 5);
            pool.AddWeight(SlotSymbolType.Seven, -2);

            pool.Reset();

            Assert.That(
                pool.GetWeight(SlotSymbolType.Cherry),
                Is.EqualTo(SlotSymbolPool.DefaultWeightFor(SlotSymbolType.Cherry)));
            Assert.That(
                pool.GetWeight(SlotSymbolType.Seven),
                Is.EqualTo(SlotSymbolPool.DefaultWeightFor(SlotSymbolType.Seven)));
        }

        [Test]
        public void DefaultDamage_IsInverseOfDefaultProbabilityCurve()
        {
            Assert.That(
                SlotSymbolAttackValues.DefaultCherryDamage,
                Is.EqualTo(SlotSymbolAttackValues.DefaultLemonDamage));
            Assert.That(
                SlotSymbolAttackValues.DefaultCherryDamage,
                Is.LessThan(SlotSymbolAttackValues.DefaultCloverDamage));
            Assert.That(
                SlotSymbolAttackValues.DefaultLemonDamage,
                Is.LessThan(SlotSymbolAttackValues.DefaultBellDamage));
            Assert.That(
                SlotSymbolAttackValues.DefaultCloverDamage,
                Is.LessThan(SlotSymbolAttackValues.DefaultDiamondDamage));
            Assert.That(
                SlotSymbolAttackValues.DefaultDiamondDamage,
                Is.LessThan(SlotSymbolAttackValues.DefaultSevenDamage));
        }

        [Test]
        public void ResetUniform_SetsSameWeightForAllSymbols()
        {
            var pool = new SlotSymbolPool(4);

            foreach (SlotSymbolType symbol in SlotSymbolPool.Symbols)
            {
                Assert.That(pool.GetWeight(symbol), Is.EqualTo(4));
            }
        }

        [Test]
        public void Add_NegativeAmount_ClampsAtZero()
        {
            var pool = new SlotSymbolPool();

            pool.AddWeight(SlotSymbolType.Lemon, -100);

            Assert.That(pool.GetWeight(SlotSymbolType.Lemon), Is.EqualTo(0));
        }

        [Test]
        public void ProbabilityOf_UsesSymbolWeightOverTotalWeight()
        {
            var pool = new SlotSymbolPool(0);
            pool.SetWeight(SlotSymbolType.Cherry, 3);
            pool.SetWeight(SlotSymbolType.Seven, 1);

            Assert.That(pool.ProbabilityOf(SlotSymbolType.Cherry), Is.EqualTo(0.75d));
            Assert.That(pool.ProbabilityOf(SlotSymbolType.Seven), Is.EqualTo(0.25d));
        }
    }
}
