using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public interface IEnemyCombatVisualPresentationTarget
    {
        void PlayEnemyCombatVisualAttack(CombatParticipantId participantId);
    }
}
