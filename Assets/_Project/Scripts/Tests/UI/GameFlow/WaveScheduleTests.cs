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
        public void Evaluate_Battle1_ReturnsNormalThemeSection0Position0()
        {
            WaveResult result = WaveSchedule.CreateDefault().Evaluate(1);

            Assert.That(result.EncounterTier, Is.EqualTo(EncounterTier.Normal));
            Assert.That(result.ThemeSectionIndex, Is.EqualTo(0));
            Assert.That(result.PositionInWave, Is.EqualTo(0));
        }

        [Test]
        public void Evaluate_Battle5_ReturnsEliteThemeSection0Position4()
        {
            WaveResult result = WaveSchedule.CreateDefault().Evaluate(5);

            Assert.That(result.EncounterTier, Is.EqualTo(EncounterTier.Elite));
            Assert.That(result.ThemeSectionIndex, Is.EqualTo(0));
            Assert.That(result.PositionInWave, Is.EqualTo(4));
        }

        [Test]
        public void Evaluate_Battle10_ReturnsBossThemeSection0Position9()
        {
            WaveResult result = WaveSchedule.CreateDefault().Evaluate(10);

            Assert.That(result.EncounterTier, Is.EqualTo(EncounterTier.Boss));
            Assert.That(result.ThemeSectionIndex, Is.EqualTo(0));
            Assert.That(result.PositionInWave, Is.EqualTo(9));
        }

        [Test]
        public void Evaluate_Battle11_ReturnsNormalThemeSection1Position0()
        {
            WaveResult result = WaveSchedule.CreateDefault().Evaluate(11);

            Assert.That(result.EncounterTier, Is.EqualTo(EncounterTier.Normal));
            Assert.That(result.ThemeSectionIndex, Is.EqualTo(1));
            Assert.That(result.PositionInWave, Is.EqualTo(0));
        }

        [Test]
        public void Evaluate_Battle20_ReturnsBossThemeSection1Position9()
        {
            WaveResult result = WaveSchedule.CreateDefault().Evaluate(20);

            Assert.That(result.EncounterTier, Is.EqualTo(EncounterTier.Boss));
            Assert.That(result.ThemeSectionIndex, Is.EqualTo(1));
            Assert.That(result.PositionInWave, Is.EqualTo(9));
        }

        [Test]
        public void Evaluate_Battles1Through10_ReturnSameThemeSectionIndex()
        {
            WaveSchedule schedule = WaveSchedule.CreateDefault();

            for (int battleNumber = 1; battleNumber <= 10; battleNumber++)
            {
                Assert.That(schedule.Evaluate(battleNumber).ThemeSectionIndex, Is.EqualTo(0));
            }
        }

        [Test]
        public void Evaluate_PositionInWave_RepeatsEveryTenBattles()
        {
            WaveSchedule schedule = WaveSchedule.CreateDefault();

            Assert.That(schedule.Evaluate(1).PositionInWave, Is.EqualTo(0));
            Assert.That(schedule.Evaluate(10).PositionInWave, Is.EqualTo(9));
            Assert.That(schedule.Evaluate(11).PositionInWave, Is.EqualTo(0));
            Assert.That(schedule.Evaluate(20).PositionInWave, Is.EqualTo(9));
        }

        [Test]
        public void Evaluate_SinglePattern_RepeatsForEveryThemeSection()
        {
            var schedule = new WaveSchedule(new[]
            {
                Pattern(EncounterTier.Normal, EncounterTier.Elite),
            });

            Assert.That(schedule.Evaluate(1).EncounterTier, Is.EqualTo(EncounterTier.Normal));
            Assert.That(schedule.Evaluate(2).EncounterTier, Is.EqualTo(EncounterTier.Elite));
            Assert.That(schedule.Evaluate(3).EncounterTier, Is.EqualTo(EncounterTier.Normal));
            Assert.That(schedule.Evaluate(4).EncounterTier, Is.EqualTo(EncounterTier.Elite));
        }

        [Test]
        public void Evaluate_MultiplePatterns_SelectsPatternByThemeSection()
        {
            var schedule = new WaveSchedule(new[]
            {
                Pattern(EncounterTier.Normal, EncounterTier.Elite),
                Pattern(EncounterTier.Boss, EncounterTier.Normal),
            });

            Assert.That(schedule.Evaluate(1).EncounterTier, Is.EqualTo(EncounterTier.Normal));
            Assert.That(schedule.Evaluate(2).EncounterTier, Is.EqualTo(EncounterTier.Elite));
            Assert.That(schedule.Evaluate(3).EncounterTier, Is.EqualTo(EncounterTier.Boss));
            Assert.That(schedule.Evaluate(4).EncounterTier, Is.EqualTo(EncounterTier.Normal));
        }

        [Test]
        public void Evaluate_ThemeSectionAfterLastPattern_RepeatsLastPattern()
        {
            var schedule = new WaveSchedule(new[]
            {
                Pattern(EncounterTier.Normal, EncounterTier.Normal),
                Pattern(EncounterTier.Elite, EncounterTier.Boss),
            });

            Assert.That(schedule.Evaluate(5).EncounterTier, Is.EqualTo(EncounterTier.Elite));
            Assert.That(schedule.Evaluate(6).EncounterTier, Is.EqualTo(EncounterTier.Boss));
            Assert.That(schedule.Evaluate(7).EncounterTier, Is.EqualTo(EncounterTier.Elite));
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
