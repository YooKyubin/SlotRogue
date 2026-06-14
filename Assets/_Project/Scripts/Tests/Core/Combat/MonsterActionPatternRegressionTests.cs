using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class MonsterActionPatternRegressionTests
    {
        private BattleSystem _battle = null!;

        [SetUp]
        public void SetUp()
        {
            _battle = new BattleSystem();
        }

        [Test]
        public void MonsterTurnSchedule_ConsumesPatternInOrderAndRepeats()
        {
            var schedule = new MonsterTurnSchedule(
                Turn(CombatEffectKind.Damage, 1, CombatEffectTarget.Enemy),
                Turn(CombatEffectKind.Shield, 2, CombatEffectTarget.Self),
                Turn(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy));

            AssertUpcoming(schedule, turnIndex: 0, CombatEffectKind.Damage, amount: 1);
            AssertTurn(schedule.ConsumeUpcomingTurn(), CombatEffectKind.Damage, amount: 1);
            AssertUpcoming(schedule, turnIndex: 1, CombatEffectKind.Shield, amount: 2);

            AssertTurn(schedule.ConsumeUpcomingTurn(), CombatEffectKind.Shield, amount: 2);
            AssertUpcoming(schedule, turnIndex: 2, CombatEffectKind.Damage, amount: 3);

            AssertTurn(schedule.ConsumeUpcomingTurn(), CombatEffectKind.Damage, amount: 3);
            AssertUpcoming(schedule, turnIndex: 0, CombatEffectKind.Damage, amount: 1);
        }

        [Test]
        public void StartBattle_InitialUpcomingEnemyTurn_IsFirstPatternAction()
        {
            CombatParticipant player = CreatePlayer(maxHp: 50);
            CombatParticipant enemy = CreateEnemy(id: 100, maxHp: 30);
            var schedule = new MonsterTurnSchedule(
                Turn(CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy),
                Turn(CombatEffectKind.Shield, 6, CombatEffectTarget.Self));

            _battle.StartBattle(player, enemy, schedule);

            Assert.That(_battle.TryGetUpcomingEnemyTurn(enemy.Id, out EnemyUpcomingTurn upcomingTurn), Is.True);
            Assert.That(upcomingTurn.ParticipantId.Value, Is.EqualTo(100));
            AssertTurn(upcomingTurn.Plan.Effects, CombatEffectKind.Damage, amount: 4);
        }

        [Test]
        public void ApplyPlayerTurn_AfterEnemyAction_AdvancesToNextPatternAction()
        {
            CombatParticipant player = CreatePlayer(maxHp: 50);
            CombatParticipant enemy = CreateEnemy(id: 100, maxHp: 30);
            var schedule = new MonsterTurnSchedule(
                Turn(CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy),
                Turn(CombatEffectKind.Shield, 6, CombatEffectTarget.Self));
            _battle.StartBattle(player, enemy, schedule);

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            Assert.That(player.CurrentHp, Is.EqualTo(46));
            Assert.That(_battle.TryGetUpcomingEnemyTurn(enemy.Id, out EnemyUpcomingTurn upcomingTurn), Is.True);
            AssertTurn(upcomingTurn.Plan.Effects, CombatEffectKind.Shield, amount: 6);
        }

        [Test]
        public void ApplyPlayerTurn_ActionSkippedByFreeze_StillAdvancesPattern()
        {
            CombatParticipant player = CreatePlayer(maxHp: 50);
            CombatParticipant enemy = CreateEnemy(id: 100, maxHp: 30);
            var schedule = new MonsterTurnSchedule(
                Turn(CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy),
                Turn(CombatEffectKind.Damage, 7, CombatEffectTarget.Enemy));
            _battle.StartBattle(player, enemy, schedule);

            _battle.ApplyPlayerTurn(new[]
            {
                CombatEffect.ApplyStatus(
                    new StatusEffectSpec(StatusEffectKind.Freeze, duration: 1, magnitude: 0, StatusStackMode.Refresh),
                    CombatEffectTarget.Enemy),
            });

            Assert.That(player.CurrentHp, Is.EqualTo(50));
            Assert.That(_battle.Events, Has.Some.Matches<CombatEvent>(e =>
                e.Kind == CombatEventKind.ActionSkipped &&
                e.StatusEffectKind == StatusEffectKind.Freeze &&
                !e.IsPlayerParticipant));
            Assert.That(_battle.TryGetUpcomingEnemyTurn(enemy.Id, out EnemyUpcomingTurn upcomingTurn), Is.True);
            AssertTurn(upcomingTurn.Plan.Effects, CombatEffectKind.Damage, amount: 7);

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            Assert.That(player.CurrentHp, Is.EqualTo(43));
        }

        [Test]
        public void ApplyPlayerTurn_MultipleEnemies_ResolveActionsInRosterOrder()
        {
            CombatParticipant player = CreatePlayer(maxHp: 50);
            CombatParticipant enemy0 = CreateEnemy(id: 100, maxHp: 30);
            CombatParticipant enemy1 = CreateEnemy(id: 101, maxHp: 30);
            var schedules = new[]
            {
                new MonsterTurnSchedule(Turn(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy)),
                new MonsterTurnSchedule(Turn(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy)),
            };
            _battle.StartBattle(player, new[] { enemy0, enemy1 }, schedules);
            int eventCursor = _battle.Events.Count;

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            int[] actionSourceIds = EnemyDamageEventsOnPlayerSince(eventCursor)
                .Select(e => e.SourceParticipantId.Value)
                .ToArray();
            int[] actionAmounts = EnemyDamageEventsOnPlayerSince(eventCursor)
                .Select(e => e.Effect.Amount)
                .ToArray();

            Assert.That(actionSourceIds, Is.EqualTo(new[] { 100, 101 }));
            Assert.That(actionAmounts, Is.EqualTo(new[] { 3, 5 }));
            Assert.That(player.CurrentHp, Is.EqualTo(42));
        }

        [Test]
        public void ApplyPlayerTurn_DirectTargetingHitsSelectedEnemyAndDeadTargetRetargets()
        {
            CombatParticipant player = CreatePlayer(maxHp: 50);
            CombatParticipant enemy0 = CreateEnemy(id: 100, maxHp: 5);
            CombatParticipant enemy1 = CreateEnemy(id: 101, maxHp: 20);
            _battle.StartBattle(
                player,
                new[] { enemy0, enemy1 },
                new[] { EmptyEnemySchedule(), EmptyEnemySchedule() });

            _battle.ApplyPlayerTurn(
                new[]
                {
                    DamageSelected(amount: 5, targetId: 100),
                    DamageSelected(amount: 4, targetId: 100),
                },
                selectedTargetId: new CombatParticipantId(100));

            Assert.That(enemy0.IsDead, Is.True);
            Assert.That(enemy1.CurrentHp, Is.EqualTo(16));
            Assert.That(
                PlayerDamageEventsOnEnemies().Select(e => e.TargetParticipantId.Value).ToArray(),
                Is.EqualTo(new[] { 100, 101 }));
        }

        private IEnumerable<CombatEvent> PlayerDamageEventsOnEnemies() =>
            _battle.Events.Where(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage &&
                e.TargetParticipantId.IsValid &&
                e.TargetParticipantId.Value != _battle.Player.Id.Value);

        private IEnumerable<CombatEvent> EnemyDamageEventsOnPlayerSince(int eventCursor) =>
            _battle.Events
                .Skip(eventCursor)
                .Where(e =>
                    e.Kind == CombatEventKind.EffectApplied &&
                    e.Effect.Kind == CombatEffectKind.Damage &&
                    e.IsPlayerParticipant);

        private static void AssertUpcoming(
            MonsterTurnSchedule schedule,
            int turnIndex,
            CombatEffectKind kind,
            int amount)
        {
            Assert.That(schedule.UpcomingTurnIndex, Is.EqualTo(turnIndex));
            AssertTurn(schedule.UpcomingActions, kind, amount);
        }

        private static void AssertTurn(
            IReadOnlyList<CombatEffect> actions,
            CombatEffectKind kind,
            int amount)
        {
            Assert.That(actions.Count, Is.EqualTo(1));
            Assert.That(actions[0].Kind, Is.EqualTo(kind));
            Assert.That(actions[0].Amount, Is.EqualTo(amount));
        }

        private static CombatEffect[] Turn(
            CombatEffectKind kind,
            int amount,
            CombatEffectTarget target) =>
            new[] { new CombatEffect(kind, amount, target) };

        private static CombatEffect DamageSelected(int amount, int targetId) =>
            new(
                CombatEffectKind.Damage,
                amount,
                CombatEffectTarget.SelectedEnemy(new CombatParticipantId(targetId)));

        private static MonsterTurnSchedule EmptyEnemySchedule() =>
            new(System.Array.Empty<CombatEffect>());

        private static CombatParticipant CreatePlayer(int maxHp) =>
            new(maxHp, maxHp, shield: 0, new CombatParticipantId(1), CombatTeam.Player);

        private static CombatParticipant CreateEnemy(int id, int maxHp) =>
            new(maxHp, maxHp, shield: 0, new CombatParticipantId(id), CombatTeam.Enemy);
    }
}
