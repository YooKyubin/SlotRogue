using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class MonsterTurnScheduleTests
    {
        [Test]
        public void ConsumeUpcomingTurn_CyclesThroughTurnSets()
        {
            var schedule = new MonsterTurnSchedule(
                new[] { new CombatEffect(CombatEffectKind.Damage, 1, CombatEffectTarget.Enemy) },
                new[] { new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy) });

            Assert.That(schedule.UpcomingTurnIndex, Is.Zero);
            Assert.That(schedule.UpcomingActions[0].Amount, Is.EqualTo(1));

            IReadOnlyList<CombatEffect> firstTurn = schedule.ConsumeUpcomingTurn();
            Assert.That(firstTurn[0].Amount, Is.EqualTo(1));
            Assert.That(schedule.UpcomingTurnIndex, Is.EqualTo(1));
            Assert.That(schedule.UpcomingActions[0].Amount, Is.EqualTo(2));

            schedule.ConsumeUpcomingTurn();
            Assert.That(schedule.UpcomingTurnIndex, Is.Zero);
            Assert.That(schedule.UpcomingActions[0].Amount, Is.EqualTo(1));
        }

        [Test]
        public void Reset_RestoresUpcomingTurnIndex()
        {
            var schedule = new MonsterTurnSchedule(
                new[] { new CombatEffect(CombatEffectKind.Damage, 1, CombatEffectTarget.Enemy) },
                new[] { new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy) });

            schedule.ConsumeUpcomingTurn();
            schedule.Reset();

            Assert.That(schedule.UpcomingTurnIndex, Is.Zero);
        }

        [Test]
        public void EmptyTurnList_FallsBackToSingleEmptyTurn()
        {
            var schedule = new MonsterTurnSchedule(System.Array.Empty<CombatEffect[]>());

            Assert.That(schedule.TurnCount, Is.EqualTo(1));
            Assert.That(schedule.UpcomingActions, Is.Empty);
        }
    }
}
