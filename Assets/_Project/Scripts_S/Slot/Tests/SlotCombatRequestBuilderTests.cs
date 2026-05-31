using NUnit.Framework;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Tests
{
    public sealed class SlotCombatRequestBuilderTests
    {
        [Test]
        public void Build_MapsCalculationAndPatternFields()
        {
            var patternResult = new SlotPatternResult(
                hasMatch: true,
                patternName: "Skull Line x5",
                symbol: SlotSymbolType.Skull,
                row: 0,
                startColumn: 0,
                matchLength: 5,
                score: 100);
            var calculationResult = new SlotCalculationResult(22, 7, 3, 5, true);
            var builder = new SlotCombatRequestBuilder();

            SlotCombatRequest request = builder.Build(patternResult, calculationResult);

            Assert.That(request.Damage, Is.EqualTo(22));
            Assert.That(request.Defense, Is.EqualTo(7));
            Assert.That(request.AttackCount, Is.EqualTo(3));
            Assert.That(request.HealAmount, Is.EqualTo(5));
            Assert.That(request.IsCritical, Is.True);
            Assert.That(request.PatternName, Is.EqualTo("Skull Line x5"));
        }

        [Test]
        public void Build_ReturnsEmpty_WhenInputsAreNull()
        {
            var builder = new SlotCombatRequestBuilder();

            Assert.That(builder.Build(null, null), Is.SameAs(SlotCombatRequest.Empty));
        }

        [Test]
        public void Build_NoMatchUsesBaseAttackName()
        {
            var calculationResult = new SlotCalculationResult(
                SlotCombatRequest.BaseAttackDamage,
                0,
                SlotCombatRequest.BaseAttackCount,
                0,
                false);
            var builder = new SlotCombatRequestBuilder();

            SlotCombatRequest request = builder.Build(SlotPatternResult.NoMatch, calculationResult);

            Assert.That(request.PatternName, Is.EqualTo(SlotCombatRequest.BaseAttackName));
            Assert.That(request.Damage, Is.EqualTo(SlotCombatRequest.BaseAttackDamage));
            Assert.That(request.AttackCount, Is.EqualTo(SlotCombatRequest.BaseAttackCount));
        }
    }
}
