using System;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    [Obsolete("Use FloatingCombatTextRequest with FloatingCombatTextKind.Damage.")]
    public readonly struct FloatingDamageRequest
    {
        public FloatingDamageRequest(
            int amount,
            bool isPlayerTarget,
            CombatParticipantId targetParticipantId)
        {
            Amount = amount;
            IsPlayerTarget = isPlayerTarget;
            TargetParticipantId = targetParticipantId;
        }

        public int Amount { get; }

        public bool IsPlayerTarget { get; }

        public CombatParticipantId TargetParticipantId { get; }

        public FloatingCombatTextRequest ToCombatTextRequest()
        {
            return new FloatingCombatTextRequest(
                FloatingCombatTextKind.Damage,
                Amount,
                useDamageScaledFontSize: false,
                IsPlayerTarget,
                TargetParticipantId);
        }
    }
}
