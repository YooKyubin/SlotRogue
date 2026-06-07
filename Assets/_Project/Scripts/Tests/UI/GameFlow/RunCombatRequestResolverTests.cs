using NUnit.Framework;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class RunCombatRequestResolverTests
    {
        private RunCombatRequestResolver _resolver = null!;

        [SetUp]
        public void SetUp()
        {
            _resolver = new RunCombatRequestResolver();
        }

        [Test]
        public void Resolve_BlankRequest_AddsBaseAttack()
        {
            RunCombatRequestResult result = _resolver.Resolve(
                SlotPatternResult.NoMatch,
                SlotCombatRequest.Empty,
                StarterArtifactCatalog.Get(StarterArtifactId.None),
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.FinalRequest.Damage, Is.EqualTo(SlotCombatRequest.BaseAttackDamage));
            Assert.That(result.FinalRequest.AttackCount, Is.EqualTo(SlotCombatRequest.BaseAttackCount));
            Assert.That(result.FinalRequest.PatternName, Is.EqualTo(SlotCombatRequest.BaseAttackName));
        }

        [Test]
        public void Resolve_MatchingStarterArtifact_AppliesArtifactBonus()
        {
            var pattern = new SlotPatternResult(
                true,
                "Cherry x3",
                SlotSymbolType.Cherry,
                row: 0,
                startColumn: 0,
                matchLength: 3,
                score: 30);
            var request = new SlotCombatRequest(18, 0, 1, 0, false, "Cherry x3");

            RunCombatRequestResult result = _resolver.Resolve(
                pattern,
                request,
                StarterArtifactCatalog.Get(StarterArtifactId.Cherry),
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.StarterArtifactActivation.Activated, Is.True);
            Assert.That(result.FinalRequest.Damage, Is.EqualTo(23));
        }

        [Test]
        public void Resolve_NonMatchingStarterArtifact_DoesNotApplyArtifactBonus()
        {
            var pattern = new SlotPatternResult(
                true,
                "Clover x3",
                SlotSymbolType.Clover,
                row: 0,
                startColumn: 0,
                matchLength: 3,
                score: 30);
            var request = new SlotCombatRequest(15, 0, 1, 0, false, "Clover x3");

            RunCombatRequestResult result = _resolver.Resolve(
                pattern,
                request,
                StarterArtifactCatalog.Get(StarterArtifactId.Cherry),
                runDamageBonus: 0,
                runDefenseBonus: 0);

            Assert.That(result.StarterArtifactActivation.Activated, Is.False);
            Assert.That(result.FinalRequest.Damage, Is.EqualTo(15));
        }

        [Test]
        public void Resolve_RunBonuses_AdjustFinalRequest()
        {
            var pattern = new SlotPatternResult(
                true,
                "Grape x3",
                SlotSymbolType.Grape,
                row: 1,
                startColumn: 0,
                matchLength: 3,
                score: 30);
            var request = new SlotCombatRequest(6, 0, 1, 12, false, "Grape x3");

            RunCombatRequestResult result = _resolver.Resolve(
                pattern,
                request,
                StarterArtifactCatalog.Get(StarterArtifactId.Grape),
                runDamageBonus: 2,
                runDefenseBonus: 2);

            Assert.That(result.FinalRequest.Damage, Is.EqualTo(8));
            Assert.That(result.FinalRequest.Defense, Is.EqualTo(2));
            Assert.That(result.FinalRequest.HealAmount, Is.EqualTo(16));
            Assert.That(result.RunBonusSummary, Is.EqualTo("damage +2, defense +2"));
        }

        [Test]
        public void Resolve_MultiHitRequest_CalculatesTotalAttackPower()
        {
            var request = new SlotCombatRequest(6, 0, 3, 0, false, "Multi Hit");

            RunCombatRequestResult result = _resolver.Resolve(
                SlotPatternResult.NoMatch,
                request,
                StarterArtifactCatalog.Get(StarterArtifactId.None),
                runDamageBonus: 2,
                runDefenseBonus: 0);

            Assert.That(result.FinalRequest.Damage, Is.EqualTo(8));
            Assert.That(result.FinalRequest.AttackCount, Is.EqualTo(3));
            Assert.That(result.AttackPower, Is.EqualTo(24));
        }
    }
}
