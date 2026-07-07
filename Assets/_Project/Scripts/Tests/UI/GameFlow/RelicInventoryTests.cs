using NUnit.Framework;
using SlotRogue.Relics.Pool;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Tests.GameFlow
{
    /// <summary>
    /// 유물 보유 로직(RelicInventory)을 전역 상태 없이 격리 검증합니다.
    /// </summary>
    public sealed class RelicInventoryTests
    {
        private static RelicDefinition AnyRewardRelic() => RelicCatalog.RewardPool[0];

        [Test]
        public void Add_AppendsRelic()
        {
            var inventory = new RelicInventory();

            inventory.Add(AnyRewardRelic());

            Assert.That(inventory.Owned.Count, Is.EqualTo(1));
        }

        [Test]
        public void Add_IgnoresNull()
        {
            var inventory = new RelicInventory();

            inventory.Add(null);

            Assert.That(inventory.Owned.Count, Is.EqualTo(0));
        }

        [Test]
        public void Add_AllowsDuplicates()
        {
            var inventory = new RelicInventory();
            RelicDefinition relic = AnyRewardRelic();

            inventory.Add(relic);
            inventory.Add(relic);

            Assert.That(inventory.Owned.Count, Is.EqualTo(2));
        }

        [Test]
        public void SelectStarter_RejectsNonStarter()
        {
            var inventory = new RelicInventory();

            bool selected = inventory.SelectStarter(AnyRewardRelic());

            Assert.That(selected, Is.False);
            Assert.That(inventory.Owned.Count, Is.EqualTo(0));
        }

        [Test]
        public void Clear_RemovesAll()
        {
            var inventory = new RelicInventory();
            inventory.Add(AnyRewardRelic());

            inventory.Clear();

            Assert.That(inventory.Owned.Count, Is.EqualTo(0));
            Assert.That(inventory.HasStarter, Is.False);
        }

        [Test]
        public void TickWaveLifetimes_RemovesConsumableAfterItsWaves()
        {
            var inventory = new RelicInventory();
            inventory.Add(RelicCatalog.GetById("R-39")); // 소멸·2웨이브
            Assert.That(inventory.Owned.Count, Is.EqualTo(1));

            inventory.TickWaveLifetimes(); // 2 -> 1
            Assert.That(inventory.Owned.Count, Is.EqualTo(1));

            inventory.TickWaveLifetimes(); // 1 -> 0 → 제거
            Assert.That(inventory.Owned.Count, Is.EqualTo(0));
        }

        [Test]
        public void TickWaveLifetimes_KeepsPermanentRelics()
        {
            var inventory = new RelicInventory();
            inventory.Add(RelicCatalog.GetById("R-16")); // 상시

            inventory.TickWaveLifetimes();
            inventory.TickWaveLifetimes();

            Assert.That(inventory.Owned.Count, Is.EqualTo(1));
        }
    }
}
