using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public interface IEnemyCombatVisualPresentationTarget
    {
        UniTask PlayEnemyCombatVisualActionUntilEffectPointAsync(
            CombatParticipantId participantId,
            string actionName,
            CancellationToken cancellationToken);

        UniTask WaitEnemyCombatVisualActionCompletedAsync(
            CombatParticipantId participantId,
            CancellationToken cancellationToken);
    }
}
