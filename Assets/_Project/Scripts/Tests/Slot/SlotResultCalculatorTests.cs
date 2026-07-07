using System;
using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Tests
{
    public sealed class SlotResultCalculatorTests
    {
        [SetUp]
        public void SetUp()
        {
            SlotSymbolAttackValues.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            SlotSymbolAttackValues.ResetToDefaults();
        }

        [Test]
        public void Calculate_NoMatches_CreatesNoAttack()
        {
            var calculator = new SlotResultCalculator();

            SlotCalculationResult result = calculator.Calculate(Array.Empty<SlotPatternMatch>());

            Assert.That(result.Damage, Is.EqualTo(0));
            Assert.That(result.AttackCount, Is.EqualTo(0));
            Assert.That(result.Defense, Is.EqualTo(0));
            Assert.That(result.HealAmount, Is.EqualTo(0));
            Assert.That(result.IsCritical, Is.False);
        }

        [Test]
        public void Calculate_SinglePattern_DamageFromPatternValueOnly()
        {
            var calculator = new SlotResultCalculator();
            // CalculatedValue = baseValue(6) * multiplier(2.0) = 12.
            SlotPatternMatch match = MakeMatch(SlotSymbolType.Cherry, multiplier: 2.0f, baseValue: 6);

            SlotCalculationResult result = calculator.Calculate(new[] { match });

            Assert.That(result.Damage, Is.EqualTo(12));
            Assert.That(result.AttackCount, Is.EqualTo(1));
            Assert.That(result.Defense, Is.EqualTo(0));
            Assert.That(result.HealAmount, Is.EqualTo(0));
            Assert.That(result.IsCritical, Is.False);
        }

        [Test]
        public void Calculate_MultiplePatterns_AccumulatesPatternValue()
        {
            var calculator = new SlotResultCalculator();
            // (6 * 1.0 = 6) + (28 * 2.0 = 56) = 62.
            var matches = new[]
            {
                MakeMatch(SlotSymbolType.Cherry, multiplier: 1.0f, baseValue: 6),
                MakeMatch(SlotSymbolType.Seven, multiplier: 2.0f, baseValue: 28),
            };

            SlotCalculationResult result = calculator.Calculate(matches);

            Assert.That(result.Damage, Is.EqualTo(62));
            Assert.That(result.AttackCount, Is.EqualTo(1));
        }

        [Test]
        public void Calculate_UsesPrecomputedSymbolPatternValues()
        {
            var calculator = new SlotResultCalculator();
            SlotCalculationResult cherry = calculator.Calculate(
                new[] { MakeMatch(SlotSymbolType.Cherry, 1.0f, 6) });
            SlotCalculationResult seven = calculator.Calculate(
                new[] { MakeMatch(SlotSymbolType.Seven, 1.0f, 21) });

            Assert.That(cherry.Damage, Is.EqualTo(6));
            Assert.That(seven.Damage, Is.EqualTo(21));
            Assert.That(seven.Defense, Is.EqualTo(0), "Seven 자체가 방어를 만들면 안 된다.");
            Assert.That(cherry.IsCritical, Is.False, "심볼 자체가 치명타를 만들면 안 된다.");
        }

        private static SlotPatternMatch MakeMatch(SlotSymbolType symbol, float multiplier, int baseValue)
        {
            var cells = new List<SlotCell>();
            for (int i = 0; i < baseValue; i++)
            {
                cells.Add(new SlotCell(i, 0));
            }

            var definition = new SlotPatternDefinition(
                "test", "Test", 0, multiplier, SlotPatternRank.HorizontalSm, false, cells);
            return new SlotPatternMatch(definition, symbol, cells, baseValue, 0);
        }
    }
}
