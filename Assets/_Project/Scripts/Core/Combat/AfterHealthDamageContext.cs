using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class AfterHealthDamageContext
    {
        private readonly BattlePhase _phase;
        private readonly EffectApplicator _effectApplicator;
        private readonly List<CombatEvent> _events;

        internal AfterHealthDamageContext(
            BattlePhase phase,
            CombatParticipant attacker,
            CombatParticipant target,
            int healthDamage,
            DamageOrigin damageOrigin,
            StatusEffectSnapshot statusSnapshot,
            EffectApplicator effectApplicator,
            List<CombatEvent> events)
        {
            _phase = phase;
            Attacker = attacker;
            Target = target;
            HealthDamage = healthDamage;
            DamageOrigin = damageOrigin;
            StatusSnapshot = statusSnapshot;
            _effectApplicator = effectApplicator;
            _events = events;
        }

        public CombatParticipant Attacker { get; }

        public CombatParticipant Target { get; }

        public int HealthDamage { get; }

        public DamageOrigin DamageOrigin { get; }

        public StatusEffectSnapshot StatusSnapshot { get; }

        public EffectApplyResult HealAttacker(int amount)
        {
            CombatParticipantSnapshot before = Attacker.CaptureSnapshot();
            var effect = new CombatEffect(
                CombatEffectKind.Heal,
                amount,
                CombatEffectTarget.Self);
            EffectApplyResult result = _effectApplicator.ApplyToParticipant(effect, Attacker);
            CombatParticipantSnapshot after = Attacker.CaptureSnapshot();

            _events.Add(new CombatEvent(
                CombatEventKind.EffectApplied,
                _phase,
                effect,
                result,
                isPlayerParticipant: Attacker.Team == CombatTeam.Player,
                targetParticipantId: Attacker.Id,
                targetBefore: before,
                targetAfter: after,
                statusEffectKind: StatusSnapshot.Kind,
                statusDuration: StatusSnapshot.RemainingTurns,
                statusMagnitude: StatusSnapshot.Magnitude,
                statusStackCount: StatusSnapshot.StackCount,
                sourceParticipantId: Attacker.Id));

            return result;
        }
    }
}
