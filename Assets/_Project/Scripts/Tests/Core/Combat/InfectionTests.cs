using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class InfectionTests
    {
        private StatusEffectEngine _statusEffectEngine = null!;

        [SetUp]
        public void SetUp()
        {
            _statusEffectEngine = new StatusEffectEngine(new EffectApplicator());
        }

        [Test]
        public void Infection_PreservesLegacySerializedValueAndUsesOnlyStackCount()
        {
            CombatParticipant participant = Participant(1, CombatTeam.Player, maxHp: 20);
            var events = new List<CombatEvent>();

            ApplyInfection(participant, amount: 3, events);

            StatusEffectInstance infection = GetStatus(participant);
            Assert.That((int)StatusEffectKind.Infection, Is.EqualTo(3));
            Assert.That(participant.CurrentHp, Is.EqualTo(20));
            Assert.That(infection.StackCount, Is.EqualTo(3));
            Assert.That(infection.Magnitude, Is.EqualTo(0));
            Assert.That(infection.RemainingTurns, Is.EqualTo(0));
            Assert.That(events.Select(combatEvent => combatEvent.Kind), Is.EqualTo(new[]
            {
                CombatEventKind.StatusApplied,
            }));
        }

        [Test]
        public void NotifyTeamTurnEnded_InfectionDamagesBeforeDecreasingAndExpiresAtZero()
        {
            CombatParticipant participant = Participant(1, CombatTeam.Player, maxHp: 20);
            var events = new List<CombatEvent>();
            ApplyInfection(participant, amount: 3, events);
            events.Clear();

            NotifyTeamTurnEnded(CombatTeam.Player, participant, events);
            Assert.That(participant.CurrentHp, Is.EqualTo(17));
            Assert.That(GetStatus(participant).StackCount, Is.EqualTo(2));

            NotifyTeamTurnEnded(CombatTeam.Player, participant, events);
            Assert.That(participant.CurrentHp, Is.EqualTo(15));
            Assert.That(GetStatus(participant).StackCount, Is.EqualTo(1));

            NotifyTeamTurnEnded(CombatTeam.Player, participant, events);

            Assert.That(participant.CurrentHp, Is.EqualTo(14));
            Assert.That(participant.TryGetStatusEffect(StatusEffectKind.Infection, out _), Is.False);
            Assert.That(events.Select(combatEvent => combatEvent.Kind), Is.EqualTo(new[]
            {
                CombatEventKind.StatusTicked,
                CombatEventKind.StatusTicked,
                CombatEventKind.StatusTicked,
                CombatEventKind.StatusExpired,
            }));
            Assert.That(events[0].StatusStackCount, Is.EqualTo(3));
            Assert.That(events[1].StatusStackCount, Is.EqualTo(2));
            Assert.That(events[2].StatusStackCount, Is.EqualTo(1));
            Assert.That(events[2].Effect.DamageOrigin, Is.EqualTo(DamageOrigin.Status));
        }

        [Test]
        public void NotifyTeamTurnEnded_InfectionIgnoresOpponentTeam()
        {
            CombatParticipant participant = Participant(1, CombatTeam.Player, maxHp: 20);
            var events = new List<CombatEvent>();
            ApplyInfection(participant, amount: 3, events);
            events.Clear();

            NotifyTeamTurnEnded(CombatTeam.Enemy, participant, events);

            Assert.That(participant.CurrentHp, Is.EqualTo(20));
            Assert.That(GetStatus(participant).StackCount, Is.EqualTo(3));
            Assert.That(events, Is.Empty);
        }

        [Test]
        public void ApplyStatus_InfectionStackAddsWithoutLimitAndRefreshReplaces()
        {
            CombatParticipant participant = Participant(1, CombatTeam.Player, maxHp: 20);
            var events = new List<CombatEvent>();
            ApplyInfection(participant, amount: 3, events);
            StatusEffectInstance infection = GetStatus(participant);
            int initialRevision = infection.Revision;

            ApplyInfection(participant, amount: 4, events, StatusStackMode.Stack);

            Assert.That(infection.StackCount, Is.EqualTo(7));
            Assert.That(infection.Revision, Is.GreaterThan(initialRevision));
            int stackedRevision = infection.Revision;

            ApplyInfection(participant, amount: 5, events, StatusStackMode.Refresh);

            Assert.That(infection.StackCount, Is.EqualTo(5));
            Assert.That(infection.Revision, Is.GreaterThan(stackedRevision));
        }

        [Test]
        public void NotifyTeamTurnEnded_InfectionConsumesShieldAndBypassesOtherStatusReactions()
        {
            CombatParticipant participant = Participant(1, CombatTeam.Player, maxHp: 20, shield: 2);
            var events = new List<CombatEvent>();
            ApplyStatus(participant, StatusEffectKind.Vulnerable, amount: 2, events);
            ApplyStatus(participant, StatusEffectKind.Weaken, amount: 2, events);
            ApplyStatus(participant, StatusEffectKind.Lifesteal, amount: 2, events);
            ApplyStatus(participant, StatusEffectKind.Thorns, amount: 7, events);
            ApplyInfection(participant, amount: 5, events);
            events.Clear();

            NotifyTeamTurnEnded(CombatTeam.Player, participant, events);

            Assert.That(participant.Shield, Is.EqualTo(0));
            Assert.That(participant.CurrentHp, Is.EqualTo(17));
            Assert.That(GetStatus(participant, StatusEffectKind.Vulnerable).StackCount, Is.EqualTo(2));
            Assert.That(GetStatus(participant, StatusEffectKind.Weaken).StackCount, Is.EqualTo(2));
            Assert.That(GetStatus(participant, StatusEffectKind.Lifesteal).StackCount, Is.EqualTo(2));
            Assert.That(GetStatus(participant, StatusEffectKind.Thorns).Magnitude, Is.EqualTo(7));
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].ApplyResult.ShieldConsumed, Is.EqualTo(2));
            Assert.That(events[0].ApplyResult.DamageDealt, Is.EqualTo(3));
        }

        [Test]
        public void ApplyPlayerTurn_PlayerInfectionCanCauseDefeatAfterExpiration()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 3);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 20);
            ApplyInfection(player, amount: 3, new List<CombatEvent>());
            var battle = new BattleSystem();
            battle.StartBattle(player, new[] { Combatant(enemy) });

            BattleApplyResult result = battle.ApplyPlayerTurn(Array.Empty<CombatEffect>());

            Assert.That(result.EndReason, Is.EqualTo(BattleEndReason.Defeat));
            Assert.That(player.TryGetStatusEffect(StatusEffectKind.Infection, out _), Is.True);
            Assert.That(GetStatus(player).StackCount, Is.EqualTo(2));
            AssertEventOrder(battle.Events, CombatEventKind.StatusTicked, CombatEventKind.BattleEnded);
        }

        [Test]
        public void ApplyPlayerTurn_EnemyFinalInfectionTickExpiresBeforeVictory()
        {
            CombatParticipant player = Participant(1, CombatTeam.Player, maxHp: 20);
            CombatParticipant enemy = Participant(100, CombatTeam.Enemy, maxHp: 1);
            ApplyInfection(enemy, amount: 1, new List<CombatEvent>());
            var battle = new BattleSystem();
            battle.StartBattle(player, new[] { Combatant(enemy) });

            BattleApplyResult result = battle.ApplyPlayerTurn(Array.Empty<CombatEffect>());

            Assert.That(result.EndReason, Is.EqualTo(BattleEndReason.Victory));
            Assert.That(enemy.TryGetStatusEffect(StatusEffectKind.Infection, out _), Is.False);
            AssertEventOrder(
                battle.Events,
                CombatEventKind.StatusTicked,
                CombatEventKind.StatusExpired,
                CombatEventKind.BattleEnded);
        }

        private void ApplyInfection(
            CombatParticipant participant,
            int amount,
            List<CombatEvent> events,
            StatusStackMode stackMode = StatusStackMode.Refresh)
        {
            ApplyStatus(participant, StatusEffectKind.Infection, amount, events, stackMode);
        }

        private void ApplyStatus(
            CombatParticipant participant,
            StatusEffectKind kind,
            int amount,
            List<CombatEvent> events,
            StatusStackMode stackMode = StatusStackMode.Refresh)
        {
            _statusEffectEngine.ApplyStatus(
                new StatusEffectSpec(kind, duration: 0, magnitude: amount, stackMode),
                participant,
                BattlePhase.Resolving,
                events);
        }

        private void NotifyTeamTurnEnded(
            CombatTeam endedTeam,
            CombatParticipant participant,
            List<CombatEvent> events)
        {
            _statusEffectEngine.NotifyTeamTurnEnded(
                endedTeam,
                participant,
                BattlePhase.Resolving,
                events);
        }

        private static StatusEffectInstance GetStatus(
            CombatParticipant participant,
            StatusEffectKind kind = StatusEffectKind.Infection)
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

        private static EnemyCombatant Combatant(CombatParticipant participant)
        {
            var plan = new EnemyActionPlan(Array.Empty<CombatEffect>());
            return new EnemyCombatant(
                participant,
                new FixedSequenceEnemyActionPlanner(new[] { plan }));
        }

        private static void AssertEventOrder(
            IReadOnlyList<CombatEvent> events,
            params CombatEventKind[] expectedKinds)
        {
            int previousIndex = -1;
            for (int expectedIndex = 0; expectedIndex < expectedKinds.Length; expectedIndex++)
            {
                int actualIndex = FindEventIndex(events, expectedKinds[expectedIndex], previousIndex + 1);
                Assert.That(actualIndex, Is.GreaterThan(previousIndex));
                previousIndex = actualIndex;
            }
        }

        private static int FindEventIndex(
            IReadOnlyList<CombatEvent> events,
            CombatEventKind kind,
            int startIndex)
        {
            for (int index = startIndex; index < events.Count; index++)
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
