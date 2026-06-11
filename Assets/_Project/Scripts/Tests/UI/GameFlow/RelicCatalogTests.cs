using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class RelicCatalogTests
    {
        [Test]
        public void All_Has63Relics()
        {
            Assert.That(RelicCatalog.All.Count, Is.EqualTo(63));
        }

        [Test]
        public void Ids_AreUnique()
        {
            var seen = new HashSet<string>();
            foreach (RelicDefinition relic in RelicCatalog.All)
            {
                Assert.That(seen.Add(relic.Id), Is.True, $"중복 ID: {relic.Id}");
            }
        }

        [Test]
        public void Starters_AreSix_AllStarterAndPhase1()
        {
            Assert.That(RelicCatalog.Starters.Count, Is.EqualTo(6));
            foreach (RelicDefinition relic in RelicCatalog.Starters)
            {
                Assert.That(relic.IsStarter, Is.True);
                Assert.That(relic.Phase1, Is.True);
                Assert.That(relic.Grade, Is.EqualTo(RelicGrade.Starter));
            }
        }

        [Test]
        public void RewardPool_ExcludesStartersAndNonPhase1()
        {
            foreach (RelicDefinition relic in RelicCatalog.RewardPool)
            {
                Assert.That(relic.IsStarter, Is.False, $"{relic.Id}는 시작 유물인데 보상풀에 있음");
                Assert.That(relic.Phase1, Is.True, $"{relic.Id}는 미구현인데 보상풀에 있음");
            }
        }

        [Test]
        public void RewardPool_OnlyCommonOrUncommon_InPhase1()
        {
            // Phase 1: Rare/Legendary/Curse는 전부 미구현이라 보상풀에 없어야 한다.
            foreach (RelicDefinition relic in RelicCatalog.RewardPool)
            {
                Assert.That(relic.Grade, Is.EqualTo(RelicGrade.Common).Or.EqualTo(RelicGrade.Uncommon));
            }
        }

        [Test]
        public void NonPhase1Relic_IsRegisteredButNotInRewardPool()
        {
            RelicDefinition r01 = RelicCatalog.GetById("R-01");
            Assert.That(r01, Is.Not.Null, "카탈로그에는 등록되어 있어야 한다");
            Assert.That(r01.Phase1, Is.False);
            Assert.That(RelicCatalog.RewardPool, Has.None.Matches<RelicDefinition>(x => x.Id == "R-01"));
        }

        [Test]
        public void DiamondStarter_MapsToDiamondSymbol()
        {
            RelicDefinition s05 = RelicCatalog.GetById("S-05");
            Assert.That(s05, Is.Not.Null);
            Assert.That(s05.TriggerSymbol, Is.EqualTo(SlotSymbolType.Diamond));
        }
    }
}
