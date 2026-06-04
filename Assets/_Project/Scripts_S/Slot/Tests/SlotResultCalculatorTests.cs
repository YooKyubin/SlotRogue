using NUnit.Framework;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Tests
{
    public sealed class SlotResultCalculatorTests
    {
        [Test]
        public void Calculate_CloverLineFour_CreatesCriticalDamage()
        {
            var patternResult = new SlotPatternResult(true, "Clover Line x4", SlotSymbolType.Clover, 0, 0, 4, 32);
            var calculator = new SlotResultCalculator();

            SlotCalculationResult result = calculator.Calculate(patternResult);

            Assert.That(result.Damage, Is.EqualTo(40));
            Assert.That(result.AttackCount, Is.EqualTo(1));
            Assert.That(result.HealAmount, Is.EqualTo(0));
            Assert.That(result.IsCritical, Is.True);
        }

        [Test]
        public void Calculate_SevenLineThree_CreatesDefense()
        {
            var patternResult = new SlotPatternResult(true, "Seven Line x3", SlotSymbolType.Seven, 0, 0, 3, 15);
            var calculator = new SlotResultCalculator();

            SlotCalculationResult result = calculator.Calculate(patternResult);

            Assert.That(result.Damage, Is.EqualTo(6));
            Assert.That(result.Defense, Is.EqualTo(15));
            Assert.That(result.AttackCount, Is.EqualTo(1));
        }

        [Test]
        public void Calculate_NoMatch_CreatesBaseAttack()
        {
            var calculator = new SlotResultCalculator();

            SlotCalculationResult result = calculator.Calculate(SlotPatternResult.NoMatch);

            Assert.That(result.Damage, Is.EqualTo(SlotCombatRequest.BaseAttackDamage));
            Assert.That(result.AttackCount, Is.EqualTo(SlotCombatRequest.BaseAttackCount));
            Assert.That(result.IsCritical, Is.False);
        }
    }
}
