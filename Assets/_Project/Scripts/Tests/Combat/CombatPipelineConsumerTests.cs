using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using UnityEngine;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class CombatPipelineConsumerTests
    {
        [Test]
        public void OnSpinResolved_PublishesEventsAndFinalStateThroughPresenter()
        {
            var presenter = new BattlePresenter();
            var resolver = CreateResolver(CreateAction(MonsterActionKind.Attack, rawAttack: 10));
            var consumer = new CombatPipelineConsumer(resolver, presenter);
            CombatEventKind firstEventKind = default;
            BattleStateSnapshot completedSnapshot = default;
            bool eventCalled = false;
            bool completedCalled = false;

            presenter.CombatEventEmitted += evt =>
            {
                if (!eventCalled)
                {
                    firstEventKind = evt.Kind;
                }

                eventCalled = true;
            };
            presenter.TurnCompleted += snapshot =>
            {
                completedCalled = true;
                completedSnapshot = snapshot;
            };

            consumer.OnSpinResolved(new CombatSpinOutcome(5, 0));

            Assert.That(eventCalled, Is.True);
            Assert.That(completedCalled, Is.True);
            Assert.That(firstEventKind, Is.EqualTo(CombatEventKind.PlayerDamageToMonster));
            Assert.That(completedSnapshot.PlayerHp, Is.EqualTo(20));
            Assert.That(completedSnapshot.MonsterHp, Is.EqualTo(45));
        }

        private static BattleResolver CreateResolver(params MonsterActionDefinition[] steps)
        {
            var pattern = ScriptableObject.CreateInstance<MonsterPattern>();
            pattern.Loop = true;
            pattern.Steps = CreateSteps(steps);
            var monster = ScriptableObject.CreateInstance<MonsterDefinition>();
            monster.MaxHp = 50;
            monster.Pattern = pattern;
            return new BattleResolver(monster, 30);
        }

        private static PatternStep[] CreateSteps(MonsterActionDefinition[] definitions)
        {
            var steps = new PatternStep[definitions.Length];
            for (var index = 0; index < definitions.Length; index++)
            {
                steps[index] = new PatternStep
                {
                    Action = definitions[index]
                };
            }

            return steps;
        }

        private static MonsterActionDefinition CreateAction(
            MonsterActionKind kind,
            int rawAttack = 0,
            int defendValue = 0)
        {
            var action = ScriptableObject.CreateInstance<MonsterActionDefinition>();
            action.Kind = kind;
            action.RawAttack = rawAttack;
            action.DefendValue = defendValue;
            return action;
        }
    }
}
