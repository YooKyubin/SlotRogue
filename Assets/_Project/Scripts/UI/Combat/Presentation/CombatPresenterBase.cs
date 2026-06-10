using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public abstract class CombatPresenterBase : ICombatEventPresenter
    {
        protected CombatPresenterBase(CombatPresentationHost host)
        {
            Host = host;
        }

        protected CombatPresentationHost Host { get; }

        public abstract UniTask PresentAsync(
            CombatEvent combatEvent,
            CombatViewModel viewModel,
            PresentationContext context,
            CancellationToken cancellationToken);
    }
}
