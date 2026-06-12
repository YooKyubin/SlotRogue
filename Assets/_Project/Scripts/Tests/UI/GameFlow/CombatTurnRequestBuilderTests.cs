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
                    new StatusEffectRequest(StatusEffectKind.Burn, 1),
                    new StatusEffectRequest(StatusEffectKind.Burn, 2),
                },
                activationSummary: "붉은 성냥, 붉은 점화탄");

            RunCombatRequestResult result = _builder.Build(
                request,
                relicResult,
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.StatusEffectsToApply.Count, Is.EqualTo(1));
            Assert.That(result.StatusEffectsToApply[0].Kind, Is.EqualTo(StatusEffectKind.Burn));
            Assert.That(result.StatusEffectsToApply[0].Duration, Is.EqualTo(1));
            Assert.That(result.StatusEffectsToApply[0].Magnitude, Is.EqualTo(3));
            Assert.That(result.StatusEffectsToApply[0].StackMode, Is.EqualTo(StatusStackMode.Refresh));
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
