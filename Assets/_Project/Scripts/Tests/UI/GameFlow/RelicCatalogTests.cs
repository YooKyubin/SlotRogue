using System.Collections.Generic;
using System.Linq;
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
        [SetUp]
        public void SetUp()
        {
            SlotSymbolAttackValues.ResetToDefaults();
            ConfigureDefaultInitialSymbolWeights();
        }

        [TearDown]
        public void TearDown()
        {
            SlotSymbolAttackValues.ResetToDefaults();
            ConfigureDefaultInitialSymbolWeights();
        }

        [Test]
        public void All_Has55Relics()
        {
            // v29 41종 + 엔진 확장(클로버핏 착안, 순수 조합) 14종 = 55.
            Assert.That(RelicCatalog.All.Count, Is.EqualTo(55));
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
        public void RewardPool_ExcludesStartersAndNonPhase1()
        {
            foreach (RelicDefinition relic in RelicCatalog.RewardPool)
            {
                Assert.That(relic.IsStarter, Is.False, $"{relic.Id}는 시작 유물인데 보상풀에 있음");
                Assert.That(relic.Phase1, Is.True, $"{relic.Id}는 미구현인데 보상풀에 있음");
            }
        }

        [Test]
        public void AddSpinCoins_AddsBaseSpinReward()
        {
            GameFlowSession.StartNewRun();

            int reward = GameFlowSession.AddSpinCoins();

            Assert.That(reward, Is.EqualTo(GameFlowSession.BaseSpinCoinReward));
            Assert.That(GameFlowSession.RunCoins, Is.EqualTo(GameFlowSession.BaseSpinCoinReward));
        }

        [Test]
        public void TmpSpriteIndexes_FollowSlotSymbolOrder()
        {
            Assert.That(
                SlotSymbolIconKeys.TmpSpriteIndexFor(SlotSymbolType.Cherry),
                Is.EqualTo(0));
            Assert.That(
                SlotSymbolIconKeys.TmpSpriteIndexFor(SlotSymbolType.Lemon),
                Is.EqualTo(5));
        }

        [Test]
        public void IconSheetAddresses_FollowAddressableNames()
        {
            Assert.That(RelicIconKeys.SheetAddress, Is.EqualTo("Relic Sheet Highlight"));
            Assert.That(SlotSymbolIconKeys.NormalSheetAddress, Is.EqualTo("Symbol Sheet Normal"));
            Assert.That(SlotSymbolIconKeys.AnimationSheetAddress, Is.EqualTo("Symbol Sheet Animation"));
            Assert.That(SlotSymbolIconKeys.HighlightSheetAddress, Is.EqualTo("Symbol Sheet Highlight"));
            Assert.That(SlotSymbolIconKeys.TmpSpriteAssetAddress, Is.EqualTo("Symbols-Sheet-TMP"));
        }

        [Test]
        public void RewardDefinitions_FromSeparateCatalogCalls_AreEqualByRelicId()
        {
            RelicDefinition relic = RelicCatalog.GetById("R-01");
            var original = new RunRewardDefinition(relic);
            var recreated = new RunRewardDefinition(RelicCatalog.GetById("R-01"));

            Assert.That(recreated, Is.EqualTo(original));
            Assert.That(new List<RunRewardDefinition> { original }.Contains(recreated), Is.True);
        }

        [Test]
        public void RewardDefinitions_WithDifferentRelicIds_AreNotEqual()
        {
            var first = new RunRewardDefinition(RelicCatalog.GetById("R-01"));
            var second = new RunRewardDefinition(RelicCatalog.GetById("R-02"));

            Assert.That(first, Is.Not.EqualTo(second));
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
        public void ForTier_Normal_OffersProposalsOnly()
        {
            GameFlowSession.StartNewRun();

            IReadOnlyList<RunRewardDefinition> rewards =
                RunRewardCatalog.ForTier(EncounterTier.Normal);

            Assert.That(rewards.Count, Is.GreaterThan(0));
            Assert.That(rewards, Has.All.Matches<RunRewardDefinition>(
                reward => reward.Kind == RunRewardKind.Proposal));
            Assert.That(rewards, Has.Some.Matches<RunRewardDefinition>(
                reward => reward.ProposalEffect == RunProposalEffectKind.SymbolWeight));
            Assert.That(rewards, Has.None.Matches<RunRewardDefinition>(
                reward => reward.Kind == RunRewardKind.Relic));
        }

        [Test]
        public void RollOptions_ReturnsAvailableProposalsOnly()
        {
            GameFlowSession.StartNewRun();
            var service = new RunRewardService(new System.Random(1234));

            IReadOnlyList<RunRewardDefinition> options = service.RollOptions(3);

            Assert.That(options.Count, Is.EqualTo(3));
            Assert.That(options, Has.All.Matches<RunRewardDefinition>(
                reward => reward.Kind == RunRewardKind.Proposal));
            Assert.That(options, Has.All.Matches<RunRewardDefinition>(
                reward => reward.ProposalEffect != RunProposalEffectKind.None));
            Assert.That(
                options.Select(reward => reward.ProposalId).Distinct().Count(),
                Is.EqualTo(options.Count));
        }

        [Test]
        public void ApplyReward_RunCoins_AddsShopCoinsAndClaimsReward()
        {
            GameFlowSession.StartNewRun();
            int beforeRewards = GameFlowSession.RewardsClaimed;

            GameFlowSession.ApplyReward(RunRewardType.RunCoins);

            Assert.That(
                GameFlowSession.RunCoins,
                Is.EqualTo(RewardEconomy.RunCoinRewardAmount));
            Assert.That(GameFlowSession.RewardsClaimed, Is.EqualTo(beforeRewards + 1));
        }

        [Test]
        public void ForTier_Normal_IncludesImplementedSymbolProposalCoverage()
        {
            GameFlowSession.StartNewRun();

            IReadOnlyList<RunRewardDefinition> rewards =
                RunRewardCatalog.ForTier(EncounterTier.Normal);

            foreach (SlotSymbolType symbol in SlotSymbolPool.Symbols)
            {
                Assert.That(
                    rewards, Has.Some.Matches<RunRewardDefinition>(
                        reward => reward.ProposalEffect == RunProposalEffectKind.SymbolWeight &&
                            reward.Symbols.Contains(symbol)),
                    $"{symbol} 가중치 제안 누락");
                Assert.That(
                    rewards, Has.Some.Matches<RunRewardDefinition>(
                        reward => reward.ProposalEffect == RunProposalEffectKind.SymbolBaseDamage &&
                            reward.Symbols.Contains(symbol)),
                    $"{symbol} 기본 공격력 제안 누락");
            }
        }

        [Test]
        public void RollOptions_FiltersUnimplementedProposals()
        {
            GameFlowSession.StartNewRun();
            var service = new RunRewardService(new System.Random(42));

            IReadOnlyList<RunRewardDefinition> options = service.RollOptions(12);

            Assert.That(options, Has.All.Matches<RunRewardDefinition>(
                reward => reward.Kind == RunRewardKind.Proposal));
            Assert.That(options, Has.All.Matches<RunRewardDefinition>(
                reward => reward.ProposalEffect != RunProposalEffectKind.None));
        }

        [TestCase(EncounterTier.Elite)]
        [TestCase(EncounterTier.Boss)]
        public void ForTier_EliteAndBoss_OfferProposalsWithoutRelics(EncounterTier tier)
        {
            IReadOnlyList<RunRewardDefinition> rewards = RunRewardCatalog.ForTier(tier);

            Assert.That(rewards.Count, Is.GreaterThan(0));
            Assert.That(rewards, Has.All.Matches<RunRewardDefinition>(
                reward => reward.Kind == RunRewardKind.Proposal));
            Assert.That(rewards, Has.Some.Matches<RunRewardDefinition>(
                reward => reward.ProposalEffect == RunProposalEffectKind.SymbolWeight));
            Assert.That(rewards, Has.None.Matches<RunRewardDefinition>(
                reward => reward.Kind == RunRewardKind.Relic));
        }

        [Test]
        public void RunDescription_Open_ShowsCurrentSymbolWeights()
        {
            GameFlowSession.StartNewRun();
            var viewModel = new RunInventoryViewModel();

            viewModel.OpenDescription();

            Assert.That(viewModel.State.CurrentValue.IsOpen, Is.True);
            Assert.That(viewModel.State.CurrentValue.ActiveTab, Is.EqualTo(RunInventoryTab.SymbolProbability));
            Assert.That(viewModel.State.CurrentValue.Symbols.Count, Is.EqualTo(SlotSymbolPool.Symbols.Count));
            Assert.That(
                viewModel.State.CurrentValue.Symbols.Select(symbol => symbol.Symbol),
                Is.EqualTo(new[]
                {
                    SlotSymbolType.Cherry,
                    SlotSymbolType.Lemon,
                    SlotSymbolType.Clover,
                    SlotSymbolType.Bell,
                    SlotSymbolType.Diamond,
                    SlotSymbolType.Seven,
                }));
            AssertInventorySymbolWeight(
                viewModel.State.CurrentValue,
                SlotSymbolType.Cherry,
                SlotSymbolPool.DefaultWeightFor(SlotSymbolType.Cherry));
            AssertInventorySymbolWeight(
                viewModel.State.CurrentValue,
                SlotSymbolType.Seven,
                SlotSymbolPool.DefaultWeightFor(SlotSymbolType.Seven));
        }

        [Test]
        public void RunInventory_Refresh_ReflectsSymbolReward()
        {
            GameFlowSession.StartNewRun();
            var viewModel = new RunInventoryViewModel();
            GameFlowSession.ApplySymbolReward(SlotSymbolType.Lemon, 2);

            viewModel.OpenDescription();

            AssertInventorySymbolWeight(
                viewModel.State.CurrentValue,
                SlotSymbolType.Lemon,
                SlotSymbolPool.DefaultWeightFor(SlotSymbolType.Lemon) + 2);
            Assert.That(
                viewModel.State.CurrentValue.Summary,
                Does.Contain($"확률 합계 {DefaultInitialSymbolWeightTotal() + 2}%"));
        }

        [Test]
        public void RunHUD_Refresh_ShowsSymbolProbabilitiesInFixedOrder()
        {
            GameFlowSession.ConfigureInitialSymbolWeights(
                cherry: 4,
                lemon: 2,
                clover: 3,
                bell: 1,
                diamond: 5,
                seven: 5);
            GameFlowSession.StartNewRun();
            var viewModel = new RunHUDViewModel();

            viewModel.Refresh();

            string text = viewModel.State.CurrentValue.SymbolProbabilityText;
            Assert.That(text, Does.Contain("체리 20%  레몬 10%  클로버 15%"));
            Assert.That(text, Does.Contain("종 5%  다이아 25%  7 25%"));
            Assert.That(text.IndexOf("체리", System.StringComparison.Ordinal), Is.LessThan(
                text.IndexOf("레몬", System.StringComparison.Ordinal)));
            Assert.That(text.IndexOf("레몬", System.StringComparison.Ordinal), Is.LessThan(
                text.IndexOf("클로버", System.StringComparison.Ordinal)));
            Assert.That(text.IndexOf("클로버", System.StringComparison.Ordinal), Is.LessThan(
                text.IndexOf("종", System.StringComparison.Ordinal)));
            Assert.That(text.IndexOf("종", System.StringComparison.Ordinal), Is.LessThan(
                text.IndexOf("다이아", System.StringComparison.Ordinal)));
            Assert.That(text.IndexOf("다이아", System.StringComparison.Ordinal), Is.LessThan(
                text.IndexOf("7", System.StringComparison.Ordinal)));
        }

        [Test]
        public void RunInventory_Open_ShowsAllOwnedRelics()
        {
            GameFlowSession.StartNewRun();
            var viewModel = new RunInventoryViewModel();
            GameFlowSession.AddRelic(RelicCatalog.GetById("R-09"));
            GameFlowSession.AddRelic(RelicCatalog.GetById("R-01"));

            viewModel.Open();

            Assert.That(viewModel.State.CurrentValue.IsRelicInventoryOpen, Is.True);
            Assert.That(viewModel.State.CurrentValue.IsDescriptionOpen, Is.False);
            Assert.That(viewModel.State.CurrentValue.Relics.Count, Is.EqualTo(2));
            Assert.That(viewModel.State.CurrentValue.Relics[0].Id, Is.EqualTo("R-09"));
            Assert.That(viewModel.State.CurrentValue.Relics[1].Id, Is.EqualTo("R-01"));
            Assert.That(viewModel.State.CurrentValue.Relics[0].Description, Does.Contain("체리"));
        }

        [Test]
        public void RunInventory_Close_HidesPanelButKeepsSnapshot()
        {
            GameFlowSession.StartNewRun();
            var viewModel = new RunInventoryViewModel();
            viewModel.Open();

            viewModel.Close();

            Assert.That(viewModel.State.CurrentValue.IsOpen, Is.False);
            Assert.That(viewModel.State.CurrentValue.Symbols.Count, Is.EqualTo(SlotSymbolPool.Symbols.Count));
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

        private static void AssertInventorySymbolWeight(
            RunInventoryViewState state,
            SlotSymbolType symbol,
            int expectedWeight)
        {
            foreach (RunInventorySymbolViewState item in state.Symbols)
            {
                if (item.Symbol == symbol)
                {
                    Assert.That(item.Weight, Is.EqualTo(expectedWeight));
                    return;
                }
            }

            Assert.Fail($"심볼이 인벤토리에 없습니다: {symbol}");
        }

        private static int DefaultInitialSymbolWeightTotal()
        {
            int total = 0;
            foreach (SlotSymbolType symbol in SlotSymbolPool.Symbols)
            {
                total += SlotSymbolPool.DefaultWeightFor(symbol);
            }

            return total;
        }

        private static void ConfigureDefaultInitialSymbolWeights()
        {
            GameFlowSession.ConfigureInitialSymbolWeights(
                SlotSymbolPool.DefaultWeightFor(SlotSymbolType.Cherry),
                SlotSymbolPool.DefaultWeightFor(SlotSymbolType.Lemon),
                SlotSymbolPool.DefaultWeightFor(SlotSymbolType.Clover),
                SlotSymbolPool.DefaultWeightFor(SlotSymbolType.Bell),
                SlotSymbolPool.DefaultWeightFor(SlotSymbolType.Diamond),
                SlotSymbolPool.DefaultWeightFor(SlotSymbolType.Seven));
        }
    }
}
