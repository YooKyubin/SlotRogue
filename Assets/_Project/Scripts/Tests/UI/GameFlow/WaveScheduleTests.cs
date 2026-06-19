using System;
using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Data.GameFlow;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class WaveScheduleTests
    {
        [Test]
        public void Evaluate_Battle1_ReturnsNormalCycle0Position0()
        {
            WaveResult result = WaveSchedule.CreateDefault().Evaluate(1);

            Assert.That(result.Tier, Is.EqualTo(EncounterTier.Normal));
            Assert.That(result.Cycle, Is.EqualTo(0));
            Assert.That(result.PositionInCycle, Is.EqualTo(0));
        }

        [Test]
        public void Evaluate_Battle5_ReturnsEliteCycle0Position4()
        {
            WaveResult result = WaveSchedule.CreateDefault().Evaluate(5);

            Assert.That(result.Tier, Is.EqualTo(EncounterTier.Elite));
            Assert.That(result.Cycle, Is.EqualTo(0));
            Assert.That(result.PositionInCycle, Is.EqualTo(4));
        }

        [Test]
        public void Evaluate_Battle10_ReturnsBossCycle0Position9()
        {
            WaveResult result = WaveSchedule.CreateDefault().Evaluate(10);

            Assert.That(result.Tier, Is.EqualTo(EncounterTier.Boss));
            Assert.That(result.Cycle, Is.EqualTo(0));
            Assert.That(result.PositionInCycle, Is.EqualTo(9));
        }

        [Test]
        public void Evaluate_Battle11_ReturnsNormalCycle1Position0()
        {
            WaveResult result = WaveSchedule.CreateDefault().Evaluate(11);

            Assert.That(result.Tier, Is.EqualTo(EncounterTier.Normal));
            Assert.That(result.Cycle, Is.EqualTo(1));
            Assert.That(result.PositionInCycle, Is.EqualTo(0));
        }

        [Test]
        public void Evaluate_Battle20_ReturnsBossCycle1Position9()
        {
            WaveResult result = WaveSchedule.CreateDefault().Evaluate(20);

            Assert.That(result.Tier, Is.EqualTo(EncounterTier.Boss));
            Assert.That(result.Cycle, Is.EqualTo(1));
            Assert.That(result.PositionInCycle, Is.EqualTo(9));
        }

        [Test]
        public void Evaluate_SinglePattern_RepeatsForEveryCycle()
        {
            var schedule = new WaveSchedule(new[]
            {
                Pattern(EncounterTier.Normal, EncounterTier.Elite),
            });

            Assert.That(schedule.Evaluate(1).Tier, Is.EqualTo(EncounterTier.Normal));
            Assert.That(schedule.Evaluate(2).Tier, Is.EqualTo(EncounterTier.Elite));
            Assert.That(schedule.Evaluate(3).Tier, Is.EqualTo(EncounterTier.Normal));
            Assert.That(schedule.Evaluate(4).Tier, Is.EqualTo(EncounterTier.Elite));
        }

        [Test]
        public void Evaluate_MultiplePatterns_SelectsPatternByCycle()
        {
            var schedule = new WaveSchedule(new[]
            {
                Pattern(EncounterTier.Normal, EncounterTier.Elite),
                Pattern(EncounterTier.Boss, EncounterTier.Normal),
            });

            Assert.That(schedule.Evaluate(1).Tier, Is.EqualTo(EncounterTier.Normal));
            Assert.That(schedule.Evaluate(2).Tier, Is.EqualTo(EncounterTier.Elite));
            Assert.That(schedule.Evaluate(3).Tier, Is.EqualTo(EncounterTier.Boss));
            Assert.That(schedule.Evaluate(4).Tier, Is.EqualTo(EncounterTier.Normal));
        }

        [Test]
        public void Evaluate_CycleAfterLastPattern_RepeatsLastPattern()
        {
            var schedule = new WaveSchedule(new[]
            {
                Pattern(EncounterTier.Normal, EncounterTier.Normal),
                Pattern(EncounterTier.Elite, EncounterTier.Boss),
            });

            Assert.That(schedule.Evaluate(5).Tier, Is.EqualTo(EncounterTier.Elite));
            Assert.That(schedule.Evaluate(6).Tier, Is.EqualTo(EncounterTier.Boss));
            Assert.That(schedule.Evaluate(7).Tier, Is.EqualTo(EncounterTier.Elite));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Evaluate_InvalidBattleNumber_Fails(int battleNumber)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => WaveSchedule.CreateDefault().Evaluate(battleNumber));
        }

        [Test]
        public void Constructor_NoPatterns_Fails()
        {
            Assert.Throws<ArgumentException>(
                () => new WaveSchedule(Array.Empty<IReadOnlyList<EncounterTier>>()));
        }

        [Test]
        public void Constructor_EmptyPattern_Fails()
        {
            Assert.Throws<ArgumentException>(
                () => new WaveSchedule(new IReadOnlyList<EncounterTier>[]
                {
                    Array.Empty<EncounterTier>(),
                }));
        }

        [Test]
        public void Constructor_NullPattern_Fails()
        {
            Assert.Throws<ArgumentException>(
                () => new WaveSchedule(new IReadOnlyList<EncounterTier>[]
                {
                    null,
                }));
        }

        [Test]
        public void Constructor_DifferentPatternLengths_Fails()
        {
            Assert.Throws<ArgumentException>(
                () => new WaveSchedule(new[]
                {
                    Pattern(EncounterTier.Normal, EncounterTier.Elite),
                    Pattern(EncounterTier.Boss),
                }));
        }

        private static IReadOnlyList<EncounterTier> Pattern(params EncounterTier[] tiers)
        {
            return tiers;
        }
    }
}
