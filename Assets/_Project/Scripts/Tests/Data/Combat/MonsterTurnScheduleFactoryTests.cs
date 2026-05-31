using System;
using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using UnityEngine;

namespace SlotRogue.Data.Tests.Combat
{
    public sealed class MonsterTurnScheduleFactoryTests
    {
        [Test]
        public void FromPattern_Null_ReturnsDefaultFallback()
        {
            MonsterTurnSchedule schedule = MonsterTurnScheduleFactory.FromPattern(null);

            Assert.That(schedule.TurnCount, Is.EqualTo(1));
            Assert.That(schedule.UpcomingActions[0].Kind, Is.EqualTo(CombatEffectKind.Damage));
            Assert.That(schedule.UpcomingActions[0].Amount, Is.EqualTo(2));
            Assert.That(schedule.UpcomingActions[0].Target, Is.EqualTo(CombatEffectTarget.Enemy));
        }

        [Test]
        public void FromPattern_EmptyTurns_ReturnsSingleEmptyTurn()
        {
            var pattern = ScriptableObject.CreateInstance<MonsterTurnPatternDefinition>();
            pattern.turns = Array.Empty<MonsterTurnStepDefinition>();

            MonsterTurnSchedule schedule = MonsterTurnScheduleFactory.FromPattern(pattern);

            Assert.That(schedule.TurnCount, Is.EqualTo(1));
            Assert.That(schedule.UpcomingActions, Is.Empty);
        }

        [Test]
        public void FromSteps_ThreeTurns_CyclesAfterConsume()
        {
            MonsterTurnSchedule schedule = MonsterTurnScheduleFactory.FromSteps(
                new[] { new[] { Step(1) }, new[] { Step(2) }, new[] { Step(3) } });

            Assert.That(schedule.UpcomingTurnIndex, Is.Zero);
            Assert.That(schedule.UpcomingActions[0].Amount, Is.EqualTo(1));

            IReadOnlyList<CombatEffect> firstTurn = schedule.ConsumeUpcomingTurn();
            Assert.That(firstTurn[0].Amount, Is.EqualTo(1));
            Assert.That(schedule.UpcomingActions[0].Amount, Is.EqualTo(2));

            schedule.ConsumeUpcomingTurn();
            schedule.ConsumeUpcomingTurn();
            Assert.That(schedule.UpcomingTurnIndex, Is.Zero);
            Assert.That(schedule.UpcomingActions[0].Amount, Is.EqualTo(1));
        }

        [Test]
        public void FromSteps_MultiActionTurn_PreservesOrder()
        {
            MonsterTurnSchedule schedule = MonsterTurnScheduleFactory.FromSteps(
                new[]
                {
                    new[]
                    {
                        Step(1),
                        new CombatEffectStep
                        {
                            kind = CombatEffectKind.Shield,
                            amount = 3,
                            target = CombatEffectTarget.Self,
                        },
                        Step(2),
                    },
                });

            IReadOnlyList<CombatEffect> actions = schedule.UpcomingActions;

            Assert.That(actions.Count, Is.EqualTo(3));
            Assert.That(actions[0].Kind, Is.EqualTo(CombatEffectKind.Damage));
            Assert.That(actions[0].Amount, Is.EqualTo(1));
            Assert.That(actions[1].Kind, Is.EqualTo(CombatEffectKind.Shield));
            Assert.That(actions[1].Amount, Is.EqualTo(3));
            Assert.That(actions[2].Amount, Is.EqualTo(2));
        }

        [Test]
        public void FromSteps_NullOrEmpty_ReturnsDefaultFallback()
        {
            MonsterTurnSchedule fromNull = MonsterTurnScheduleFactory.FromSteps((CombatEffectStep[][])null);
            MonsterTurnSchedule fromEmpty = MonsterTurnScheduleFactory.FromSteps(Array.Empty<CombatEffectStep[]>());

            Assert.That(fromNull.UpcomingActions[0].Amount, Is.EqualTo(2));
            Assert.That(fromEmpty.UpcomingActions[0].Amount, Is.EqualTo(2));
        }

        private static CombatEffectStep Step(int amount)
        {
            return new CombatEffectStep
            {
                kind = CombatEffectKind.Damage,
                amount = amount,
                target = CombatEffectTarget.Enemy,
            };
        }
    }
}
