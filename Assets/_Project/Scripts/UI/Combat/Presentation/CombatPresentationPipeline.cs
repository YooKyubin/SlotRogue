using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class CombatPresentationPipeline
    {
        private readonly ICombatEventPresenter _fallbackPresenter;

        public CombatPresentationPipeline(ICombatEventPresenter fallbackPresenter)
        {
            _fallbackPresenter = fallbackPresenter;
        }

        public UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken)
        {
            return _fallbackPresenter.PresentAsync(combatEvent, viewModel, context, cancellationToken);
        }
    }
}
