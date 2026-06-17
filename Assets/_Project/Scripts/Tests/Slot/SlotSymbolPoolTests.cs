using NUnit.Framework;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Tests
{
    public sealed class SlotSymbolPoolTests
    {
        [TestCase(SlotSymbolType.Cherry)]
        [TestCase(SlotSymbolType.Clover)]
        [TestCase(SlotSymbolType.Bell)]
        public void Reset_HighProbabilitySymbols_StartAtHighCount(SlotSymbolType symbol)
        {
            var pool = new SlotSymbolPool();

            Assert.That(SlotSymbolPool.IsHighProbability(symbol), Is.True);
            Assert.That(
                pool.GetCount(symbol),
                Is.EqualTo(SlotSymbolPool.DefaultHighProbabilityCount));
        }

        [TestCase(SlotSymbolType.Lemon)]
        [TestCase(SlotSymbolType.Diamond)]
        [TestCase(SlotSymbolType.Seven)]
        public void Reset_LowProbabilitySymbols_StartAtLowCount(SlotSymbolType symbol)
        {
            var pool = new SlotSymbolPool();

            Assert.That(SlotSymbolPool.IsHighProbability(symbol), Is.False);
            Assert.That(
                pool.GetCount(symbol),
                Is.EqualTo(SlotSymbolPool.DefaultLowProbabilityCount));
        }

        [Test]
        public void Reset_RestoresDefaultCountsAfterChanges()
        {
            var pool = new SlotSymbolPool();
            pool.Add(SlotSymbolType.Cherry, 5);
            pool.Add(SlotSymbolType.Seven, -2);

            pool.Reset();

            Assert.That(
                pool.GetCount(SlotSymbolType.Cherry),
                Is.EqualTo(SlotSymbolPool.DefaultHighProbabilityCount));
            Assert.That(
                pool.GetCount(SlotSymbolType.Seven),
                Is.EqualTo(SlotSymbolPool.DefaultLowProbabilityCount));
        }

        [Test]
        public void ResetUniform_SetsSameCountForAllSymbols()
        {
            var pool = new SlotSymbolPool(4);

            foreach (SlotSymbolType symbol in SlotSymbolPool.Symbols)
            {
                Assert.That(pool.GetCount(symbol), Is.EqualTo(4));
            }
        }

        [Test]
        public void Add_NegativeAmount_ClampsAtZero()
        {
            var pool = new SlotSymbolPool();

            pool.Add(SlotSymbolType.Lemon, -100);

            Assert.That(pool.GetCount(SlotSymbolType.Lemon), Is.EqualTo(0));
        }
    }
}
