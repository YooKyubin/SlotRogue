using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public readonly struct ShieldPresentationRequest
    {
        public ShieldPresentationRequest(
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
    }
}
