using System;
using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class StatusEffectPresentationMapperTests
    {
        [TestCase(StatusEffectKind.Burn, 3, 8, 9, 3)]
        [TestCase(StatusEffectKind.Infection, 7, 2, 9, 2)]
        [TestCase(StatusEffectKind.Vulnerable, 7, 1, 9, 1)]
        [TestCase(StatusEffectKind.Weaken, 7, 4, 9, 4)]
        [TestCase(StatusEffectKind.Lifesteal, 7, 5, 9, 5)]
        [TestCase(StatusEffectKind.Thorns, 6, 8, 9, 6)]
        [TestCase(StatusEffectKind.Freeze, 7, 8, 2, 2)]
        public void Map_UsesStatusSpecificDisplayValue(
            StatusEffectKind kind,
            int magnitude,
            int stackCount,
            int remainingTurns,
            int expectedDisplayValue)
        {
            StatusEffectViewData result = StatusEffectPresentationMapper.Map(
                kind,
                magnitude,
                stackCount,
                remainingTurns);

            Assert.That(result.Kind, Is.EqualTo(kind));
            Assert.That(result.DisplayValue, Is.EqualTo(expectedDisplayValue));
            Assert.That(result.ShowValue, Is.True);
        }

        [Test]
        public void Map_DisplayValueOne_RemainsVisible()
        {
            StatusEffectViewData result = StatusEffectPresentationMapper.Map(
                StatusEffectKind.Vulnerable,
                magnitude: 0,
                stackCount: 1,
                remainingTurns: 0);

            Assert.That(result.DisplayValue, Is.EqualTo(1));
            Assert.That(result.ShowValue, Is.True);
        }

        [Test]
        public void Map_NonPositiveDisplayValue_HidesValue()
        {
            StatusEffectViewData result = StatusEffectPresentationMapper.Map(
                StatusEffectKind.Burn,
                magnitude: 0,
                stackCount: 5,
                remainingTurns: 5);

            Assert.That(result.DisplayValue, Is.Zero);
            Assert.That(result.ShowValue, Is.False);
        }

        [Test]
        public void Map_UnsupportedKind_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                StatusEffectPresentationMapper.Map(
                    (StatusEffectKind)999,
                    magnitude: 1,
                    stackCount: 1,
                    remainingTurns: 1));
        }
    }
}
