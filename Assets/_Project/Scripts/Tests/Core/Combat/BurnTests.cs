using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class BurnTests
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
        public void ApplyStatus_BurnDealsImmediateStatusDamageAfterAppliedEvent()
        {
            CombatParticipant participant = Participant(1, CombatTeam.Player, maxHp: 20);
            var events = new List<CombatEvent>();

            ApplyBurn(participant, magnitude: 4, events);

            Assert.That(participant.CurrentHp, Is.EqualTo(16));
            Assert.That(participant.TryGetStatusEffect(StatusEffectKind.Burn, out StatusEffectInstance burn), Is.True);
            Assert.That(burn.Magnitude, Is.EqualTo(4));
            Assert.That(events.Select(combatEvent => combatEvent.Kind), Is.EqualTo(new[]
            {
                CombatEventKind.StatusApplied,
                CombatEventKind.StatusTicked,
            }));
            Assert.That(events[1].Effect.DamageOrigin, Is.EqualTo(DamageOrigin.Status));
        }

        [Test]
        public void ApplyStatus_BurnConsumesShieldBeforeHealth()
        {
            CombatParticipant participant = Participant(1, CombatTeam.Player, maxHp: 20, shield: 3);

            ApplyBurn(participant, magnitude: 4, new List<CombatEvent>());

            Assert.That(participant.Shield, Is.EqualTo(0));
            Assert.That(participant.CurrentHp, Is.EqualTo(19));
        }

        [Test]
        public void NotifyTeamTurnEnded_BurnOnlyTicksAndExpiresForOwnersTeam()
        {
            CombatParticipant participant = Participant(1, CombatTeam.Player, maxHp: 20);
            var events = new List<CombatEvent>();
            ApplyBurn(participant, magnitude: 4, events);
            events.Clear();

            _statusEffectEngine.NotifyTeamTurnEnded(
                CombatTeam.Enemy,
                participant,
                BattlePhase.EnemyTurn,
                events);

            Assert.That(participant.CurrentHp, Is.EqualTo(16));
            Assert.That(participant.TryGetStatusEffect(StatusEffectKind.Burn, out _), Is.True);
            Assert.That(events, Is.Empty);

            _statusEffectEngine.NotifyTeamTurnEnded(
                CombatTeam.Player,
                participant,
                BattlePhase.Resolving,
                events);

            Assert.That(participant.CurrentHp, Is.EqualTo(12));
            Assert.That(participant.TryGetStatusEffect(StatusEffectKind.Burn, out _), Is.False);
            Assert.That(events.Select(combatEvent => combatEvent.Kind), Is.EqualTo(new[]
            {
                CombatEventKind.StatusTicked,
                CombatEventKind.StatusExpired,
            }));
        }

        [Test]
        public void ApplyStatus_BurnRefreshUsesUpdatedMagnitudeImmediatelyAndAtTurnEnd()
        {
            CombatParticipant participant = Participant(1, CombatTeam.Player, maxHp: 30);
            var events = new List<CombatEvent>();
            ApplyBurn(participant, magnitude: 3, events);
            StatusEffectInstance burn = GetStatus(participant, StatusEffectKind.Burn);
            int revisionBeforeRefresh = burn.Revision;

            ApplyBurn(participant, magnitude: 5, events);
            _statusEffectEngine.NotifyTeamTurnEnded(
                CombatTeam.Player,
                participant,
                BattlePhase.Resolving,
                events);

            Assert.That(participant.CurrentHp, Is.EqualTo(17));
            Assert.That(burn.Revision, Is.GreaterThan(revisionBeforeRefresh));
            Assert.That(participant.TryGetStatusEffect(StatusEffectKind.Burn, out _), Is.False);
        }

        [Test]
        public void ApplyStatus_BurnStackUsesMagnitudeWithoutMultiplyingStackCount()
        {
            CombatParticipant participant = Participant(1, CombatTeam.Player, maxHp: 30);
            var events = new List<CombatEvent>();
            ApplyBurn(participant, magnitude: 3, events);

            ApplyBurn(participant, magnitude: 5, events, StatusStackMode.Stack);

            Assert.That(participant.CurrentHp, Is.EqualTo(22));
            Assert.That(participant.TryGetStatusEffect(StatusEffectKind.Burn, out StatusEffectInstance burn), Is.True);
            Assert.That(burn.Magnitude, Is.EqualTo(5));
            Assert.That(burn.StackCount, Is.EqualTo(0));

            _statusEffectEngine.NotifyTeamTurnEnded(
                CombatTeam.Player,
                participant,
                BattlePhase.Resolving,
                events);

            Assert.That(participant.CurrentHp, Is.EqualTo(17));
        }

        [Test]
        public void ApplyStatus_BurnBypassesDamageModifiersLifestealAndThorns()
        {
            CombatParticipant participant = Participant(1, CombatTeam.Player, maxHp: 30);
            var events = new List<CombatEvent>();
            ApplyStatus(participant, StatusEffectKind.Vulnerable, magnitude: 2, events);
            ApplyStatus(participant, StatusEffectKind.Weaken, magnitude: 2, events);
            ApplyStatus(participant, StatusEffectKind.Lifesteal, magnitude: 2, events);
            ApplyStatus(participant, StatusEffectKind.Thorns, magnitude: 7, events);

            ApplyBurn(participant, magnitude: 10, events);

            Assert.That(participant.CurrentHp, Is.EqualTo(20));
            Assert.That(GetStatus(participant, StatusEffectKind.Vulnerable).StackCount, Is.EqualTo(2));
            Assert.That(GetStatus(participant, StatusEffectKind.Weaken).StackCount, Is.EqualTo(2));
            Assert.That(GetStatus(participant, StatusEffectKind.Lifesteal).StackCount, Is.EqualTo(2));
            Assert.That(events.Count(combatEvent =>
                combatEvent.Kind == CombatEventKind.StatusTicked &&
                combatEvent.StatusEffectKind == StatusEffectKind.Burn), Is.EqualTo(1));
        }

        [Test]
        public void ApplyPlayerTurn_PlayerBurnTicksAtPlayerTeamTurnEnd()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 20);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 20);
            ApplyBurn(player, magnitude: 3, new List<CombatEvent>());
            var battle = new BattleSystem();
            battle.StartBattle(player, new[] { Combatant(enemy, damage: 0) });

            battle.ApplyPlayerTurn(Array.Empty<CombatEffect>());

            Assert.That(player.CurrentHp, Is.EqualTo(14));
            Assert.That(player.TryGetStatusEffect(StatusEffectKind.Burn, out _), Is.False);
        }

        [Test]
        public void ApplyPlayerTurn_EnemyBurnTicksAtEnemyTeamTurnEndAndEndsBattleAfterExpiration()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 20);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 6);
            ApplyBurn(enemy, magnitude: 3, new List<CombatEvent>());
            var battle = new BattleSystem();
            battle.StartBattle(player, new[] { Combatant(enemy, damage: 0) });

            BattleApplyResult result = battle.ApplyPlayerTurn(Array.Empty<CombatEffect>());

            Assert.That(result.EndReason, Is.EqualTo(BattleEndReason.Victory));
            Assert.That(enemy.IsDead, Is.True);
            Assert.That(enemy.TryGetStatusEffect(StatusEffectKind.Burn, out _), Is.False);

            int damageIndex = FindEventIndex(battle.Events, CombatEventKind.StatusTicked);
            int expiredIndex = FindEventIndex(battle.Events, CombatEventKind.StatusExpired);
            int battleEndedIndex = FindEventIndex(battle.Events, CombatEventKind.BattleEnded);
            Assert.That(expiredIndex, Is.GreaterThan(damageIndex));
            Assert.That(battleEndedIndex, Is.GreaterThan(expiredIndex));
        }

        [Test]
        public void ApplyPlayerTurn_EnemyKilledByBurnApply_DoesNotTickBurnAtEnemyTeamTurnEnd()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 20);
            CombatParticipant firstEnemy = Participant(100, CombatTeam.Enemy, maxHp: 3);
            CombatParticipant secondEnemy = Participant(101, CombatTeam.Enemy, maxHp: 10);
            var battle = new BattleSystem();
            battle.StartBattle(
                player,
                new[]
                {
                    Combatant(firstEnemy, damage: 0),
                    Combatant(secondEnemy, damage: 0),
                });

            BattleApplyResult result = battle.ApplyPlayerTurn(
                new[]
                {
                    CombatEffect.ApplyStatus(
                        new StatusEffectSpec(
                            StatusEffectKind.Burn,
                            duration: 0,
                            magnitude: 3,
                            StatusStackMode.Refresh),
                        CombatEffectTarget.SelectedEnemy(firstEnemy.Id)),
                },
                selectedTargetId: firstEnemy.Id);

            Assert.That(result.EndReason, Is.EqualTo(BattleEndReason.None));
            Assert.That(firstEnemy.IsDead, Is.True);
            Assert.That(secondEnemy.IsDead, Is.False);
            Assert.That(battle.CurrentPhase, Is.EqualTo(BattlePhase.PlayerTurn));
            Assert.That(battle.Events.Any(combatEvent =>
                combatEvent.Kind == CombatEventKind.StatusTicked &&
                combatEvent.Phase == BattlePhase.EnemyTurn &&
                combatEvent.TargetParticipantId.Value == firstEnemy.Id.Value &&
                combatEvent.StatusEffectKind == StatusEffectKind.Burn), Is.False);
        }

        private void ApplyBurn(
            CombatParticipant participant,
            int magnitude,
            List<CombatEvent> events,
            StatusStackMode stackMode = StatusStackMode.Refresh)
        {
            ApplyStatus(participant, StatusEffectKind.Burn, magnitude, events, stackMode);
        }

        private void ApplyStatus(
            CombatParticipant participant,
            StatusEffectKind kind,
            int magnitude,
            List<CombatEvent> events,
            StatusStackMode stackMode = StatusStackMode.Refresh)
        {
            _statusEffectEngine.ApplyStatus(
                new StatusEffectSpec(kind, duration: 0, magnitude, stackMode),
                participant,
                BattlePhase.Resolving,
                events);
        }

        private static StatusEffectInstance GetStatus(
            CombatParticipant participant,
            StatusEffectKind kind)
        {
            Assert.That(participant.TryGetStatusEffect(kind, out StatusEffectInstance instance), Is.True);
            return instance;
        }

        private static CombatParticipant Participant(
            int id,
            CombatTeam team,
            int maxHp,
            int shield = 0) =>
            new(
                maxHp,
                currentHp: maxHp,
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

        private static int FindEventIndex(
            IReadOnlyList<CombatEvent> events,
            CombatEventKind kind)
        {
            for (int index = 0; index < events.Count; index++)
            {
                if (events[index].Kind == kind)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
