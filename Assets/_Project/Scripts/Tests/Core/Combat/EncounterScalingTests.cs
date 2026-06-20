using System;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class EncounterScalingTests
    {
        [Test]
        public void Scale_NormalTier_UsesBaseHp()
        {
            var scaling = new EncounterScaling(Config());

            EncounterScaleResult result = scaling.Scale(new EncounterScaleRequest(
                baseMaxHp: 20,
                battleNumber: 1,
                cycle: 0,
                tierHpMultiplier: 1f));

            Assert.That(result.MaxHp, Is.EqualTo(20));
        }

        [Test]
        public void Scale_EliteTier_UsesTierMultiplier()
        {
            var scaling = new EncounterScaling(Config(eliteTierHpMultiplier: 1.5f));

            EncounterScaleResult result = scaling.Scale(new EncounterScaleRequest(
                baseMaxHp: 20,
                battleNumber: 1,
                cycle: 0,
                tierHpMultiplier: 1.5f));

            Assert.That(result.MaxHp, Is.EqualTo(30));
        }

        [Test]
        public void Scale_BossTier_UsesTierMultiplier()
        {
            var scaling = new EncounterScaling(Config(bossTierHpMultiplier: 2f));

            EncounterScaleResult result = scaling.Scale(new EncounterScaleRequest(
                baseMaxHp: 20,
                battleNumber: 1,
                cycle: 0,
                tierHpMultiplier: 2f));

            Assert.That(result.MaxHp, Is.EqualTo(40));
        }

        [Test]
        public void Scale_BattleNumberIncrease_AddsBattleGrowth()
        {
            var scaling = new EncounterScaling(Config(hpIncreasePerBattle: 0.1f));

            EncounterScaleResult result = scaling.Scale(new EncounterScaleRequest(
                baseMaxHp: 20,
                battleNumber: 3,
                cycle: 0,
                tierHpMultiplier: 1f));

            Assert.That(result.MaxHp, Is.EqualTo(24));
        }

        [Test]
        public void Scale_CycleIncrease_AddsCycleGrowth()
        {
            var scaling = new EncounterScaling(Config(hpIncreasePerCycle: 0.25f));

            EncounterScaleResult result = scaling.Scale(new EncounterScaleRequest(
                baseMaxHp: 20,
                battleNumber: 1,
                cycle: 2,
                tierHpMultiplier: 1f));

            Assert.That(result.MaxHp, Is.EqualTo(30));
        }

        [Test]
        public void Scale_InvalidInput_Fails()
        {
            var scaling = new EncounterScaling(Config());

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EncounterScaleRequest(baseMaxHp: 0, battleNumber: 1, cycle: 0, tierHpMultiplier: 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EncounterScaleRequest(baseMaxHp: 10, battleNumber: 0, cycle: 0, tierHpMultiplier: 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EncounterScaleRequest(baseMaxHp: 10, battleNumber: 1, cycle: -1, tierHpMultiplier: 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EncounterScaleRequest(baseMaxHp: 10, battleNumber: 1, cycle: 0, tierHpMultiplier: 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EncounterBalanceConfig(
                    hpIncreasePerBattle: -0.1f,
                    hpIncreasePerCycle: 0f,
                    normalTierHpMultiplier: 1f,
                    eliteTierHpMultiplier: 1f,
                    bossTierHpMultiplier: 1f));

            Assert.DoesNotThrow(() => scaling.Scale(new EncounterScaleRequest(
                baseMaxHp: 10,
                battleNumber: 1,
                cycle: 0,
                tierHpMultiplier: 1f)));
        }

        private static EncounterBalanceConfig Config(
            float hpIncreasePerBattle = 0f,
            float hpIncreasePerCycle = 0f,
            float normalTierHpMultiplier = 1f,
            float eliteTierHpMultiplier = 1f,
            float bossTierHpMultiplier = 1f)
        {
            return new EncounterBalanceConfig(
                hpIncreasePerBattle,
                hpIncreasePerCycle,
                normalTierHpMultiplier,
                eliteTierHpMultiplier,
                bossTierHpMultiplier);
        }
    }
}
