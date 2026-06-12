using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class CombatDummyPresenter : ICombatEventPresenter
    {
        public UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            viewModel.ApplySnapshot(combatEvent);

            return UniTask.CompletedTask;
        }
    }
}
