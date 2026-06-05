using System.Linq;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class BattleSystemStatusEffectTests
    {
        private BattleSystem _battle = null!;

        private CombatParticipant FirstEnemy => _battle.Enemies[0];

        [SetUp]
        public void SetUp()
        {
            _battle = new BattleSystem();
        }

        [Test]
        public void ApplyPlayerTurn_BurnTicksOnEnemyTurnStart()
        {
            var player = new CombatParticipant(maxHp: 30);
            var monster = new CombatParticipant(maxHp: 20);
            _battle.StartBattle(player, monster, System.Array.Empty<CombatEffect>());

            _battle.ApplyPlayerTurn(new[]
            {
                CombatEffect.ApplyStatus(
                    new StatusEffectSpec(StatusEffectKind.Burn, duration: 3, magnitude: 2, StatusStackMode.Refresh),
                    CombatEffectTarget.Enemy),
            });

            CombatParticipant battleMonster = FirstEnemy;
            Assert.That(battleMonster.CurrentHp, Is.EqualTo(18));
            Assert.That(battleMonster.StatusEffects.Single().RemainingTurns, Is.EqualTo(2));
            Assert.That(_battle.Events, Has.Some.Matches<CombatEvent>(e =>
                e.Kind == CombatEventKind.StatusTicked &&
                e.StatusEffectKind == StatusEffectKind.Burn &&
                e.ApplyResult.DamageDealt == 2));
        }

        [Test]
        public void ApplyPlayerTurn_FreezeSkipsEnemyActionAndExpires()
        {
            var player = new CombatParticipant(maxHp: 30);
            var monster = new CombatParticipant(maxHp: 20);
            var enemyActions = new[] { new CombatEffect(CombatEffectKind.Damage, 5, CombatEffectTarget.Enemy) };
            _battle.StartBattle(player, monster, enemyActions);

            _battle.ApplyPlayerTurn(new[]
            {
                CombatEffect.ApplyStatus(
                    new StatusEffectSpec(StatusEffectKind.Freeze, duration: 1, magnitude: 0, StatusStackMode.Refresh),
                    CombatEffectTarget.Enemy),
            });

            Assert.That(_battle.Player.CurrentHp, Is.EqualTo(30));
            Assert.That(FirstEnemy.StatusEffects, Is.Empty);
            Assert.That(_battle.Events, Has.Some.Matches<CombatEvent>(e =>
                e.Kind == CombatEventKind.ActionSkipped &&
                e.StatusEffectKind == StatusEffectKind.Freeze));
            Assert.That(_battle.Events, Has.Some.Matches<CombatEvent>(e =>
                e.Kind == CombatEventKind.StatusExpired &&
                e.StatusEffectKind == StatusEffectKind.Freeze));
        }

        [Test]
        public void ApplyPlayerTurn_PoisonStacksAndCapsAtFive()
        {
            var player = new CombatParticipant(maxHp: 30);
            var monster = new CombatParticipant(maxHp: 30);
            _battle.StartBattle(player, monster, System.Array.Empty<CombatEffect>());

            _battle.ApplyPlayerTurn(new[]
            {
                CombatEffect.ApplyStatus(
                    new StatusEffectSpec(StatusEffectKind.Poison, duration: 0, magnitude: 3, StatusStackMode.Stack),
                    CombatEffectTarget.Enemy),
                CombatEffect.ApplyStatus(
                    new StatusEffectSpec(StatusEffectKind.Poison, duration: 0, magnitude: 4, StatusStackMode.Stack),
                    CombatEffectTarget.Enemy),
            });

            CombatParticipant battleMonster = FirstEnemy;
            StatusEffectInstance poison = battleMonster.StatusEffects.Single();
            Assert.That(poison.Kind, Is.EqualTo(StatusEffectKind.Poison));
            Assert.That(poison.StackCount, Is.EqualTo(5));
            Assert.That(battleMonster.CurrentHp, Is.EqualTo(25));
            Assert.That(_battle.Events.Count(e =>
                e.Kind == CombatEventKind.StatusApplied &&
                e.StatusEffectKind == StatusEffectKind.Poison), Is.EqualTo(2));
        }
    }
}
