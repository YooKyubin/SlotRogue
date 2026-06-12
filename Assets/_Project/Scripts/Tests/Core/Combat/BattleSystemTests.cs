using System.Linq;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class BattleSystemTests
    {
        private BattleSystem _battle = null!;

        private CombatParticipant FirstEnemy => _battle.Enemies[0];

        [SetUp]
        public void SetUp()
        {
            _battle = new BattleSystem();
        }

        [Test]
        public void StartBattle_SetsPlayerTurn()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 10);
            var enemyActions = new[] { new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy) };

            _battle.StartBattle(player, monster, enemyActions);

            Assert.That(_battle.CurrentPhase, Is.EqualTo(BattlePhase.PlayerTurn));
            Assert.That(_battle.CanApplyPlayerTurn, Is.True);
            Assert.That(
                _battle.TryGetUpcomingEnemyTurn(monster.Id, out EnemyUpcomingTurn upcomingTurn),
                Is.True);
            Assert.That(upcomingTurn.ParticipantId.Value, Is.EqualTo(monster.Id.Value));
            Assert.That(upcomingTurn.Actions, Is.EqualTo(enemyActions));
            Assert.That(upcomingTurn.TurnIndex, Is.Zero);
        }

        [Test]
        public void ApplyPlayerTurn_RejectsWhenNotPlayerTurn()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 5);
            _battle.StartBattle(player, monster, System.Array.Empty<CombatEffect>());

            BattleApplyResult winResult = _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy),
            });

            Assert.That(_battle.CurrentPhase, Is.EqualTo(BattlePhase.Ended));

            BattleApplyResult result = _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Phase, Is.EqualTo(BattlePhase.Ended));
        }

        [Test]
        public void ApplyPlayerTurn_RejectsWhenNotInBattle()
        {
            BattleApplyResult result = _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Phase, Is.EqualTo(BattlePhase.NotInBattle));
        }

        [Test]
        public void ApplyPlayerTurn_MonsterDies_SkipsEnemyTurnAndEndsVictory()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 5);
            var enemyActions = new[] { new CombatEffect(CombatEffectKind.Damage, 99, CombatEffectTarget.Enemy) };
            _battle.StartBattle(player, monster, enemyActions);

            BattleApplyResult result = _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy),
            });

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Phase, Is.EqualTo(BattlePhase.Ended));
            Assert.That(result.EndReason, Is.EqualTo(BattleEndReason.Victory));
            Assert.That(_battle.Player.CurrentHp, Is.EqualTo(30));
        }

        [Test]
        public void ApplyPlayerTurn_PlayerDiesDuringPlayerTurn_SkipsEnemyTurn()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30, currentHp: 1);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 50);
            var enemyActions = new[] { new CombatEffect(CombatEffectKind.Damage, 99, CombatEffectTarget.Enemy) };
            _battle.StartBattle(player, monster, enemyActions);

            BattleApplyResult result = _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 1, CombatEffectTarget.Self),
            });

            Assert.That(result.Phase, Is.EqualTo(BattlePhase.Ended));
            Assert.That(result.EndReason, Is.EqualTo(BattleEndReason.Defeat));
            Assert.That(FirstEnemy.CurrentHp, Is.EqualTo(50));
        }

        [Test]
        public void ApplyPlayerTurn_PlayerAndEnemyAlreadyDead_EndsDefeat()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30, currentHp: 0);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 10, currentHp: 0);
            _battle.StartBattle(player, monster, System.Array.Empty<CombatEffect>());

            BattleApplyResult result = _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            Assert.That(result.Phase, Is.EqualTo(BattlePhase.Ended));
            Assert.That(result.EndReason, Is.EqualTo(BattleEndReason.Defeat));
            Assert.That(_battle.EndReason, Is.EqualTo(BattleEndReason.Defeat));
        }

        [Test]
        public void ApplyPlayerTurn_PlayerActionSkipped_StillEndsTurnResetsEnemyShieldAndRunsEnemyTurn()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 20);
            var schedule = new MonsterTurnSchedule(
                new[] { new CombatEffect(CombatEffectKind.Shield, 4, CombatEffectTarget.Self) },
                new[] { new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy) });
            _battle.StartBattle(player, monster, schedule);

            _battle.ApplyPlayerTurn(new[]
            {
                CombatEffect.ApplyStatus(
                    new StatusEffectSpec(StatusEffectKind.Freeze, duration: 2, magnitude: 0, StatusStackMode.Refresh),
                    CombatEffectTarget.Self),
            });
            Assert.That(FirstEnemy.Shield, Is.EqualTo(4));

            BattleApplyResult result = _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 99, CombatEffectTarget.Enemy),
            });

            Assert.That(result.Phase, Is.EqualTo(BattlePhase.PlayerTurn));
            Assert.That(FirstEnemy.CurrentHp, Is.EqualTo(20));
            Assert.That(FirstEnemy.Shield, Is.Zero);
            Assert.That(_battle.Player.CurrentHp, Is.EqualTo(27));
            Assert.That(_battle.Events, Has.Some.Matches<CombatEvent>(e =>
                e.Kind == CombatEventKind.ActionSkipped &&
                e.StatusEffectKind == StatusEffectKind.Freeze &&
                e.IsPlayerParticipant));
            Assert.That(_battle.Events.Count(e =>
                e.Kind == CombatEventKind.StatusExpired &&
                e.StatusEffectKind == StatusEffectKind.Freeze &&
                e.IsPlayerParticipant), Is.EqualTo(1));
        }

        [Test]
        public void ApplyPlayerTurn_ResetsMonsterShieldAfterPlayerEffects()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 20, shield: 4);
            var enemyActions = System.Array.Empty<CombatEffect>();
            _battle.StartBattle(player, monster, enemyActions);

            _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy),
            });

            Assert.That(FirstEnemy.Shield, Is.Zero);
            Assert.That(_battle.CurrentPhase, Is.EqualTo(BattlePhase.PlayerTurn));
        }

        [Test]
        public void ApplyPlayerTurn_ShieldResetEvent_IncludesTargetSnapshots()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 20, shield: 4);
            _battle.StartBattle(player, monster, System.Array.Empty<CombatEffect>());

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            CombatEvent shieldResetEvent = _battle.Events.First(e =>
                e.Kind == CombatEventKind.ShieldReset &&
                !e.IsPlayerParticipant);

            Assert.That(shieldResetEvent.TargetBefore.Hp, Is.EqualTo(20));
            Assert.That(shieldResetEvent.TargetBefore.Shield, Is.EqualTo(4));
            Assert.That(shieldResetEvent.TargetAfter.Hp, Is.EqualTo(20));
            Assert.That(shieldResetEvent.TargetAfter.Shield, Is.Zero);
        }

        [Test]
        public void ApplyPlayerTurn_ResetsPlayerShieldAfterEnemyTurn()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 20);
            var enemyActions = new[]
            {
                new CombatEffect(CombatEffectKind.Shield, 3, CombatEffectTarget.Self),
            };
            _battle.StartBattle(player, monster, enemyActions);

            _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Shield, 5, CombatEffectTarget.Self),
            });

            Assert.That(_battle.Player.Shield, Is.Zero);
            Assert.That(_battle.CurrentPhase, Is.EqualTo(BattlePhase.PlayerTurn));
        }

        [Test]
        public void ApplyPlayerTurn_RunsEnemyTurnAndReturnsToPlayerTurn()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 20);
            var enemyActions = new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy),
            };
            _battle.StartBattle(player, monster, enemyActions);

            BattleApplyResult result = _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy),
            });

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Phase, Is.EqualTo(BattlePhase.PlayerTurn));
            Assert.That(FirstEnemy.CurrentHp, Is.EqualTo(17));
            Assert.That(_battle.Player.CurrentHp, Is.EqualTo(26));
        }

        [Test]
        public void ApplyPlayerTurn_MonsterShieldBlocksDuringPlayerTurnOnly()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 20, shield: 3);
            var enemyActions = System.Array.Empty<CombatEffect>();
            _battle.StartBattle(player, monster, enemyActions);

            _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy),
            });

            Assert.That(FirstEnemy.CurrentHp, Is.EqualTo(18));
            Assert.That(FirstEnemy.Shield, Is.Zero);
        }

        [Test]
        public void ApplyPlayerTurn_EmitsPhaseAndEffectEvents()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 10);
            var enemyActions = System.Array.Empty<CombatEffect>();
            _battle.StartBattle(player, monster, enemyActions);

            _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 10, CombatEffectTarget.Enemy),
            });

            Assert.That(_battle.Events, Has.Some.Matches<CombatEvent>(e => e.Kind == CombatEventKind.PhaseChanged));
            Assert.That(_battle.Events, Has.Some.Matches<CombatEvent>(e => e.Kind == CombatEventKind.EffectApplied));
            Assert.That(_battle.Events, Has.Some.Matches<CombatEvent>(e =>
                e.Kind == CombatEventKind.BattleEnded && e.EndReason == BattleEndReason.Victory));
        }

        [Test]
        public void ApplyPlayerTurn_EnemyEffect_EmitsActionCompletedWithSource()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 20);
            var enemyActions = new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy),
            };
            _battle.StartBattle(player, monster, enemyActions);

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            CombatEvent actionCompleted = _battle.Events.First(e =>
                e.Kind == CombatEventKind.ActionCompleted &&
                e.Phase == BattlePhase.EnemyTurn);

            Assert.That(actionCompleted.SourceParticipantId.Value, Is.EqualTo(monster.Id.Value));
            Assert.That(actionCompleted.Effect.Kind, Is.EqualTo(CombatEffectKind.Damage));
            Assert.That(actionCompleted.Effect.Amount, Is.EqualTo(4));
        }

        [Test]
        public void ApplyPlayerTurn_PlayerDiesDuringEnemyTurn_EndsDefeat()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30, currentHp: 3);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 50);
            var enemyActions = new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy),
            };
            _battle.StartBattle(player, monster, enemyActions);

            BattleApplyResult result = _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            Assert.That(result.Phase, Is.EqualTo(BattlePhase.Ended));
            Assert.That(result.EndReason, Is.EqualTo(BattleEndReason.Defeat));
            Assert.That(_battle.Player.CurrentHp, Is.Zero);
        }

        [Test]
        public void ApplyPlayerTurn_MultiTurnCycle_PreservesHpAndPhase()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 20);
            var enemyActions = new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy),
            };
            _battle.StartBattle(player, monster, enemyActions);

            _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Shield, 3, CombatEffectTarget.Self),
                new CombatEffect(CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy),
            });

            Assert.That(_battle.CurrentPhase, Is.EqualTo(BattlePhase.PlayerTurn));
            Assert.That(_battle.Player.CurrentHp, Is.EqualTo(30));
            Assert.That(FirstEnemy.CurrentHp, Is.EqualTo(16));
            Assert.That(_battle.Player.Shield, Is.Zero);

            _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy),
            });

            Assert.That(_battle.CurrentPhase, Is.EqualTo(BattlePhase.PlayerTurn));
            Assert.That(FirstEnemy.CurrentHp, Is.EqualTo(11));
            Assert.That(_battle.Player.CurrentHp, Is.EqualTo(28));
        }

        [Test]
        public void ApplyPlayerTurn_EmitsEventsInExpectedOrder()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 20);
            var enemyActions = new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy),
            };
            _battle.StartBattle(player, monster, enemyActions);

            _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy),
            });

            CombatEvent[] events = _battle.Events.ToArray();
            int resolvingIndex = System.Array.FindIndex(
                events,
                e => e.Kind == CombatEventKind.PhaseChanged && e.Phase == BattlePhase.Resolving);
            int effectIndex = System.Array.FindIndex(events, e => e.Kind == CombatEventKind.EffectApplied);
            int monsterShieldResetIndex = System.Array.FindIndex(
                events,
                e => e.Kind == CombatEventKind.ShieldReset && !e.IsPlayerParticipant);
            int enemyTurnIndex = System.Array.FindIndex(
                events,
                e => e.Kind == CombatEventKind.PhaseChanged && e.Phase == BattlePhase.EnemyTurn);
            int playerShieldResetIndex = System.Array.FindIndex(
                events,
                e => e.Kind == CombatEventKind.ShieldReset && e.IsPlayerParticipant);

            Assert.That(resolvingIndex, Is.GreaterThan(0));
            Assert.That(effectIndex, Is.GreaterThan(resolvingIndex));
            Assert.That(monsterShieldResetIndex, Is.GreaterThan(effectIndex));
            Assert.That(enemyTurnIndex, Is.GreaterThan(monsterShieldResetIndex));
            Assert.That(playerShieldResetIndex, Is.GreaterThan(enemyTurnIndex));
            Assert.That(_battle.CurrentPhase, Is.EqualTo(BattlePhase.PlayerTurn));
        }

        [Test]
        public void ApplyPlayerTurn_EffectApplied_DamageSnapshot_ReflectsShieldAndHp()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 20, shield: 3);
            _battle.StartBattle(player, monster, System.Array.Empty<CombatEffect>());

            _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy),
            });

            CombatEvent damageEvent = _battle.Events.First(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage);

            Assert.That(damageEvent.HasTargetSnapshot, Is.True);
            Assert.That(damageEvent.IsPlayerParticipant, Is.False);
            Assert.That(damageEvent.TargetBefore.Hp, Is.EqualTo(20));
            Assert.That(damageEvent.TargetBefore.Shield, Is.EqualTo(3));
            Assert.That(damageEvent.TargetAfter.Hp, Is.EqualTo(18));
            Assert.That(damageEvent.TargetAfter.Shield, Is.Zero);
            Assert.That(damageEvent.ApplyResult.DamageDealt, Is.EqualTo(2));
            Assert.That(damageEvent.ApplyResult.ShieldConsumed, Is.EqualTo(3));
        }

        [Test]
        public void ApplyPlayerTurn_EffectApplied_HealSnapshot_ReflectsCappedHeal()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30, currentHp: 25);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 20);
            _battle.StartBattle(player, monster, System.Array.Empty<CombatEffect>());

            _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Heal, 10, CombatEffectTarget.Self),
            });

            CombatEvent healEvent = _battle.Events.First(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Heal);

            Assert.That(healEvent.TargetBefore.Hp, Is.EqualTo(25));
            Assert.That(healEvent.TargetAfter.Hp, Is.EqualTo(30));
            Assert.That(healEvent.ApplyResult.HealApplied, Is.EqualTo(5));
        }

        [Test]
        public void ApplyPlayerTurn_CyclesMonsterTurnSchedule()
        {
            CombatParticipant player = CombatParticipantFactory.CreatePlayer(maxHp: 30);
            CombatParticipant monster = CombatParticipantFactory.CreateEnemy(maxHp: 50);
            var schedule = new MonsterTurnSchedule(
                new[] { new CombatEffect(CombatEffectKind.Damage, 1, CombatEffectTarget.Enemy) },
                new[] { new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy) },
                new[] { new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy) });
            _battle.StartBattle(player, monster, schedule);

            Assert.That(_battle.TryGetUpcomingEnemyTurn(monster.Id, out EnemyUpcomingTurn upcomingTurn), Is.True);
            Assert.That(upcomingTurn.TurnIndex, Is.Zero);

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());
            Assert.That(_battle.Player.CurrentHp, Is.EqualTo(29));
            Assert.That(_battle.TryGetUpcomingEnemyTurn(monster.Id, out upcomingTurn), Is.True);
            Assert.That(upcomingTurn.TurnIndex, Is.EqualTo(1));

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());
            Assert.That(_battle.Player.CurrentHp, Is.EqualTo(27));
            Assert.That(_battle.TryGetUpcomingEnemyTurn(monster.Id, out upcomingTurn), Is.True);
            Assert.That(upcomingTurn.TurnIndex, Is.EqualTo(2));

            _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());
            Assert.That(_battle.Player.CurrentHp, Is.EqualTo(24));
            Assert.That(_battle.TryGetUpcomingEnemyTurn(monster.Id, out upcomingTurn), Is.True);
            Assert.That(upcomingTurn.TurnIndex, Is.Zero);
        }
    }
}
