using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class ThornsTests
    {
        private EffectApplicator _effectApplicator = null!;
        private StatusEffectEngine _statusEffectEngine = null!;

        [SetUp]
        public void SetUp()
        {
            _effectApplicator = new EffectApplicator();
            _statusEffectEngine = new StatusEffectEngine(_effectApplicator);
        }

        [Test]
        public void ResolvePlayerEffects_ThornsSuccessfulRoll_ReflectsMagnitude()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 30);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 30);
            ApplyThorns(enemy, magnitude: 4);
            var random = new SequenceCombatRandom(true);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(player, new[] { enemy }, random, events, DirectDamage(enemy, 3));

            Assert.That(player.CurrentHp, Is.EqualTo(26));
            Assert.That(enemy.CurrentHp, Is.EqualTo(27));
            Assert.That(random.RollCount, Is.EqualTo(1));
            AssertReflection(events, amount: 4, target: player);
        }

        [Test]
        public void ResolvePlayerEffects_ThornsFailedRoll_DoesNotReflect()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 30);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 30);
            ApplyThorns(enemy, magnitude: 4);
            var random = new SequenceCombatRandom(false);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(player, new[] { enemy }, random, events, DirectDamage(enemy, 3));

            Assert.That(player.CurrentHp, Is.EqualTo(30));
            Assert.That(random.RollCount, Is.EqualTo(1));
            Assert.That(ReflectionEvents(events), Is.Empty);
        }

        [Test]
        public void ResolvePlayerEffects_Thorns_MultiHitRollsIndependently()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 30);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 30);
            ApplyThorns(enemy, magnitude: 2);
            var random = new SequenceCombatRandom(true, false, true);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                random,
                events,
                DirectDamage(enemy, 1),
                DirectDamage(enemy, 1),
                DirectDamage(enemy, 1));

            Assert.That(player.CurrentHp, Is.EqualTo(26));
            Assert.That(random.RollCount, Is.EqualTo(3));
            Assert.That(ReflectionEvents(events).Count, Is.EqualTo(2));
        }

        [Test]
        public void ResolvePlayerEffects_Thorns_MultiTargetRollsForEachTarget()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 30);
            CombatParticipant enemy1 = Participant(100, CombatTeam.Enemy, maxHp: 30);
            CombatParticipant enemy2 = Participant(101, CombatTeam.Enemy, maxHp: 30);
            ApplyThorns(enemy1, magnitude: 2);
            ApplyThorns(enemy2, magnitude: 3);
            var random = new SequenceCombatRandom(true, true);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy1, enemy2 },
                random,
                events,
                new CombatEffect(
                    CombatEffectKind.Damage,
                    1,
                    new CombatEffectTarget(CombatTargetMode.AllEnemies)));

            Assert.That(player.CurrentHp, Is.EqualTo(25));
            Assert.That(random.RollCount, Is.EqualTo(2));
            Assert.That(
                ReflectionEvents(events).Select(e => e.Effect.Amount),
                Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void ResolvePlayerEffects_Thorns_FullyBlockedDirectDamageStillRolls()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 30);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 30, shield: 10);
            ApplyThorns(enemy, magnitude: 4);
            var random = new SequenceCombatRandom(true);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(player, new[] { enemy }, random, events, DirectDamage(enemy, 3));

            Assert.That(enemy.CurrentHp, Is.EqualTo(30));
            Assert.That(player.CurrentHp, Is.EqualTo(26));
            Assert.That(random.RollCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_Thorns_ReflectionConsumesAttackerShieldFirst()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 30, shield: 3);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 30);
            ApplyThorns(enemy, magnitude: 5);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                new SequenceCombatRandom(true),
                events,
                DirectDamage(enemy, 1));

            Assert.That(player.Shield, Is.EqualTo(0));
            Assert.That(player.CurrentHp, Is.EqualTo(28));
            CombatEvent reflection = ReflectionEvents(events).Single();
            Assert.That(reflection.ApplyResult.ShieldConsumed, Is.EqualTo(3));
            Assert.That(reflection.ApplyResult.DamageDealt, Is.EqualTo(2));
        }

        [Test]
        public void ResolvePlayerEffects_ReflectionDoesNotTriggerOtherThorns()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 30);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 30);
            ApplyThorns(player, magnitude: 7);
            ApplyThorns(enemy, magnitude: 4);
            var random = new SequenceCombatRandom(true, true);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(player, new[] { enemy }, random, events, DirectDamage(enemy, 1));

            Assert.That(player.CurrentHp, Is.EqualTo(26));
            Assert.That(enemy.CurrentHp, Is.EqualTo(29));
            Assert.That(random.RollCount, Is.EqualTo(1));
            Assert.That(ReflectionEvents(events).Count, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePlayerEffects_ReflectionBypassesDamageModifiersAndLifesteal()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 30, currentHp: 20);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 30, currentHp: 20);
            ApplyStatus(player, StatusEffectKind.Weaken, magnitude: 2);
            ApplyStatus(player, StatusEffectKind.Lifesteal, magnitude: 2);
            ApplyStatus(player, StatusEffectKind.Vulnerable, magnitude: 2);
            ApplyStatus(enemy, StatusEffectKind.Lifesteal, magnitude: 2);
            ApplyThorns(enemy, magnitude: 5);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                new SequenceCombatRandom(true),
                events,
                DirectDamage(enemy, 10));

            Assert.That(enemy.CurrentHp, Is.EqualTo(12));
            Assert.That(player.CurrentHp, Is.EqualTo(17));
            Assert.That(ReflectionEvents(events).Single().Effect.Amount, Is.EqualTo(5));
            Assert.That(enemy.TryGetStatusEffect(StatusEffectKind.Lifesteal, out StatusEffectInstance enemyLifesteal), Is.True);
            Assert.That(enemyLifesteal.StackCount, Is.EqualTo(2));
        }

        [TestCase(DamageOrigin.Status)]
        [TestCase(DamageOrigin.Reflection)]
        public void ResolvePlayerEffects_Thorns_NonDirectDamageDoesNotRoll(DamageOrigin origin)
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 30);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 30);
            ApplyThorns(enemy, magnitude: 4);
            var random = new SequenceCombatRandom(true);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                random,
                events,
                new CombatEffect(
                    CombatEffectKind.Damage,
                    3,
                    CombatEffectTarget.SelectedEnemy(enemy.Id),
                    origin));

            Assert.That(random.RollCount, Is.EqualTo(0));
            Assert.That(player.CurrentHp, Is.EqualTo(30));
        }

        [Test]
        public void ResolvePlayerEffects_Thorns_LethalHitStillReflectsBeforeBattleEndCheck()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 30);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 1);
            ApplyThorns(enemy, magnitude: 4);
            var events = new List<CombatEvent>();
            int attackerHpAtBattleEndCheck = 0;

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                new SequenceCombatRandom(true),
                events,
                () =>
                {
                    attackerHpAtBattleEndCheck = player.CurrentHp;
                    return enemy.IsDead;
                },
                DirectDamage(enemy, 10));

            Assert.That(enemy.IsDead, Is.True);
            Assert.That(player.CurrentHp, Is.EqualTo(26));
            Assert.That(attackerHpAtBattleEndCheck, Is.EqualTo(26));
        }

        [Test]
        public void ResolvePlayerEffects_ThornsReflectionKillsAttackerBeforeBattleEndCheck()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 4);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 30);
            ApplyThorns(enemy, magnitude: 4);
            bool attackerWasDeadAtBattleEndCheck = false;

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                new SequenceCombatRandom(true),
                new List<CombatEvent>(),
                () =>
                {
                    attackerWasDeadAtBattleEndCheck = player.IsDead;
                    return player.IsDead;
                },
                DirectDamage(enemy, 1));

            Assert.That(player.IsDead, Is.True);
            Assert.That(attackerWasDeadAtBattleEndCheck, Is.True);
        }

        [Test]
        public void ResolvePlayerEffects_LifestealEventPrecedesThornsReflectionEvent()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 30, currentHp: 20);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 30);
            ApplyStatus(player, StatusEffectKind.Lifesteal, magnitude: 2);
            ApplyThorns(enemy, magnitude: 4);
            var events = new List<CombatEvent>();

            ResolvePlayerEffects(
                player,
                new[] { enemy },
                new SequenceCombatRandom(true),
                events,
                DirectDamage(enemy, 10));

            int damageIndex = FindEffectEventIndex(events, DamageOrigin.DirectAction);
            int healIndex = events.FindIndex(e =>
                e.Kind == CombatEventKind.EffectApplied &&
                e.Effect.Kind == CombatEffectKind.Heal);
            int reflectionIndex = FindEffectEventIndex(events, DamageOrigin.Reflection);

            Assert.That(healIndex, Is.GreaterThan(damageIndex));
            Assert.That(reflectionIndex, Is.GreaterThan(healIndex));
        }

        [Test]
        public void ApplyPlayerTurn_EnemyThornsExpiresWithoutExpiringOtherStatuses()
        {
            var random = new SequenceCombatRandom(false);
            var battle = new BattleSystem(new EffectApplicator(), random);
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 30);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 30);
            ApplyThorns(enemy, magnitude: 4);
            ApplyStatus(enemy, StatusEffectKind.Weaken, magnitude: 2);
            battle.StartBattle(player, new[] { Combatant(enemy, damage: 0) });

            battle.ApplyPlayerTurn(Array.Empty<CombatEffect>());

            Assert.That(enemy.TryGetStatusEffect(StatusEffectKind.Thorns, out _), Is.False);
            Assert.That(enemy.TryGetStatusEffect(StatusEffectKind.Weaken, out _), Is.True);
            Assert.That(battle.Events, Has.Some.Matches<CombatEvent>(e =>
                e.Kind == CombatEventKind.StatusExpired &&
                e.TargetParticipantId.Value == enemy.Id.Value &&
                e.StatusEffectKind == StatusEffectKind.Thorns));
        }

        [Test]
        public void ApplyPlayerTurn_PlayerThornsSurvivesOwnTurnAndExpiresAtEnemyTeamTurnEnd()
        {
            var random = new SequenceCombatRandom(true);
            var battle = new BattleSystem(new EffectApplicator(), random);
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 30);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 30);
            ApplyThorns(player, magnitude: 4);
            battle.StartBattle(player, new[] { Combatant(enemy, damage: 1) });

            battle.ApplyPlayerTurn(Array.Empty<CombatEffect>());

            Assert.That(enemy.CurrentHp, Is.EqualTo(26));
            Assert.That(player.TryGetStatusEffect(StatusEffectKind.Thorns, out _), Is.False);

            int reflectionIndex = FindEffectEventIndex(battle.Events, DamageOrigin.Reflection);
            int expiredIndex = FindStatusExpiredIndex(battle.Events, player.Id, StatusEffectKind.Thorns);
            Assert.That(reflectionIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(expiredIndex, Is.GreaterThan(reflectionIndex));
        }

        [Test]
        public void Thorns_RefreshAndStackUseCommonStatusPolicy()
        {
            CombatParticipant participant = Participant(1, CombatTeam.Player, maxHp: 30);
            StatusEffectInstance thorns = ApplyThorns(participant, magnitude: 3);

            ApplyStatus(participant, StatusEffectKind.Thorns, magnitude: 5, StatusStackMode.Refresh);

            Assert.That(thorns.Magnitude, Is.EqualTo(5));
            Assert.That(thorns.StackCount, Is.EqualTo(0));

            ApplyStatus(participant, StatusEffectKind.Thorns, magnitude: 7, StatusStackMode.Stack);

            Assert.That(thorns.Magnitude, Is.EqualTo(7));
            Assert.That(thorns.StackCount, Is.EqualTo(0));
        }

        [Test]
        public void ResolveEnemyActions_PlayerThornsUsesSameRules()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 30);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 30);
            ApplyThorns(player, magnitude: 4);
            var events = new List<CombatEvent>();
            var resolver = new CombatActionResolver(
                _effectApplicator,
                _statusEffectEngine,
                new SequenceCombatRandom(true));
            var action = new EnemyPlannedAction(
                new EnemyActionKey(1),
                "Attack",
                new[]
                {
                    EnemyActionEffect.FromCombatEffect(
                        new CombatEffect(
                            CombatEffectKind.Damage,
                            3,
                            CombatEffectTarget.Enemy)),
                });

            resolver.ResolveEnemyPlannedActions(
                new[] { action },
                enemy,
                player,
                new[] { enemy },
                Participants(player, enemy),
                default,
                BattlePhase.EnemyTurn,
                events,
                () => false);

            Assert.That(player.CurrentHp, Is.EqualTo(27));
            Assert.That(enemy.CurrentHp, Is.EqualTo(26));
            AssertReflection(events, amount: 4, target: enemy);
        }

        private void ResolvePlayerEffects(
            CombatParticipant player,
            IReadOnlyList<CombatParticipant> enemies,
            ICombatRandom random,
            List<CombatEvent> events,
            params CombatEffect[] effects)
        {
            ResolvePlayerEffects(player, enemies, random, events, () => false, effects);
        }

        private void ResolvePlayerEffects(
            CombatParticipant player,
            IReadOnlyList<CombatParticipant> enemies,
            ICombatRandom random,
            List<CombatEvent> events,
            Func<bool> shouldEndBattle,
            params CombatEffect[] effects)
        {
            var resolver = new CombatActionResolver(
                _effectApplicator,
                _statusEffectEngine,
                random);
            var participants = new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
            };
            for (int index = 0; index < enemies.Count; index++)
            {
                participants[enemies[index].Id.Value] = enemies[index];
            }

            resolver.ResolvePlayerEffects(
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

        private static CombatEffect DirectDamage(CombatParticipant target, int amount) =>
            new(
                CombatEffectKind.Damage,
                amount,
                CombatEffectTarget.SelectedEnemy(target.Id));

        private static StatusEffectInstance ApplyThorns(
            CombatParticipant participant,
            int magnitude) =>
            ApplyStatus(participant, StatusEffectKind.Thorns, magnitude);

        private static StatusEffectInstance ApplyStatus(
            CombatParticipant participant,
            StatusEffectKind kind,
            int magnitude,
            StatusStackMode stackMode = StatusStackMode.Refresh)
        {
            var spec = new StatusEffectSpec(
                kind,
                duration: 0,
                magnitude,
                stackMode);
            return participant.ApplyStatusEffect(
                new StatusEffectFactory().Create(spec),
                spec.StackMode);
        }

        private static CombatParticipant Participant(
            int id,
            CombatTeam team,
            int maxHp,
            int currentHp = -1,
            int shield = 0) =>
            new(
                maxHp,
                currentHp,
                shield,
                new CombatParticipantId(id),
                team);

        private static EnemyCombatant Combatant(CombatParticipant participant, int damage)
        {
            var plan = new EnemyActionPlan(new[]
            {
                new CombatEffect(
                    CombatEffectKind.Damage,
                    damage,
                    CombatEffectTarget.Enemy),
            });
            return new EnemyCombatant(
                participant,
                new FixedSequenceEnemyActionPlanner(new[] { plan }));
        }

        private static IReadOnlyDictionary<int, CombatParticipant> Participants(
            CombatParticipant player,
            CombatParticipant enemy) =>
            new Dictionary<int, CombatParticipant>
            {
                [player.Id.Value] = player,
                [enemy.Id.Value] = enemy,
            };

        private static List<CombatEvent> ReflectionEvents(IReadOnlyList<CombatEvent> events) =>
            events
                .Where(e =>
                    e.Kind == CombatEventKind.EffectApplied &&
                    e.Effect.DamageOrigin == DamageOrigin.Reflection)
                .ToList();

        private static void AssertReflection(
            IReadOnlyList<CombatEvent> events,
            int amount,
            CombatParticipant target)
        {
            CombatEvent reflection = ReflectionEvents(events).Single();
            Assert.That(reflection.Effect.Amount, Is.EqualTo(amount));
            Assert.That(reflection.Effect.DamageOrigin, Is.EqualTo(DamageOrigin.Reflection));
            Assert.That(reflection.TargetParticipantId, Is.EqualTo(target.Id));
        }

        private static int FindEffectEventIndex(
            IReadOnlyList<CombatEvent> events,
            DamageOrigin origin)
        {
            for (int index = 0; index < events.Count; index++)
            {
                if (events[index].Kind == CombatEventKind.EffectApplied &&
                    events[index].Effect.DamageOrigin == origin)
                {
                    return index;
                }
            }

            return -1;
        }

        private static int FindStatusExpiredIndex(
            IReadOnlyList<CombatEvent> events,
            CombatParticipantId participantId,
            StatusEffectKind kind)
        {
            for (int index = 0; index < events.Count; index++)
            {
                if (events[index].Kind == CombatEventKind.StatusExpired &&
                    events[index].TargetParticipantId.Value == participantId.Value &&
                    events[index].StatusEffectKind == kind)
                {
                    return index;
                }
            }

            return -1;
        }

        private sealed class SequenceCombatRandom : ICombatRandom
        {
            private readonly bool[] _results;
            private int _index;

            internal SequenceCombatRandom(params bool[] results)
            {
                _results = results;
            }

            internal int RollCount { get; private set; }

            public bool RollPercent(int successPercent)
            {
                bool result = _results[_index];
                _index++;
                RollCount++;
                return result;
            }
        }
    }
}
