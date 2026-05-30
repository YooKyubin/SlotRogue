using System.Linq;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class BattleSystemTests
    {
        private BattleSystem _battle = null!;

        [SetUp]
        public void SetUp()
        {
            _battle = new BattleSystem();
        }

        [Test]
        public void StartBattle_SetsPlayerTurn()
        {
            var player = new CombatParticipant(maxHp: 30);
            var monster = new CombatParticipant(maxHp: 10);
            var enemyActions = new[] { new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy) };

            _battle.StartBattle(player, monster, enemyActions);

            Assert.That(_battle.CurrentPhase, Is.EqualTo(BattlePhase.PlayerTurn));
            Assert.That(_battle.CanApplyPlayerTurn, Is.True);
            Assert.That(_battle.UpcomingEnemyActions, Is.SameAs(enemyActions));
        }

        [Test]
        public void ApplyPlayerTurn_RejectsWhenNotPlayerTurn()
        {
            var player = new CombatParticipant(maxHp: 30);
            var monster = new CombatParticipant(maxHp: 5);
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
            var player = new CombatParticipant(maxHp: 30);
            var monster = new CombatParticipant(maxHp: 5);
            var enemyActions = new[] { new CombatEffect(CombatEffectKind.Damage, 99, CombatEffectTarget.Enemy) };
            _battle.StartBattle(player, monster, enemyActions);

            BattleApplyResult result = _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy),
            });

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Phase, Is.EqualTo(BattlePhase.Ended));
            Assert.That(result.EndReason, Is.EqualTo(BattleEndReason.Victory));
            Assert.That(player.CurrentHp, Is.EqualTo(30));
        }

        [Test]
        public void ApplyPlayerTurn_PlayerDiesDuringPlayerTurn_SkipsEnemyTurn()
        {
            var player = new CombatParticipant(maxHp: 30, currentHp: 1);
            var monster = new CombatParticipant(maxHp: 50);
            var enemyActions = new[] { new CombatEffect(CombatEffectKind.Damage, 99, CombatEffectTarget.Enemy) };
            _battle.StartBattle(player, monster, enemyActions);

            BattleApplyResult result = _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 1, CombatEffectTarget.Self),
            });

            Assert.That(result.Phase, Is.EqualTo(BattlePhase.Ended));
            Assert.That(result.EndReason, Is.EqualTo(BattleEndReason.Defeat));
            Assert.That(monster.CurrentHp, Is.EqualTo(50));
        }

        [Test]
        public void ApplyPlayerTurn_ResetsMonsterShieldAfterPlayerEffects()
        {
            var player = new CombatParticipant(maxHp: 30);
            var monster = new CombatParticipant(maxHp: 20, shield: 4);
            var enemyActions = System.Array.Empty<CombatEffect>();
            _battle.StartBattle(player, monster, enemyActions);

            _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy),
            });

            Assert.That(monster.Shield, Is.Zero);
            Assert.That(_battle.CurrentPhase, Is.EqualTo(BattlePhase.PlayerTurn));
        }

        [Test]
        public void ApplyPlayerTurn_ResetsPlayerShieldAfterEnemyTurn()
        {
            var player = new CombatParticipant(maxHp: 30);
            var monster = new CombatParticipant(maxHp: 20);
            var enemyActions = new[]
            {
                new CombatEffect(CombatEffectKind.Shield, 3, CombatEffectTarget.Self),
            };
            _battle.StartBattle(player, monster, enemyActions);

            _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Shield, 5, CombatEffectTarget.Self),
            });

            Assert.That(player.Shield, Is.Zero);
            Assert.That(_battle.CurrentPhase, Is.EqualTo(BattlePhase.PlayerTurn));
        }

        [Test]
        public void ApplyPlayerTurn_RunsEnemyTurnAndReturnsToPlayerTurn()
        {
            var player = new CombatParticipant(maxHp: 30);
            var monster = new CombatParticipant(maxHp: 20);
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
            Assert.That(monster.CurrentHp, Is.EqualTo(17));
            Assert.That(player.CurrentHp, Is.EqualTo(26));
        }

        [Test]
        public void ApplyPlayerTurn_MonsterShieldBlocksDuringPlayerTurnOnly()
        {
            var player = new CombatParticipant(maxHp: 30);
            var monster = new CombatParticipant(maxHp: 20, shield: 3);
            var enemyActions = System.Array.Empty<CombatEffect>();
            _battle.StartBattle(player, monster, enemyActions);

            _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy),
            });

            Assert.That(monster.CurrentHp, Is.EqualTo(18));
            Assert.That(monster.Shield, Is.Zero);
        }

        [Test]
        public void ApplyPlayerTurn_EmitsPhaseAndEffectEvents()
        {
            var player = new CombatParticipant(maxHp: 30);
            var monster = new CombatParticipant(maxHp: 10);
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
        public void ApplyPlayerTurn_PlayerDiesDuringEnemyTurn_EndsDefeat()
        {
            var player = new CombatParticipant(maxHp: 30, currentHp: 3);
            var monster = new CombatParticipant(maxHp: 50);
            var enemyActions = new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 4, CombatEffectTarget.Enemy),
            };
            _battle.StartBattle(player, monster, enemyActions);

            BattleApplyResult result = _battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            Assert.That(result.Phase, Is.EqualTo(BattlePhase.Ended));
            Assert.That(result.EndReason, Is.EqualTo(BattleEndReason.Defeat));
            Assert.That(player.CurrentHp, Is.Zero);
        }

        [Test]
        public void ApplyPlayerTurn_MultiTurnCycle_PreservesHpAndPhase()
        {
            var player = new CombatParticipant(maxHp: 30);
            var monster = new CombatParticipant(maxHp: 20);
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
            Assert.That(player.CurrentHp, Is.EqualTo(30));
            Assert.That(monster.CurrentHp, Is.EqualTo(16));
            Assert.That(player.Shield, Is.Zero);

            _battle.ApplyPlayerTurn(new[]
            {
                new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy),
            });

            Assert.That(_battle.CurrentPhase, Is.EqualTo(BattlePhase.PlayerTurn));
            Assert.That(monster.CurrentHp, Is.EqualTo(11));
            Assert.That(player.CurrentHp, Is.EqualTo(28));
        }

        [Test]
        public void ApplyPlayerTurn_EmitsEventsInExpectedOrder()
        {
            var player = new CombatParticipant(maxHp: 30);
            var monster = new CombatParticipant(maxHp: 20);
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
    }
}
