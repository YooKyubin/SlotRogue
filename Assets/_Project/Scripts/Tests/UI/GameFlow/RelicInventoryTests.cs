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
        private static RelicDefinition AnyStarter() => RelicCatalog.Starters[0];

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
        public void SelectStarter_KeepsAtMostOneStarter()
        {
            var inventory = new RelicInventory();
            RelicDefinition first = RelicCatalog.Starters[0];
            RelicDefinition second = RelicCatalog.Starters.Count > 1
                ? RelicCatalog.Starters[1]
                : RelicCatalog.Starters[0];

            inventory.SelectStarter(first);
            inventory.SelectStarter(second);

            int starterCount = 0;
            foreach (RelicDefinition relic in inventory.Owned)
            {
                if (relic.IsStarter)
                {
                    starterCount++;
                }
            }

            Assert.That(starterCount, Is.EqualTo(1));
            Assert.That(inventory.HasStarter, Is.True);
        }

        [Test]
        public void SelectStarter_InsertsAtFront()
        {
            var inventory = new RelicInventory();
            inventory.Add(AnyRewardRelic());

            inventory.SelectStarter(AnyStarter());

            Assert.That(inventory.Owned[0].IsStarter, Is.True);
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
    }
}
