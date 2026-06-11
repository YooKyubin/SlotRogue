using System;
using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Tests
{
    public sealed class SlotResultCalculatorTests
    {
        [Test]
        public void Calculate_NoMatches_CreatesBaseAttack()
        {
            var calculator = new SlotResultCalculator();

            SlotCalculationResult result = calculator.Calculate(Array.Empty<SlotPatternMatch>());

            Assert.That(result.Damage, Is.EqualTo(SlotCombatRequest.BaseAttackDamage));
            Assert.That(result.AttackCount, Is.EqualTo(SlotCombatRequest.BaseAttackCount));
            Assert.That(result.Defense, Is.EqualTo(0));
            Assert.That(result.HealAmount, Is.EqualTo(0));
            Assert.That(result.IsCritical, Is.False);
        }

        [Test]
        public void Calculate_SinglePattern_DamageFromPatternValueOnly()
        {
            var calculator = new SlotResultCalculator();
            // CalculatedValue = baseValue(3) * multiplier(2.0) = 6 → damage = 6 * 2 = 12
            SlotPatternMatch match = MakeMatch(SlotSymbolType.Cherry, multiplier: 2.0f, baseValue: 3);

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
            // (3 * 1.0 = 3) + (4 * 2.0 = 8) = 11 → damage = 11 * 2 = 22
            var matches = new[]
            {
                MakeMatch(SlotSymbolType.Bell, multiplier: 1.0f, baseValue: 3),
                MakeMatch(SlotSymbolType.Seven, multiplier: 2.0f, baseValue: 4),
            };

            SlotCalculationResult result = calculator.Calculate(matches);

            Assert.That(result.Damage, Is.EqualTo(22));
            Assert.That(result.AttackCount, Is.EqualTo(1));
        }

        [Test]
        public void Calculate_SymbolDoesNotAffectResult()
        {
            var calculator = new SlotResultCalculator();
            // 동일한 패턴 가치라면 심볼이 달라도 결과가 같아야 한다(심볼 자체 효과 없음).
            SlotCalculationResult cherry = calculator.Calculate(
                new[] { MakeMatch(SlotSymbolType.Cherry, 2.0f, 3) });
            SlotCalculationResult diamond = calculator.Calculate(
                new[] { MakeMatch(SlotSymbolType.Diamond, 2.0f, 3) });
            SlotCalculationResult seven = calculator.Calculate(
                new[] { MakeMatch(SlotSymbolType.Seven, 2.0f, 3) });

            Assert.That(cherry.Damage, Is.EqualTo(diamond.Damage));
            Assert.That(cherry.Damage, Is.EqualTo(seven.Damage));
            Assert.That(diamond.HealAmount, Is.EqualTo(0), "Diamond 자체가 회복을 만들면 안 된다.");
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
