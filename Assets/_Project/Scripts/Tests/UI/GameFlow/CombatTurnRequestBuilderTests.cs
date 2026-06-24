using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class CombatTurnRequestBuilderTests
    {
        private CombatTurnRequestBuilder _builder = null!;

        [SetUp]
        public void SetUp()
        {
            _builder = new CombatTurnRequestBuilder();
        }

        [Test]
        public void Build_BlankRequest_AddsBaseAttack()
        {
            RunCombatRequestResult result = _builder.Build(
                SlotCombatRequest.Empty,
                relicResult: null,
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.FinalRequest.Damage, Is.EqualTo(SlotCombatRequest.BaseAttackDamage));
            Assert.That(result.FinalRequest.AttackCount, Is.EqualTo(SlotCombatRequest.BaseAttackCount));
            Assert.That(result.FinalRequest.PatternName, Is.EqualTo(SlotCombatRequest.BaseAttackName));
        }

        [Test]
        public void Build_MatchingStarterRelic_AppliesRelicBonus()
        {
            var request = new SlotCombatRequest(18, 0, 1, 0, false, "Cherry x3");
            var runner = new RelicEffectRunner();
            RelicResolveResult relicResult = runner.Resolve(
                Matches(SlotSymbolType.Cherry, 3),
                new[] { RelicCatalog.GetById("S-01") },
                FullHp);

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult,
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.RelicActivationSummary, Does.Contain("체리 단검"));
            Assert.That(result.FinalRequest.Damage, Is.EqualTo(21));
        }

        [Test]
        public void Build_NonMatchingStarterRelic_DoesNotApplyRelicBonus()
        {
            var request = new SlotCombatRequest(15, 0, 1, 0, false, "Clover x3");
            var runner = new RelicEffectRunner();
            RelicResolveResult relicResult = runner.Resolve(
                Matches(SlotSymbolType.Clover, 3),
                new[] { RelicCatalog.GetById("S-01") },
                FullHp);

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult,
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.RelicActivationSummary, Is.Empty);
            Assert.That(result.FinalRequest.Damage, Is.EqualTo(15));
        }

        [Test]
        public void Build_RelicSymbolInAnyPattern_Activates()
        {
            var request = new SlotCombatRequest(15, 0, 1, 0, false, "Mixed");
            var matches = new List<SlotPatternMatch>
            {
                Single(SlotSymbolType.Clover, 5),
                Single(SlotSymbolType.Cherry, 3),
            };
            var runner = new RelicEffectRunner();
            RelicResolveResult relicResult = runner.Resolve(
                matches,
                new[] { RelicCatalog.GetById("S-01") },
                FullHp);

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult,
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.RelicActivationSummary, Does.Contain("체리 단검"));
            Assert.That(result.FinalRequest.Damage, Is.EqualTo(18));
        }

        [Test]
        public void Build_RunBonuses_AdjustFinalRequest()
        {
            var request = new SlotCombatRequest(6, 0, 1, 12, false, "Diamond x3");
            var relicResult = new RelicResolveResult(
                additionalDamage: 0,
                additionalBlock: 5,
                healAmount: 0,
                statusEffectsToApply: null,
                activationSummary: "다이아 갑옷");

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult,
                runDamageBonus: 2,
                runDefenseBonus: 2);

            Assert.That(result.FinalRequest.Damage, Is.EqualTo(8));
            Assert.That(result.FinalRequest.Defense, Is.EqualTo(7));
            Assert.That(result.FinalRequest.HealAmount, Is.EqualTo(12));
            Assert.That(result.RunBonusSummary, Is.EqualTo("피해 +2, 방어 +2"));
        }

        [Test]
        public void Build_MultipleBurnRequests_AreCombined()
        {
            var request = new SlotCombatRequest(6, 0, 1, 0, false, "Lemon x3");
            var relicResult = new RelicResolveResult(
                additionalDamage: 0,
                additionalBlock: 0,
                healAmount: 0,
                statusEffectsToApply: new[]
                {
                    new StatusEffectRequest(
                        StatusEffectKind.Burn,
                        1,
                        CombatTargetMode.SelectedEnemy),
                    new StatusEffectRequest(
                        StatusEffectKind.Burn,
                        2,
                        CombatTargetMode.SelectedEnemy),
                },
                activationSummary: "붉은 성냥, 붉은 점화탄");

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult,
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.StatusEffectsToApply.Count, Is.EqualTo(1));
            TargetedStatusEffectSpec targetedSpec = result.StatusEffectsToApply[0];
            Assert.That(targetedSpec.Spec.Kind, Is.EqualTo(StatusEffectKind.Burn));
            Assert.That(targetedSpec.Spec.Duration, Is.EqualTo(1));
            Assert.That(targetedSpec.Spec.Magnitude, Is.EqualTo(3));
            Assert.That(targetedSpec.Spec.StackMode, Is.EqualTo(StatusStackMode.Refresh));
            Assert.That(targetedSpec.TargetMode, Is.EqualTo(CombatTargetMode.SelectedEnemy));
        }

        [Test]
        public void Build_MultipleInfectionRequests_AreCombinedAsStackCount()
        {
            var request = new SlotCombatRequest(6, 0, 1, 0, false, "Blue x4");
            var relicResult = new RelicResolveResult(
                additionalDamage: 0,
                additionalBlock: 0,
                healAmount: 0,
                statusEffectsToApply: new[]
                {
                    new StatusEffectRequest(
                        StatusEffectKind.Infection,
                        3,
                        CombatTargetMode.SelectedEnemy),
                    new StatusEffectRequest(
                        StatusEffectKind.Infection,
                        4,
                        CombatTargetMode.SelectedEnemy),
                },
                activationSummary: "푸른 감염가루, 푸른 배양액");

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult,
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.StatusEffectsToApply.Count, Is.EqualTo(1));
            TargetedStatusEffectSpec targetedSpec = result.StatusEffectsToApply[0];
            Assert.That(targetedSpec.Spec.Kind, Is.EqualTo(StatusEffectKind.Infection));
            Assert.That(targetedSpec.Spec.Duration, Is.EqualTo(0));
            Assert.That(targetedSpec.Spec.Magnitude, Is.EqualTo(7));
            Assert.That(targetedSpec.Spec.StackMode, Is.EqualTo(StatusStackMode.Stack));
            Assert.That(targetedSpec.TargetMode, Is.EqualTo(CombatTargetMode.SelectedEnemy));
        }

        [Test]
        public void Build_ThornsRequest_MapsAmountToMagnitude()
        {
            var request = new SlotCombatRequest(0, 5, 1, 0, false, "Guard");
            var relicResult = new RelicResolveResult(
                additionalDamage: 0,
                additionalBlock: 0,
                healAmount: 0,
                statusEffectsToApply: new[]
                {
                    new StatusEffectRequest(
                        StatusEffectKind.Thorns,
                        4,
                        CombatTargetMode.Self),
                },
                activationSummary: "가시 갑옷");

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult,
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.StatusEffectsToApply.Count, Is.EqualTo(1));
            TargetedStatusEffectSpec targetedSpec = result.StatusEffectsToApply[0];
            Assert.That(targetedSpec.Spec.Kind, Is.EqualTo(StatusEffectKind.Thorns));
            Assert.That(targetedSpec.Spec.Magnitude, Is.EqualTo(4));
            Assert.That(targetedSpec.Spec.StackMode, Is.EqualTo(StatusStackMode.Refresh));
            Assert.That(targetedSpec.TargetMode, Is.EqualTo(CombatTargetMode.Self));
        }

        [Test]
        public void Build_SameStatusWithDifferentTargets_DoesNotCombine()
        {
            var request = new SlotCombatRequest(6, 0, 1, 0, false, "Mixed");
            var relicResult = new RelicResolveResult(
                additionalDamage: 0,
                additionalBlock: 0,
                healAmount: 0,
                statusEffectsToApply: new[]
                {
                    new StatusEffectRequest(
                        StatusEffectKind.Infection,
                        2,
                        CombatTargetMode.SelectedEnemy),
                    new StatusEffectRequest(
                        StatusEffectKind.Infection,
                        3,
                        CombatTargetMode.Self),
                },
                activationSummary: "Mixed");

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult,
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.StatusEffectsToApply.Count, Is.EqualTo(2));
            Assert.That(result.StatusEffectsToApply[0].Spec.Magnitude, Is.EqualTo(2));
            Assert.That(
                result.StatusEffectsToApply[0].TargetMode,
                Is.EqualTo(CombatTargetMode.SelectedEnemy));
            Assert.That(result.StatusEffectsToApply[1].Spec.Magnitude, Is.EqualTo(3));
            Assert.That(
                result.StatusEffectsToApply[1].TargetMode,
                Is.EqualTo(CombatTargetMode.Self));
        }

        [Test]
        public void Build_SelfLifestealRequest_PreservesTargetAndUsageCount()
        {
            var request = new SlotCombatRequest(6, 0, 1, 0, false, "Attack");
            var relicResult = new RelicResolveResult(
                additionalDamage: 0,
                additionalBlock: 0,
                healAmount: 0,
                statusEffectsToApply: new[]
                {
                    new StatusEffectRequest(
                        StatusEffectKind.Lifesteal,
                        2,
                        CombatTargetMode.Self),
                },
                activationSummary: "Lifesteal");

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult,
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.StatusEffectsToApply.Count, Is.EqualTo(1));
            Assert.That(
                result.StatusEffectsToApply[0].Spec.Kind,
                Is.EqualTo(StatusEffectKind.Lifesteal));
            Assert.That(result.StatusEffectsToApply[0].Spec.Magnitude, Is.EqualTo(2));
            Assert.That(
                result.StatusEffectsToApply[0].TargetMode,
                Is.EqualTo(CombatTargetMode.Self));
        }

        [Test]
        public void Build_MultiHitRequest_CalculatesTotalAttackPower()
        {
            var request = new SlotCombatRequest(6, 0, 3, 0, false, "Multi Hit");

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult: null,
                runDamageBonus: 2,
                runDefenseBonus: 0);

            Assert.That(result.FinalRequest.Damage, Is.EqualTo(8));
            Assert.That(result.FinalRequest.AttackCount, Is.EqualTo(3));
            Assert.That(result.AttackPower, Is.EqualTo(24));
        }

        [Test]
        public void Build_Lifesteal_HealsPercentOfOutgoingDamage()
        {
            var request = new SlotCombatRequest(30, 0, 1, 0, false, "Lemon x3");
            var relicResult = new RelicResolveResult(
                additionalDamage: 0,
                additionalBlock: 0,
                healAmount: 0,
                statusEffectsToApply: null,
                activationSummary: "레몬 흡혈액",
                derivedHeals: new[]
                {
                    new RelicDerivedHeal("C-16", "레몬 흡혈액", RelicDerivedHealKind.Lifesteal, 12, 6),
                });

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult,
                runDamageBonus: 0,
                runDefenseBonus: 0);

            // 12% of 30 = 3.6 -> 3 (cap 6 not reached).
            Assert.That(result.FinalRequest.HealAmount, Is.EqualTo(3));
            Assert.That(result.DerivedHealContributions.Count, Is.EqualTo(1));
            Assert.That(result.DerivedHealContributions[0].RelicId, Is.EqualTo("C-16"));
            Assert.That(result.DerivedHealContributions[0].Heal, Is.EqualTo(3));
        }

        [Test]
        public void Build_Lifesteal_AppliesTurnCap()
        {
            var request = new SlotCombatRequest(100, 0, 1, 0, false, "Lemon x3");
            var relicResult = new RelicResolveResult(
                additionalDamage: 0,
                additionalBlock: 0,
                healAmount: 0,
                statusEffectsToApply: null,
                activationSummary: "레몬 흡혈액",
                derivedHeals: new[]
                {
                    new RelicDerivedHeal("C-16", "레몬 흡혈액", RelicDerivedHealKind.Lifesteal, 12, 6),
                });

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult,
                runDamageBonus: 0,
                runDefenseBonus: 0);

            // 12% of 100 = 12, capped to 6.
            Assert.That(result.FinalRequest.HealAmount, Is.EqualTo(6));
        }

        [Test]
        public void Build_Lifesteal_NoDamage_NoHeal()
        {
            // 방어만 있는 턴 — 피해 0이라 흡혈 회복도 0.
            var request = new SlotCombatRequest(0, 20, 1, 0, false, "Diamond x3");
            var relicResult = new RelicResolveResult(
                additionalDamage: 0,
                additionalBlock: 0,
                healAmount: 0,
                statusEffectsToApply: null,
                activationSummary: "흡혈 왕관",
                derivedHeals: new[]
                {
                    new RelicDerivedHeal("L-05", "흡혈 왕관", RelicDerivedHealKind.Lifesteal, 10, 10),
                });

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult,
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.FinalRequest.HealAmount, Is.EqualTo(0));
            Assert.That(result.DerivedHealContributions, Is.Empty);
        }

        [Test]
        public void Build_BlockToHeal_HealsPercentOfGainedDefense()
        {
            var request = new SlotCombatRequest(0, 0, 1, 0, false, "Diamond x3");
            var relicResult = new RelicResolveResult(
                additionalDamage: 0,
                additionalBlock: 20,
                healAmount: 0,
                statusEffectsToApply: null,
                activationSummary: "수호 천칭",
                derivedHeals: new[]
                {
                    new RelicDerivedHeal("L-04", "수호 천칭", RelicDerivedHealKind.BlockToHeal, 25, 0),
                });

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult,
                runDamageBonus: 0,
                runDefenseBonus: 0);

            // 획득 방어도 20의 25% = 5.
            Assert.That(result.FinalRequest.Defense, Is.EqualTo(20));
            Assert.That(result.FinalRequest.HealAmount, Is.EqualTo(5));
            Assert.That(result.DerivedHealContributions[0].Heal, Is.EqualTo(5));
        }

        private static readonly RelicBattleContext FullHp = new(30, 30, 40, 40, false);

        private static IReadOnlyList<SlotPatternMatch> Matches(SlotSymbolType symbol, int cellCount)
        {
            return new[] { Single(symbol, cellCount) };
        }

        private static SlotPatternMatch Single(SlotSymbolType symbol, int cellCount)
        {
            var cells = new List<SlotCell>(cellCount);
            for (int i = 0; i < cellCount; i++)
            {
                cells.Add(new SlotCell(i, 0));
            }

            var definition = new SlotPatternDefinition(
                "test", "Test", 0, 1f, SlotPatternRank.HorizontalSm, false, cells);
            return new SlotPatternMatch(definition, symbol, cells, cellCount, 0);
        }
    }
}
