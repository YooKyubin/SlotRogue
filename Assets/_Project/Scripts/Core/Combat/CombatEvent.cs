using System;
using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public readonly struct AppliedStatusModifier
    {
        public AppliedStatusModifier(
            CombatParticipantId ownerParticipantId,
            StatusEffectKind kind,
            CombatTeam ownerTeam)
        {
            OwnerParticipantId = ownerParticipantId;
            Kind = kind;
            OwnerTeam = ownerTeam;
        }

        public CombatParticipantId OwnerParticipantId { get; }

        public StatusEffectKind Kind { get; }

        public CombatTeam OwnerTeam { get; }
    }

    public readonly struct CombatEvent
    {
        public CombatEvent(
            CombatEventKind kind,
            BattlePhase phase = BattlePhase.NotInBattle,
            CombatEffect effect = default,
            EffectApplyResult applyResult = default,
            BattleEndReason endReason = BattleEndReason.None,
            bool isPlayerParticipant = false,
            CombatParticipantId targetParticipantId = default,
            CombatParticipantSnapshot targetBefore = default,
            CombatParticipantSnapshot targetAfter = default,
            StatusEffectKind statusEffectKind = StatusEffectKind.None,
            int statusDuration = 0,
            int statusMagnitude = 0,
            int statusStackCount = 0,
            CombatParticipantId sourceParticipantId = default,
            string actionName = "",
            IReadOnlyList<AppliedStatusModifier> appliedStatusModifiers = null)
        {
            Kind = kind;
            Phase = phase;
            Effect = effect;
            ApplyResult = applyResult;
            EndReason = endReason;
            IsPlayerParticipant = isPlayerParticipant;
            TargetParticipantId = targetParticipantId;
            TargetBefore = targetBefore;
            TargetAfter = targetAfter;
            StatusEffectKind = statusEffectKind;
            StatusDuration = statusDuration;
            StatusMagnitude = statusMagnitude;
            StatusStackCount = statusStackCount;
            SourceParticipantId = sourceParticipantId;
            ActionName = actionName ?? string.Empty;
            AppliedStatusModifiers = appliedStatusModifiers ?? Array.Empty<AppliedStatusModifier>();
        }

        public CombatEventKind Kind { get; }

        public BattlePhase Phase { get; }

        public CombatEffect Effect { get; }

        public EffectApplyResult ApplyResult { get; }

        public BattleEndReason EndReason { get; }

        public bool IsPlayerParticipant { get; }

        public CombatParticipantId TargetParticipantId { get; }

        public CombatParticipantSnapshot TargetBefore { get; }

        public CombatParticipantSnapshot TargetAfter { get; }

        public StatusEffectKind StatusEffectKind { get; }

        public int StatusDuration { get; }

        public int StatusMagnitude { get; }

        public int StatusStackCount { get; }

        public CombatParticipantId SourceParticipantId { get; }

        public string ActionName { get; }

        public IReadOnlyList<AppliedStatusModifier> AppliedStatusModifiers { get; }

        public bool HasTargetSnapshot =>
            Kind == CombatEventKind.EffectApplied ||
            Kind == CombatEventKind.StatusTicked;
    }
}
