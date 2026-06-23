using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class StatusEffectContext
    {
        internal StatusEffectContext(
            BattlePhase phase,
            CombatParticipant participant,
            StatusEffectInstance instance,
            EffectApplicator effectApplicator,
            List<CombatEvent> events)
        {
            Phase = phase;
            Participant = participant;
            Instance = instance;
            _effectApplicator = effectApplicator;
            _events = events;
        }

        private readonly EffectApplicator _effectApplicator;
        private readonly List<CombatEvent> _events;

        public BattlePhase Phase { get; }

        public CombatParticipant Participant { get; }

        public StatusEffectInstance Instance { get; }

        public EffectApplyResult DealDamage(int amount)
        {
            CombatParticipantSnapshot before = new(Participant.CurrentHp, Participant.Shield);
            EffectApplyResult result = _effectApplicator.ApplyToParticipant(
                new CombatEffect(CombatEffectKind.Damage, amount, CombatEffectTarget.Self, DamageOrigin.Status),
                Participant);
            CombatParticipantSnapshot after = new(Participant.CurrentHp, Participant.Shield);

            _events.Add(new CombatEvent(
                CombatEventKind.StatusTicked,
                Phase,
                applyResult: result,
                isPlayerParticipant: Participant.Team == CombatTeam.Player,
                targetParticipantId: Participant.Id,
                targetBefore: before,
                targetAfter: after,
                statusEffectKind: Instance.Kind,
                statusDuration: Instance.RemainingTurns,
                statusMagnitude: Instance.Magnitude,
                statusStackCount: Instance.StackCount));

            return result;
        }

        internal void EmitApplied()
        {
            _events.Add(new CombatEvent(
                CombatEventKind.StatusApplied,
                Phase,
                isPlayerParticipant: Participant.Team == CombatTeam.Player,
                targetParticipantId: Participant.Id,
                statusEffectKind: Instance.Kind,
                statusDuration: Instance.RemainingTurns,
                statusMagnitude: Instance.Magnitude,
                statusStackCount: Instance.StackCount));
        }

        internal void EmitExpired()
        {
            _events.Add(new CombatEvent(
                CombatEventKind.StatusExpired,
                Phase,
                isPlayerParticipant: Participant.Team == CombatTeam.Player,
                targetParticipantId: Participant.Id,
                statusEffectKind: Instance.Kind,
                statusDuration: Instance.RemainingTurns,
                statusMagnitude: Instance.Magnitude,
                statusStackCount: Instance.StackCount));
        }

        internal void EmitActionSkipped()
        {
            _events.Add(new CombatEvent(
                CombatEventKind.ActionSkipped,
                Phase,
                isPlayerParticipant: Participant.Team == CombatTeam.Player,
                targetParticipantId: Participant.Id,
                statusEffectKind: Instance.Kind,
                statusDuration: Instance.RemainingTurns,
                statusMagnitude: Instance.Magnitude,
                statusStackCount: Instance.StackCount));
        }
    }
}
