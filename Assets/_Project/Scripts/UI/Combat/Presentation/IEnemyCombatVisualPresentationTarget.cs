using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public interface IEnemyCombatVisualPresentationTarget
    {
        void PlayEnemyCombatVisualAction(CombatParticipantId participantId, string actionName);
    }
}
