using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class LifestealTests
    {
        private EffectApplicator _effectApplicator = null!;
        private StatusEffectEngine _statusEffectEngine = null!;
        private CombatActionResolver _resolver = null!;

        [SetUp]
        public void SetUp()
        {
            _effectApplicator = new EffectApplicator();
            _statusEffectEngine = new StatusEffectEngine(_effectApplicator);
            _resolver = new CombatActionResolver(_effectApplicator, _statusEffectEngine);
        }

        [TestCase(10, 2)]
        [TestCase(6, 2)]
        [TestCase(1, 1)]
        public void ResolvePlayerEffects_Lifesteal_HealsFromActualHealthDamage(
            int healthDamage,
            int expectedHeal)
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 100, currentHp: 50);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 100);
            StatusEffectInstance lifesteal = ApplyLifesteal(player, stackCount: 2);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                events,
                new CombatEffect(
                    CombatEffectKind.Damage,
                    healthDamage,
                    CombatEffectTarget.SelectedEnemy(enemy.Id)));

            Assert.That(player.CurrentHp, Is.EqualTo(50 + expectedHeal));
            Assert.That(lifesteal.StackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_Lifesteal_UsesHealthDamageAfterShield()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 100, currentHp: 50);
            CombatParticipant enemy = Participant(
                100,
                CombatTeam.Enemy,
                maxHp: 100,
                shield: 4);
            ApplyLifesteal(player, stackCount: 2);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                events,
                new CombatEffect(
                    CombatEffectKind.Damage,
                    10,
                    CombatEffectTarget.SelectedEnemy(enemy.Id)));

            Assert.That(enemy.CurrentHp, Is.EqualTo(94));
            Assert.That(player.CurrentHp, Is.EqualTo(52));
        }

        [Test]
        public void ResolvePlayerEffects_Lifesteal_FullyBlockedDamageDoesNotHealOrConsume()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 100, currentHp: 50);
            CombatParticipant enemy = Participant(
                100,
                CombatTeam.Enemy,
                maxHp: 100,
                shield: 10);
            StatusEffectInstance lifesteal = ApplyLifesteal(player, stackCount: 2);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                events,
                new CombatEffect(
                    CombatEffectKind.Damage,
                    10,
                    CombatEffectTarget.SelectedEnemy(enemy.Id)));

            Assert.That(player.CurrentHp, Is.EqualTo(50));
            Assert.That(lifesteal.StackCount, Is.EqualTo(2));
            Assert.That(HealEvents(events), Is.Empty);
        }

        [Test]
        public void ResolvePlayerEffects_Lifesteal_NoValidTargetDoesNotHealOrConsume()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 100, currentHp: 50);
            StatusEffectInstance lifesteal = ApplyLifesteal(player, stackCount: 2);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                Array.Empty<CombatParticipant>(),
                events,
                new CombatEffect(
                    CombatEffectKind.Damage,
                    10,
                    CombatEffectTarget.Enemy));

            Assert.That(player.CurrentHp, Is.EqualTo(50));
            Assert.That(lifesteal.StackCount, Is.EqualTo(2));
            Assert.That(HealEvents(events), Is.Empty);
        }

        [Test]
        public void ResolvePlayerEffects_Lifesteal_MultiHitHealsEachHitAndConsumesOnce()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 100, currentHp: 50);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 100);
            StatusEffectInstance lifesteal = ApplyLifesteal(player, stackCount: 2);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                events,
                new CombatEffect(
                    CombatEffectKind.Damage,
                    1,
                    CombatEffectTarget.SelectedEnemy(enemy.Id)),
                new CombatEffect(
                    CombatEffectKind.Damage,
                    6,
                    CombatEffectTarget.SelectedEnemy(enemy.Id)));

            Assert.That(player.CurrentHp, Is.EqualTo(53));
            Assert.That(HealEvents(events).Select(e => e.ApplyResult.HealApplied), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(lifesteal.StackCount, Is.EqualTo(1));
            CombatEvent statusValueChanged = events.Single(
                combatEvent => combatEvent.Kind == CombatEventKind.StatusValueChanged);
            Assert.That(statusValueChanged.TargetParticipantId.Value, Is.EqualTo(player.Id.Value));
            Assert.That(statusValueChanged.StatusEffectKind, Is.EqualTo(StatusEffectKind.Lifesteal));
            Assert.That(statusValueChanged.StatusStackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_Lifesteal_MultiTargetHealsForEachTarget()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 100, currentHp: 50);
            CombatParticipant enemy1 = Participant(100, CombatTeam.Enemy, maxHp: 100);
            CombatParticipant enemy2 = Participant(101, CombatTeam.Enemy, maxHp: 100);
            StatusEffectInstance lifesteal = ApplyLifesteal(player, stackCount: 2);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy1, enemy2 },
                events,
                new CombatEffect(
                    CombatEffectKind.Damage,
                    6,
                    new CombatEffectTarget(CombatTargetMode.AllEnemies)));

            Assert.That(player.CurrentHp, Is.EqualTo(54));
            Assert.That(HealEvents(events).Count, Is.EqualTo(2));
            Assert.That(lifesteal.StackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_Lifesteal_LethalHitHealsBeforeBattleEndCheck()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 100, currentHp: 50);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 3);
            ApplyLifesteal(player, stackCount: 1);
            var events = new List<CombatEvent>();
            int hpObservedByBattleEndCheck = 0;

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                events,
                () =>
                {
                    hpObservedByBattleEndCheck = player.CurrentHp;
                    return enemy.IsDead;
                },
                new CombatEffect(
                    CombatEffectKind.Damage,
                    10,
                    CombatEffectTarget.SelectedEnemy(enemy.Id)));

            Assert.That(enemy.IsDead, Is.True);
            Assert.That(player.CurrentHp, Is.EqualTo(51));
            Assert.That(hpObservedByBattleEndCheck, Is.EqualTo(51));
        }

        [TestCase(DamageOrigin.Status)]
        [TestCase(DamageOrigin.Reflection)]
        public void ResolvePlayerEffects_Lifesteal_NonDirectDamageDoesNotHealOrConsume(
            DamageOrigin damageOrigin)
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 100, currentHp: 50);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 100);
            StatusEffectInstance lifesteal = ApplyLifesteal(player, stackCount: 2);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                events,
                new CombatEffect(
                    CombatEffectKind.Damage,
                    10,
                    CombatEffectTarget.SelectedEnemy(enemy.Id),
                    damageOrigin));

            Assert.That(player.CurrentHp, Is.EqualTo(50));
            Assert.That(lifesteal.StackCount, Is.EqualTo(2));
        }

        [Test]
        public void ResolvePlayerEffects_LifestealAppliedDuringAction_StartsNextAction()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 100, currentHp: 50);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 100);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                events,
                CombatEffect.ApplyStatus(
                    new StatusEffectSpec(
                        StatusEffectKind.Lifesteal,
                        duration: 0,
                        magnitude: 1,
                        StatusStackMode.Refresh),
                    CombatEffectTarget.Self),
                new CombatEffect(
                    CombatEffectKind.Damage,
                    10,
                    CombatEffectTarget.SelectedEnemy(enemy.Id)));

            Assert.That(player.CurrentHp, Is.EqualTo(50));
            Assert.That(player.TryGetStatusEffect(StatusEffectKind.Lifesteal, out StatusEffectInstance lifesteal), Is.True);
            Assert.That(lifesteal.StackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_LifestealRefreshedDuringAction_DoesNotConsumeRefreshedState()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 100, currentHp: 50);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 100);
            StatusEffectInstance lifesteal = ApplyLifesteal(player, stackCount: 1);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                events,
                CombatEffect.ApplyStatus(
                    new StatusEffectSpec(
                        StatusEffectKind.Lifesteal,
                        duration: 0,
                        magnitude: 2,
                        StatusStackMode.Refresh),
                    CombatEffectTarget.Self),
                new CombatEffect(
                    CombatEffectKind.Damage,
                    10,
                    CombatEffectTarget.SelectedEnemy(enemy.Id)));

            Assert.That(player.CurrentHp, Is.EqualTo(52));
            Assert.That(lifesteal.StackCount, Is.EqualTo(2));
        }

        [Test]
        public void ResolvePlayerEffects_LifestealAtFullHealth_ConsumesAndExpires()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 100);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 100);
            ApplyLifesteal(player, stackCount: 1);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                events,
                new CombatEffect(
                    CombatEffectKind.Damage,
                    10,
                    CombatEffectTarget.SelectedEnemy(enemy.Id)));

            Assert.That(player.CurrentHp, Is.EqualTo(100));
            Assert.That(HealEvents(events).Single().ApplyResult.HealApplied, Is.EqualTo(0));
            Assert.That(player.TryGetStatusEffect(StatusEffectKind.Lifesteal, out _), Is.False);
            Assert.That(events, Has.Some.Matches<CombatEvent>(e =>
                e.Kind == CombatEventKind.StatusExpired &&
                e.StatusEffectKind == StatusEffectKind.Lifesteal));
        }

        [Test]
        public void ResolveEnemyActions_Lifesteal_UsesSameRulesAsPlayer()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 100);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 100, currentHp: 50);
            StatusEffectInstance lifesteal = ApplyLifesteal(enemy, stackCount: 2);
            var events = new List<CombatEvent>();
            var action = new EnemyPlannedAction(
                new EnemyActionKey(1),
                "Drain",
                new[]
                {
                    EnemyActionEffect.FromCombatEffect(
                        new CombatEffect(
                            CombatEffectKind.Damage,
                            6,
                            CombatEffectTarget.Enemy)),
                });

            _resolver.ResolveEnemyPlannedActions(
                new[] { action },
                enemy,
                player,
                new[] { enemy },
                Participants(player, enemy),
                default,
                BattlePhase.EnemyTurn,
                events,
                () => false);

            Assert.That(player.CurrentHp, Is.EqualTo(94));
            Assert.That(enemy.CurrentHp, Is.EqualTo(52));
            Assert.That(lifesteal.StackCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_Lifesteal_DamageEventPrecedesHealEvent()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 100, currentHp: 50);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 100);
            ApplyLifesteal(player, stackCount: 2);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                events,
                new CombatEffect(
                    CombatEffectKind.Damage,
                    10,
                    CombatEffectTarget.SelectedEnemy(enemy.Id)));

            int damageEventIndex = events.FindIndex(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Damage);
            int healEventIndex = events.FindIndex(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Heal);

            Assert.That(damageEventIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(healEventIndex, Is.GreaterThan(damageEventIndex));
        }

        private void ResolvePlayerEffects(
            CombatParticipant player,
            IReadOnlyList<CombatParticipant> enemies,
            List<CombatEvent> events,
            params CombatEffect[] effects)
        {
            ResolvePlayerEffects(player, enemies, events, () => false, effects);
        }

        private void ResolvePlayerEffects(
            CombatParticipant player,
            IReadOnlyList<CombatParticipant> enemies,
            List<CombatEvent> events,
            Func<bool> shouldEndBattle,
            params CombatEffect[] effects)
        {
            var participants = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
            };
            for (int index = 0; index < enemies.Count; index++)
            {
                participants[enemies[index].Id.Value] = enemies[index];
            }

            _resolver.ResolvePlayerEffects(
                effects,
                player,
                player,
                enemies,
                participants,
                enemies.Count > 0 ? enemies[0].Id : default,
                BattlePhase.Resolving,
                events,
                shouldEndBattle);
        }

        private static StatusEffectInstance ApplyLifesteal(
            CombatParticipant participant,
            int stackCount)
        {
            var spec = new StatusEffectSpec(
                StatusEffectKind.Lifesteal,
                duration: 0,
                magnitude: stackCount,
                StatusStackMode.Refresh);
            return participant.ApplyStatusEffect(
                new StatusEffectFactory().Create(spec),
                spec.StackMode);
        }

        private static CombatParticipant Participant(
            int id,
            CombatTeam team,
            int maxHp,
            int currentHp = -1,
            int shield = 0)
        {
            return new CombatParticipant(
                maxHp,
                currentHp,
                shield,
                new CombatParticipantId(id),
                team);
        }

        private static IReadOnlyDictionary<int, CombatParticipant> Participants(
            CombatParticipant player,
            CombatParticipant enemy)
        {
            return new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };
        }

        private static List<CombatEvent> HealEvents(List<CombatEvent> events)
        {
            return events
                .Where(e =>
                    e.Kind == CombatEventKind.EffectApplied &&
                    e.Effect.Kind == CombatEffectKind.Heal)
                .ToList();
        }
    }
}
