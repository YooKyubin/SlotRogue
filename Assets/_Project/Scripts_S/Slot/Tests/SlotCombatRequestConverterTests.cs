using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Tests
{
    public sealed class SlotCombatRequestConverterTests
    {
        [Test]
        public void Convert_UsesCalculationAndPatternData()
        {
            var patternResult = new SlotPatternResult(true, "Sword Line x3", SlotSymbolType.Sword, 0, 0, 3, 18);
            var calculationResult = new SlotCalculationResult(18, 4, 1, 2, false);
            var converter = new SlotCombatRequestConverter();

            SlotCombatRequest request = converter.Convert(patternResult, calculationResult);

            Assert.That(request.Damage, Is.EqualTo(18));
            Assert.That(request.Defense, Is.EqualTo(4));
            Assert.That(request.AttackCount, Is.EqualTo(1));
            Assert.That(request.HealAmount, Is.EqualTo(2));
            Assert.That(request.IsCritical, Is.False);
            Assert.That(request.PatternName, Is.EqualTo("Sword Line x3"));
        }

        [Test]
        public void ToCombatSpinOutcome_MapsDamageAndDefenseOnly()
        {
            var request = new SlotCombatRequest(22, 7, 3, 5, true, "Skull Line x5");
            var converter = new SlotCombatRequestConverter();

            CombatSpinOutcome outcome = converter.ToCombatSpinOutcome(request);

            Assert.That(outcome.Attack, Is.EqualTo(22));
            Assert.That(outcome.Defense, Is.EqualTo(7));
        }
    }
}
