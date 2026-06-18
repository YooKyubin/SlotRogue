using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class ActionStartedPresenter : CombatPresenterBase
    {
        public ActionStartedPresenter(CombatPresentationHost host)
            : base(host)
        {
        }

        public override UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            if (combatEvent.Kind != CombatEventKind.ActionStarted ||
                combatEvent.Phase != BattlePhase.EnemyTurn ||
                !combatEvent.SourceParticipantId.IsValid)
            {
                return UniTask.CompletedTask;
            }

            return Host.Commands.PlayEnemyActionUntilEffectPointAsync(
                combatEvent.SourceParticipantId,
                combatEvent.ActionName,
                cancellationToken);
        }
    }

    public sealed class ActionCompletedPresenter : CombatPresenterBase
    {
        public ActionCompletedPresenter(CombatPresentationHost host)
            : base(host)
        {
        }

        public override UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            if (combatEvent.Kind != CombatEventKind.ActionCompleted ||
                combatEvent.Phase != BattlePhase.EnemyTurn ||
                !combatEvent.SourceParticipantId.IsValid)
            {
                return UniTask.CompletedTask;
            }

            return Host.Commands.WaitEnemyActionCompletedAsync(
                combatEvent.SourceParticipantId,
                cancellationToken);
        }
    }
}
