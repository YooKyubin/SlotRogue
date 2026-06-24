using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class AfterDirectDamageReceivedContext
    {
        private readonly BattlePhase _phase;
        private readonly ICombatRandom _combatRandom;
        private readonly EffectApplicator _effectApplicator;
        private readonly List<CombatEvent> _events;

        internal AfterDirectDamageReceivedContext(
            BattlePhase phase,
            CombatParticipant attacker,
            CombatParticipant target,
            EffectApplyResult applyResult,
            DamageOrigin damageOrigin,
            StatusEffectSnapshot statusSnapshot,
            ICombatRandom combatRandom,
            EffectApplicator effectApplicator,
            List<CombatEvent> events)
        {
            _phase = phase;
            Attacker = attacker;
            Target = target;
            ApplyResult = applyResult;
            DamageOrigin = damageOrigin;
            StatusSnapshot = statusSnapshot;
            _combatRandom = combatRandom;
            _effectApplicator = effectApplicator;
            _events = events;
        }

        public CombatParticipant Attacker { get; }

        public CombatParticipant Target { get; }

        public EffectApplyResult ApplyResult { get; }

        public DamageOrigin DamageOrigin { get; }

        public StatusEffectSnapshot StatusSnapshot { get; }

        public bool RollPercent(int successPercent) =>
            _combatRandom.RollPercent(successPercent);

        public EffectApplyResult ReflectDamage(int amount)
        {
            CombatParticipantSnapshot before = Attacker.CaptureSnapshot();
            var effect = new CombatEffect(
                CombatEffectKind.Damage,
                amount,
                CombatEffectTarget.Self,
                DamageOrigin.Reflection);
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
                sourceParticipantId: Target.Id));

            return result;
        }
    }
}
