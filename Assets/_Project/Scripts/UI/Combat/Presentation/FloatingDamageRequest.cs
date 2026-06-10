using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public readonly struct FloatingDamageRequest
    {
        public FloatingDamageRequest(
            int amount,
            bool isCritical,
            bool isPlayerTarget,
            CombatParticipantId targetParticipantId)
        {
            Amount = amount;
            IsCritical = isCritical;
            IsPlayerTarget = isPlayerTarget;
            TargetParticipantId = targetParticipantId;
        }

        public int Amount { get; }

        public bool IsCritical { get; }

        public bool IsPlayerTarget { get; }

        public CombatParticipantId TargetParticipantId { get; }
    }
}
