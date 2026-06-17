using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Data.GameFlow;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.RunGame.ViewModels;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class RelicCatalogTests
    {
        [Test]
        public void All_Has80Relics()
        {
            // v23.0: 시작 6 + 일반 22 + 비일반 20 + 레어 16 + 전설 8 + 저주 8.
            Assert.That(RelicCatalog.All.Count, Is.EqualTo(80));
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
        public void AllRelics_HaveAddressableIconKeys()
        {
            foreach (RelicDefinition relic in RelicCatalog.All)
            {
                Assert.That(relic.IconKey, Is.Not.Null.And.Not.Empty, relic.Id);
            }
        }

        [TestCase(RelicRole.Damage, RelicIconKeys.Slot00)]
        [TestCase(RelicRole.Defense, RelicIconKeys.Slot01)]
        [TestCase(RelicRole.Heal, RelicIconKeys.Slot02)]
        [TestCase(RelicRole.Status, RelicIconKeys.Slot03)]
        [TestCase(RelicRole.Utility, RelicIconKeys.Slot06)]
        public void DefaultIconKey_FollowsRelicRole(
            RelicRole role,
            string expectedIconKey)
        {
            Assert.That(RelicIconKeys.DefaultFor(role), Is.EqualTo(expectedIconKey));
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

        [TestCase("C-07")]
        [TestCase("C-08")]
        [TestCase("U-07")]
        [TestCase("U-08")]
        [TestCase("U-13")]
        [TestCase("U-16")]
        [TestCase("U-17")]
        public void V23StatusRelic_IsExcludedUntilCombatCoreContractMatches(string relicId)
        {
            RelicDefinition relic = RelicCatalog.GetById(relicId);

            Assert.That(relic, Is.Not.Null);
            Assert.That(relic.Phase1, Is.False);
            Assert.That(
                RelicCatalog.RewardPool,
                Has.None.Matches<RelicDefinition>(candidate => candidate.Id == relicId));
        }

        [Test]
        public void DiamondStarter_MapsToDiamondSymbol()
        {
            RelicDefinition s05 = RelicCatalog.GetById("S-05");
            Assert.That(s05, Is.Not.Null);
            Assert.That(s05.TriggerSymbol, Is.EqualTo(SlotSymbolType.Diamond));
        }

        [Test]
        public void Starters_DoNotUseStatusEffects()
        {
            foreach (RelicDefinition relic in RelicCatalog.Starters)
            {
                Assert.That(
                    relic.EffectType,
                    Is.EqualTo(RelicEffectType.AddDamage)
                        .Or.EqualTo(RelicEffectType.AddBlock)
                        .Or.EqualTo(RelicEffectType.Heal),
                    $"{relic.Id} 시작 유물에 상태이상 또는 Phase 2 효과가 섞였습니다.");
            }
        }

        [Test]
        public void SelectStarterRelic_ReplacesPreviousStarter()
        {
            GameFlowSession.StartNewRun();

            Assert.That(GameFlowSession.SelectStarterRelic(RelicCatalog.GetById("S-01")), Is.True);
            Assert.That(GameFlowSession.SelectStarterRelic(RelicCatalog.GetById("S-02")), Is.True);

            Assert.That(GameFlowSession.OwnedRelics.Count, Is.EqualTo(1));
            Assert.That(GameFlowSession.OwnedRelics[0].Id, Is.EqualTo("S-02"));
            Assert.That(GameFlowSession.HasStarterRelic, Is.True);
        }

        [Test]
        public void StartRelicOptions_AreThreeUniqueStarters()
        {
            GameFlowSession.StartNewRun();
            var viewModel = new StartRelicSelectViewModel(new System.Random(1234));
            var ids = new HashSet<string>();
            viewModel.Refresh();

            Assert.That(viewModel.State.Options.Count, Is.EqualTo(3));
            foreach (StartRelicOptionViewState option in viewModel.State.Options)
            {
                RelicDefinition relic = RelicCatalog.GetById(option.Id);
                Assert.That(relic.IsStarter, Is.True);
                Assert.That(option.IconKey, Is.EqualTo(relic.IconKey));
                Assert.That(ids.Add(relic.Id), Is.True, $"중복 시작 유물: {relic.Id}");
            }
        }

        [Test]
        public void RelicReward_CopiesRelicIconKey()
        {
            RelicDefinition relic = RelicCatalog.GetById("C-01");
            var reward = new RunRewardDefinition(relic);

            Assert.That(reward.IconKey, Is.EqualTo(relic.IconKey));
        }

        [Test]
        public void RewardDefinitions_FromSeparateCatalogCalls_AreEqualByRelicId()
        {
            IReadOnlyList<RunRewardDefinition> first =
                RunRewardCatalog.ForTier(EncounterTier.Elite);
            IReadOnlyList<RunRewardDefinition> second =
                RunRewardCatalog.ForTier(EncounterTier.Elite);

            RunRewardDefinition original = first[0];
            RunRewardDefinition recreated = FindRewardByRelicId(second, original.Relic.Id);

            Assert.That(recreated, Is.Not.Null);
            Assert.That(recreated, Is.EqualTo(original));
            Assert.That(new List<RunRewardDefinition> { original }.Contains(recreated), Is.True);
        }

        [Test]
        public void RewardDefinitions_WithDifferentRelicIds_AreNotEqual()
        {
            IReadOnlyList<RunRewardDefinition> rewards =
                RunRewardCatalog.ForTier(EncounterTier.Elite);

            Assert.That(rewards.Count, Is.GreaterThan(1));
            Assert.That(rewards[0], Is.Not.EqualTo(rewards[1]));
        }

        [Test]
        public void SymbolRewards_CompareBySymbolAndAmount()
        {
            var reward = new RunRewardDefinition(
                SlotSymbolType.Cherry,
                2,
                "Cherry",
                "Add cherries");
            var same = new RunRewardDefinition(
                SlotSymbolType.Cherry,
                2,
                "Different display text",
                "Different description");
            var differentAmount = new RunRewardDefinition(
                SlotSymbolType.Cherry,
                3,
                "Cherry",
                "Add cherries");

            Assert.That(same, Is.EqualTo(reward));
            Assert.That(differentAmount, Is.Not.EqualTo(reward));
        }

        [Test]
        public void StatRewards_CompareByRewardType()
        {
            var reward = new RunRewardDefinition(
                RunRewardType.Heal,
                "Heal",
                "Restore health");
            var same = new RunRewardDefinition(
                RunRewardType.Heal,
                "Different display text",
                "Different description");
            var differentType = new RunRewardDefinition(
                RunRewardType.MaxHpUp,
                "Max HP",
                "Increase max health");

            Assert.That(same, Is.EqualTo(reward));
            Assert.That(differentType, Is.Not.EqualTo(reward));
        }

        [Test]
        public void ForTier_Normal_OffersOnlySymbolRewards()
        {
            GameFlowSession.StartNewRun();

            IReadOnlyList<RunRewardDefinition> rewards =
                RunRewardCatalog.ForTier(EncounterTier.Normal);

            Assert.That(rewards.Count, Is.GreaterThan(0));
            foreach (RunRewardDefinition reward in rewards)
            {
                Assert.That(reward.Kind, Is.EqualTo(RunRewardKind.Symbol));
                Assert.That(reward.Amount, Is.EqualTo(1).Or.EqualTo(-1));
            }
        }

        [Test]
        public void ForTier_Normal_OffersAddAndRemoveForEverySymbolAtRunStart()
        {
            // 런 시작 풀(고확률 6 / 저확률 4)은 전 심볼이 바닥(1)보다 크므로
            // 추가 6종 + 제거 6종 = 12개가 전부 제시되어야 한다.
            GameFlowSession.StartNewRun();

            IReadOnlyList<RunRewardDefinition> rewards =
                RunRewardCatalog.ForTier(EncounterTier.Normal);

            Assert.That(rewards.Count, Is.EqualTo(12));
            foreach (SlotSymbolType symbol in SlotSymbolPool.Symbols)
            {
                Assert.That(
                    rewards, Has.Some.Matches<RunRewardDefinition>(
                        reward => reward.Symbol == symbol && reward.Amount == 1),
                    $"{symbol} 추가 보상 누락");
                Assert.That(
                    rewards, Has.Some.Matches<RunRewardDefinition>(
                        reward => reward.Symbol == symbol && reward.Amount == -1),
                    $"{symbol} 제거 보상 누락");
            }
        }

        [Test]
        public void ForTier_Normal_HidesRemoveOptionAtMinimumCount()
        {
            GameFlowSession.StartNewRun();
            GameFlowSession.SlotPool.Add(
                SlotSymbolType.Seven,
                RunRewardCatalog.MinSymbolCountAfterRemove - GameFlowSession.SlotPool.GetCount(SlotSymbolType.Seven));

            IReadOnlyList<RunRewardDefinition> rewards =
                RunRewardCatalog.ForTier(EncounterTier.Normal);

            Assert.That(
                rewards, Has.None.Matches<RunRewardDefinition>(
                    reward => reward.Symbol == SlotSymbolType.Seven && reward.Amount == -1),
                "바닥 개수 심볼에는 제거 보상이 제시되면 안 된다");
            Assert.That(
                rewards, Has.Some.Matches<RunRewardDefinition>(
                    reward => reward.Symbol == SlotSymbolType.Seven && reward.Amount == 1));

            GameFlowSession.StartNewRun(); // 정적 풀 상태 원복
        }

        [TestCase(EncounterTier.Elite)]
        [TestCase(EncounterTier.Boss)]
        public void ForTier_EliteAndBoss_OfferOnlyRelicRewards(EncounterTier tier)
        {
            IReadOnlyList<RunRewardDefinition> rewards = RunRewardCatalog.ForTier(tier);

            Assert.That(rewards.Count, Is.GreaterThan(0));
            foreach (RunRewardDefinition reward in rewards)
            {
                Assert.That(reward.Kind, Is.EqualTo(RunRewardKind.Relic));
            }
        }

        private static RunRewardDefinition FindRewardByRelicId(
            IReadOnlyList<RunRewardDefinition> rewards,
            string relicId)
        {
            for (int index = 0; index < rewards.Count; index++)
            {
                if (rewards[index].Relic?.Id == relicId)
                {
                    return rewards[index];
                }
            }

            return null;
        }
    }
}
