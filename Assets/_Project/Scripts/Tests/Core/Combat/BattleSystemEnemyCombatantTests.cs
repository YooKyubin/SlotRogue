using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class BattleSystemEnemyCombatantTests
    {
        private BattleSystem _battle = null!;

        [SetUp]
        public void SetUp()
        {
            _battle = new BattleSystem();
        }

        [Test]
        public void StartBattle_WithEnemyCombatant_PreparesInitialPlan()
        {
            CombatParticipant player = Player(maxHp: 30);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);
            EnemyCombatant combatant = Combatant(
                enemy,
                Plan(CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy));

            _battle.StartBattle(player, new[] { combatant });

            Assert.That(_battle.CurrentPhase, Is.EqualTo(BattlePhase.PlayerTurn));
            Assert.That(_battle.TryGetUpcomingEnemyTurn(enemy.Id, out EnemyUpcomingTurn upcomingTurn), Is.True);
            Assert.That(upcomingTurn.ParticipantId.Value, Is.EqualTo(enemy.Id.Value));
            AssertPlan(upcomingTurn.Plan, CombatEffectKind.Damage, 4);
        }

        [Test]
        public void ApplyPlayerTurn_ExecutesStoredPlanAndAdvancesNextPlan()
        {
            CombatParticipant player = Player(maxHp: 30);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 30);
            EnemyCombatant combatant = Combatant(
                enemy,
                Plan(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy),
                Plan(CombatEffectKind.Shield, 5, CombatEffectTarget.Self));
            _battle.StartBattle(player, new[] { combatant });

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            Assert.That(player.CurrentHp, Is.EqualTo(27));
            Assert.That(_battle.TryGetUpcomingEnemyTurn(enemy.Id, out EnemyUpcomingTurn upcomingTurn), Is.True);
            AssertPlan(upcomingTurn.Plan, CombatEffectKind.Shield, 5);
        }

        [Test]
        public void ApplyPlayerTurn_SkippedEnemyAction_StillAdvancesNextPlan()
        {
            CombatParticipant player = Player(maxHp: 30);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 30);
            EnemyCombatant combatant = Combatant(
                enemy,
                Plan(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy),
                Plan(CombatEffectKind.Damage, 7, CombatEffectTarget.Enemy));
            _battle.StartBattle(player, new[] { combatant });

            _battle.ApplyPlayerTurn(new[]
            {
                CombatEffect.ApplyStatus(
                    new StatusEffectSpec(StatusEffectKind.Freeze, duration: 1, magnitude: 0, StatusStackMode.Refresh),
                    CombatEffectTarget.Enemy),
            });

            Assert.That(player.CurrentHp, Is.EqualTo(30));
            Assert.That(_battle.Events, Has.Some.Matches<CombatEvent>(e =>
                e.Kind == CombatEventKind.ActionSkipped &&
                e.StatusEffectKind == StatusEffectKind.Freeze));
            Assert.That(_battle.TryGetUpcomingEnemyTurn(enemy.Id, out EnemyUpcomingTurn upcomingTurn), Is.True);
            AssertPlan(upcomingTurn.Plan, CombatEffectKind.Damage, 7);
        }

        [Test]
        public void ApplyPlayerTurn_MultipleEnemyCombatants_ExecutesInRosterOrder()
        {
            CombatParticipant player = Player(maxHp: 30);
            CombatParticipant enemy0 = Enemy(id: 100, maxHp: 20);
            CombatParticipant enemy1 = Enemy(id: 101, maxHp: 20);
            _battle.StartBattle(
                player,
                new[]
                {
                    Combatant(enemy0, Plan(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy)),
                    Combatant(enemy1, Plan(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy)),
                });

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            int[] damageAmounts = _battle.Events
                .Where(e =>
                    e.Kind == CombatEventKind.EffectApplied &&
                    e.Effect.Kind == CombatEffectKind.Damage &&
                    e.IsPlayerParticipant)
                .Select(e => e.Effect.Amount)
                .ToArray();
            Assert.That(damageAmounts, Is.EqualTo(new[] { 2, 5 }));
            Assert.That(player.CurrentHp, Is.EqualTo(23));
        }

        [Test]
        public void ApplyPlayerTurn_EnemyActionStartedPrecedesEffectAppliedAndActionCompleted()
        {
            CombatParticipant player = Player(maxHp: 30);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);
            _battle.StartBattle(
                player,
                new[] { Combatant(enemy, NamedPlan("Attack", CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy)) });

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            int startedIndex = FindEventIndex(CombatEventKind.ActionStarted, enemy.Id);
            int effectIndex = FindEventIndex(CombatEventKind.EffectApplied, enemy.Id);
            int completedIndex = FindEventIndex(CombatEventKind.ActionCompleted, enemy.Id);
            Assert.That(startedIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(effectIndex, Is.GreaterThan(startedIndex));
            Assert.That(completedIndex, Is.GreaterThan(effectIndex));
            Assert.That(_battle.Events[startedIndex].Phase, Is.EqualTo(BattlePhase.EnemyTurn));
            Assert.That(_battle.Events[startedIndex].SourceParticipantId.Value, Is.EqualTo(enemy.Id.Value));
            Assert.That(_battle.Events[startedIndex].ActionName, Is.EqualTo("Attack"));
        }

        [Test]
        public void ApplyPlayerTurn_EnemyActionEndingBattleRecordsActionCompletedBeforeBattleEnded()
        {
            CombatParticipant player = Player(maxHp: 4);
            CombatParticipant enemy = Enemy(id: 100, maxHp: 20);
            _battle.StartBattle(
                player,
                new[] { Combatant(enemy, NamedPlan("Attack", CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy)) });

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            int startedIndex = FindEventIndex(CombatEventKind.ActionStarted, enemy.Id);
            int effectIndex = FindEventIndex(CombatEventKind.EffectApplied, enemy.Id);
            int completedIndex = FindEventIndex(CombatEventKind.ActionCompleted, enemy.Id);
            int battleEndedIndex = FindFirstEventIndex(CombatEventKind.BattleEnded);
            Assert.That(startedIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(effectIndex, Is.GreaterThan(startedIndex));
            Assert.That(completedIndex, Is.GreaterThan(effectIndex));
            Assert.That(battleEndedIndex, Is.GreaterThan(completedIndex));
            Assert.That(_battle.Events[completedIndex].ActionName, Is.EqualTo("Attack"));
            Assert.That(_battle.Events.Count(e =>
                e.Kind == CombatEventKind.ActionCompleted &&
                e.SourceParticipantId.Value == enemy.Id.Value), Is.EqualTo(1));
            Assert.That(_battle.EndReason, Is.EqualTo(BattleEndReason.Defeat));
        }

        [Test]
        public void ApplyPlayerTurn_DirectTargetingRetargetsWhenSelectedEnemyDies()
        {
            CombatParticipant player = Player(maxHp: 30);
            CombatParticipant enemy0 = Enemy(id: 100, maxHp: 5);
            CombatParticipant enemy1 = Enemy(id: 101, maxHp: 20);
            _battle.StartBattle(
                player,
                new[]
                {
                    Combatant(enemy0, Plan(CombatEffectKind.Damage, 0, CombatEffectTarget.Enemy)),
                    Combatant(enemy1, Plan(CombatEffectKind.Damage, 0, CombatEffectTarget.Enemy)),
                });

            _battle.ApplyPlayerTurn(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        5,
                        CombatEffectTarget.SelectedEnemy(enemy0.Id)),
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        4,
                        CombatEffectTarget.SelectedEnemy(enemy0.Id)),
                },
                selectedTargetId: enemy0.Id);

            Assert.That(enemy0.IsDead, Is.True);
            Assert.That(enemy1.CurrentHp, Is.EqualTo(16));
        }

        private static EnemyCombatant Combatant(CombatParticipant enemy, params EnemyActionPlan[] plans)
        {
            return new EnemyCombatant(enemy, new FixedSequenceEnemyActionPlanner(plans));
        }

        private static EnemyActionPlan Plan(
            CombatEffectKind kind,
            int amount,
            CombatEffectTarget target)
        {
            return new EnemyActionPlan(new[] { new CombatEffect(kind, amount, target) });
        }

        private static EnemyActionPlan NamedPlan(
            string actionName,
            CombatEffectKind kind,
            int amount,
            CombatEffectTarget target)
        {
            return EnemyActionPlan.FromActions(new[]
            {
                new EnemyPlannedAction(
                    new EnemyActionKey(1),
                    actionName,
                    new[]
                    {
                        EnemyActionEffect.FromCombatEffect(new CombatEffect(kind, amount, target)),
                    }),
            });
        }

        private int FindEventIndex(CombatEventKind kind, CombatParticipantId sourceParticipantId)
        {
            for (int index = 0; index < _battle.Events.Count; index++)
            {
                CombatEvent combatEvent = _battle.Events[index];
                if (combatEvent.Kind == kind &&
                    combatEvent.SourceParticipantId.Value == sourceParticipantId.Value)
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindFirstEventIndex(CombatEventKind kind)
        {
            for (int index = 0; index < _battle.Events.Count; index++)
            {
                if (_battle.Events[index].Kind == kind)
                {
                    return index;
                }
            }

            return -1;
        }

        private static void AssertPlan(
            EnemyActionPlan plan,
            CombatEffectKind expectedKind,
            int expectedAmount)
        {
            IReadOnlyList<CombatEffect> effects = plan.Effects;
            Assert.That(effects.Count, Is.EqualTo(1));
            Assert.That(effects[0].Kind, Is.EqualTo(expectedKind));
            Assert.That(effects[0].Amount, Is.EqualTo(expectedAmount));
        }

        private static CombatParticipant Player(int maxHp)
        {
            return new CombatParticipant(maxHp, maxHp, shield: 0, new CombatParticipantId(1), CombatTeam.Player);
        }

        private static CombatParticipant Enemy(int id, int maxHp)
        {
            return new CombatParticipant(maxHp, maxHp, shield: 0, new CombatParticipantId(id), CombatTeam.Enemy);
        }
    }
}
