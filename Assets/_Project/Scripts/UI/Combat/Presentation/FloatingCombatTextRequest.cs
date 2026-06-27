using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public readonly struct FloatingCombatTextRequest
    {
        public FloatingCombatTextRequest(
            FloatingCombatTextKind kind,
            int amount,
            bool isCritical,
            bool isPlayerTarget,
            CombatParticipantId targetParticipantId)
        {
            Kind = kind;
            Amount = amount;
            IsCritical = isCritical;
            IsPlayerTarget = isPlayerTarget;
            TargetParticipantId = targetParticipantId;
        }

        public FloatingCombatTextKind Kind { get; }

        public int Amount { get; }

        public bool IsCritical { get; }

        public bool IsPlayerTarget { get; }

        public CombatParticipantId TargetParticipantId { get; }
    }
}
