using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class BattleSystemMultiParticipantTests
    {
        private BattleSystem _battle = null!;

        [SetUp]
        public void SetUp()
        {
            _battle = new BattleSystem();
        }

        [Test]
        public void StartBattle_TwoEnemies_RegistersRosterWithDistinctIds()
        {
            StartTwoEnemyBattle(
                CreatePlayer(),
                CreateEnemy(id: 100, maxHp: 10),
                CreateEnemy(id: 101, maxHp: 20));

            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
            Assert.That(_battle.Enemies[0].Id.Value, Is.EqualTo(100));
            Assert.That(_battle.Enemies[1].Id.Value, Is.EqualTo(101));
        }

        [Test]
        public void ApplyPlayerTurn_SelectedEnemyId_HitsRequestedTarget()
        {
            StartTwoEnemyBattle(
                CreatePlayer(),
                CreateEnemy(id: 100, maxHp: 20),
                CreateEnemy(id: 101, maxHp: 20));

            _battle.ApplyPlayerTurn(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        3,
                        CombatEffectTarget.SelectedEnemy(new CombatParticipantId(101))),
                },
                selectedTargetId: new CombatParticipantId(101));

            Assert.That(EnemyWithId(100).CurrentHp, Is.EqualTo(20));
            Assert.That(EnemyWithId(101).CurrentHp, Is.EqualTo(17));
            CombatEvent hitEvent = _battle.Events.Last(e => e.Kind == CombatEventKind.EffectApplied);
            Assert.That(hitEvent.TargetParticipantId.Value, Is.EqualTo(101));
        }

        [Test]
        public void ApplyPlayerTurn_InvalidSelectedTarget_FallsBackToFirstAliveEnemy()
        {
            StartTwoEnemyBattle(
                CreatePlayer(),
                CreateEnemy(id: 100, maxHp: 5),
                CreateEnemy(id: 101, maxHp: 20));

            _battle.ApplyPlayerTurn(
                new[]
                {
                    new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy),
                },
                selectedTargetId: new CombatParticipantId(100));

            Assert.That(EnemyWithId(100).IsDead, Is.True);
            Assert.That(EnemyWithId(101).CurrentHp, Is.EqualTo(20));

            _battle.ApplyPlayerTurn(
                new[]
                {
                    new CombatEffect(CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy),
                },
                selectedTargetId: new CombatParticipantId(100));

            Assert.That(EnemyWithId(101).CurrentHp, Is.EqualTo(16));
        }

        [Test]
        public void ApplyPlayerTurn_MultiHit_RetargetsToSecondEnemyWhenFirstDiesMidCombo()
        {
            StartTwoEnemyBattle(
                CreatePlayer(),
                CreateEnemy(id: 100, maxHp: 5),
                CreateEnemy(id: 101, maxHp: 20));

            _battle.ApplyPlayerTurn(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        5,
                        CombatEffectTarget.SelectedEnemy(new CombatParticipantId(100))),
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        5,
                        CombatEffectTarget.SelectedEnemy(new CombatParticipantId(100))),
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        5,
                        CombatEffectTarget.SelectedEnemy(new CombatParticipantId(100))),
                },
                selectedTargetId: new CombatParticipantId(100));

            Assert.That(EnemyWithId(100).IsDead, Is.True);
            Assert.That(EnemyWithId(101).CurrentHp, Is.EqualTo(10));

            int[] damageTargets = PlayerDamageEventsOnEnemies()
                .Select(e => e.TargetParticipantId.Value)
                .ToArray();

            Assert.That(damageTargets, Is.EqualTo(new[] { 100, 101, 101 }));
        }

        [Test]
        public void ApplyPlayerTurn_MultiHit_DissipatesRemainingHitsWhenNoAliveEnemies()
        {
            StartSingleEnemyBattle(CreatePlayer(), CreateEnemy(id: 100, maxHp: 5));

            _battle.ApplyPlayerTurn(
                new[]
                {
                    new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy),
                    new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy),
                    new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy),
                });

            Assert.That(_battle.EndReason, Is.EqualTo(BattleEndReason.Victory));
            int damageEventCount = PlayerDamageEventsOnEnemies().Count();
            Assert.That(damageEventCount, Is.EqualTo(1));
        }

        [Test]
        public void ApplyPlayerTurn_AllEnemies_HitsEveryAliveEnemyOnce()
        {
            StartTwoEnemyBattle(
                CreatePlayer(),
                CreateEnemy(id: 100, maxHp: 10),
                CreateEnemy(id: 101, maxHp: 10));

            _battle.ApplyPlayerTurn(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        4,
                        new CombatEffectTarget(CombatTargetMode.AllEnemies)),
                });

            Assert.That(EnemyWithId(100).CurrentHp, Is.EqualTo(6));
            Assert.That(EnemyWithId(101).CurrentHp, Is.EqualTo(6));
        }

        [Test]
        public void ApplyPlayerTurn_EnemyTurn_AppliesLeftToRight()
        {
            CombatParticipant player = CreatePlayer(maxHp: 100);
            CombatParticipant enemy0 = CreateEnemy(id: 100, maxHp: 50);
            CombatParticipant enemy1 = CreateEnemy(id: 101, maxHp: 50);
            var schedules = new[]
            {
                new MonsterTurnSchedule(
                    new[] { new CombatEffect(CombatEffectKind.Damage, 1, CombatEffectTarget.Enemy) }),
                new MonsterTurnSchedule(
                    new[] { new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy) }),
            };

            _battle.StartBattle(player, new[] { enemy0, enemy1 }, schedules);
            int eventCursor = _battle.Events.Count;

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            int[] damageAmounts = EnemyDamageEventsOnPlayerSince(eventCursor)
                .Select(e => e.Effect.Amount)
                .ToArray();

            Assert.That(damageAmounts, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(_battle.Player.CurrentHp, Is.EqualTo(97));
        }

        [Test]
        public void ApplyPlayerTurn_DeadEnemySkipped_KeepsSurvivorScheduleIndex()
        {
            CombatParticipant player = CreatePlayer(maxHp: 200);
            CombatParticipant enemy0 = CreateEnemy(id: 100, maxHp: 5);
            CombatParticipant enemy1 = CreateEnemy(id: 101, maxHp: 50);
            var schedules = new[]
            {
                new MonsterTurnSchedule(
                    new[] { new CombatEffect(CombatEffectKind.Damage, 1, CombatEffectTarget.Enemy) }),
                new MonsterTurnSchedule(
                    new[] { new CombatEffect(CombatEffectKind.Damage, 10, CombatEffectTarget.Enemy) },
                    new[] { new CombatEffect(CombatEffectKind.Damage, 99, CombatEffectTarget.Enemy) }),
            };

            _battle.StartBattle(player, new[] { enemy0, enemy1 }, schedules);

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());
            Assert.That(_battle.Player.CurrentHp, Is.EqualTo(189));
            Assert.That(
                EnemyDamageEventsOnPlayerSince(0).Select(e => e.Effect.Amount).ToArray(),
                Is.EqualTo(new[] { 1, 10 }));

            int eventCursor = _battle.Events.Count;
            _battle.ApplyPlayerTurn(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        99,
                        CombatEffectTarget.SelectedEnemy(new CombatParticipantId(100))),
                },
                selectedTargetId: new CombatParticipantId(100));

            Assert.That(EnemyWithId(100).IsDead, Is.True);
            Assert.That(
                EnemyDamageEventsOnPlayerSince(eventCursor).Select(e => e.Effect.Amount).ToArray(),
                Is.EqualTo(new[] { 99 }));
            Assert.That(_battle.Player.CurrentHp, Is.EqualTo(90));
        }

        [Test]
        public void ApplyPlayerTurn_AllEnemiesDefeated_EndsVictory()
        {
            StartTwoEnemyBattle(
                CreatePlayer(),
                CreateEnemy(id: 100, maxHp: 5),
                CreateEnemy(id: 101, maxHp: 5));

            _battle.ApplyPlayerTurn(
                new[]
                {
                    new CombatEffect(
                        CombatEffectKind.Damage,
                        5,
                        new CombatEffectTarget(CombatTargetMode.AllEnemies)),
                });

            Assert.That(_battle.EndReason, Is.EqualTo(BattleEndReason.Victory));
            Assert.That(EnemyWithId(100).IsDead, Is.True);
            Assert.That(EnemyWithId(101).IsDead, Is.True);
        }

        [Test]
        public void TryGetUpcomingEnemyTurn_ExistingEnemy_ReturnsThatEnemySchedule()
        {
            CombatParticipant player = CreatePlayer();
            CombatParticipant enemy0 = CreateEnemy(id: 100, maxHp: 10);
            CombatParticipant enemy1 = CreateEnemy(id: 101, maxHp: 10);
            var enemy0Actions = new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 1, CombatEffectTarget.Enemy),
            };
            var enemy1Actions = new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy),
            };

            _battle.StartBattle(
                player,
                new[] { enemy0, enemy1 },
                new[]
                {
                    new MonsterTurnSchedule(enemy0Actions),
                    new MonsterTurnSchedule(
                        new[] { new CombatEffect(CombatEffectKind.Shield, 3, CombatEffectTarget.Self) },
                        enemy1Actions),
                });
            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            Assert.That(_battle.TryGetUpcomingEnemyTurn(enemy1.Id, out EnemyUpcomingTurn upcomingTurn), Is.True);
            Assert.That(upcomingTurn.ParticipantId.Value, Is.EqualTo(enemy1.Id.Value));
            Assert.That(upcomingTurn.Plan.Effects, Is.EqualTo(enemy1Actions));
        }

        [Test]
        public void TryGetUpcomingEnemyTurn_UnknownParticipantId_ReturnsFalse()
        {
            StartSingleEnemyBattle(CreatePlayer(), CreateEnemy(id: 100, maxHp: 10));

            bool found = _battle.TryGetUpcomingEnemyTurn(
                new CombatParticipantId(999),
                out EnemyUpcomingTurn upcomingTurn);

            Assert.That(found, Is.False);
            Assert.That(upcomingTurn, Is.EqualTo(default(EnemyUpcomingTurn)));
        }

        [Test]
        public void TryGetUpcomingEnemyTurn_DeadEnemy_ReturnsFalse()
        {
            CombatParticipant enemy = CreateEnemy(id: 100, maxHp: 5);
            StartSingleEnemyBattle(CreatePlayer(), enemy);

            _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy),
            });

            bool found = _battle.TryGetUpcomingEnemyTurn(enemy.Id, out EnemyUpcomingTurn upcomingTurn);

            Assert.That(found, Is.False);
            Assert.That(upcomingTurn, Is.EqualTo(default(EnemyUpcomingTurn)));
        }

        [Test]
        public void TryGetUpcomingEnemyTurn_MultipleEffects_ReturnsOriginalActionList()
        {
            CombatParticipant enemy = CreateEnemy(id: 100, maxHp: 10);
            var actions = new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy),
                new CombatEffect(CombatEffectKind.Shield, 4, CombatEffectTarget.Self),
            };
            StartSingleEnemyBattle(CreatePlayer(), enemy, new MonsterTurnSchedule(actions));

            Assert.That(
                _battle.TryGetUpcomingEnemyTurn(enemy.Id, out EnemyUpcomingTurn upcomingTurn),
                Is.True);
            Assert.That(upcomingTurn.Plan.Effects, Is.EqualTo(actions));
            Assert.That(upcomingTurn.Plan.Effects.Count, Is.EqualTo(2));
            Assert.That(upcomingTurn.Plan.Effects[0].Kind, Is.EqualTo(CombatEffectKind.Damage));
            Assert.That(upcomingTurn.Plan.Effects[1].Kind, Is.EqualTo(CombatEffectKind.Shield));
        }

        private CombatParticipant EnemyWithId(int id) =>
            _battle.Enemies.First(enemy => enemy.Id.Value == id);

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

        private void StartTwoEnemyBattle(
            CombatParticipant player,
            CombatParticipant enemy0,
            CombatParticipant enemy1)
        {
            _battle.StartBattle(
                player,
                new[] { enemy0, enemy1 },
                new[] { EmptyEnemySchedule(), EmptyEnemySchedule() });
        }

        private void StartSingleEnemyBattle(CombatParticipant player, CombatParticipant enemy)
        {
            StartSingleEnemyBattle(player, enemy, EmptyEnemySchedule());
        }

        private void StartSingleEnemyBattle(
            CombatParticipant player,
            CombatParticipant enemy,
            MonsterTurnSchedule schedule)
        {
            _battle.StartBattle(player, enemy, schedule);
        }

        private static MonsterTurnSchedule EmptyEnemySchedule() =>
            new(System.Array.Empty<CombatEffect>());

        private static CombatParticipant CreatePlayer(int maxHp = 30) =>
            new(maxHp, maxHp, shield: 0, new CombatParticipantId(1), CombatTeam.Player);

        private static CombatParticipant CreateEnemy(int id, int maxHp) =>
            new(maxHp, maxHp, shield: 0, new CombatParticipantId(id), CombatTeam.Enemy);
    }
}
