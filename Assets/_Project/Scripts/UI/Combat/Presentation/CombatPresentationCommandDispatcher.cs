using System.Threading;
using Cysharp.Threading.Tasks;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class CombatPresentationCommandDispatcher : ICombatPresentationCommands
    {
        private readonly FloatingCombatTextLayerView _floatingTextLayerView;
        private readonly TurnBannerView _turnBannerView;

        public CombatPresentationCommandDispatcher(
            FloatingCombatTextLayerView floatingTextLayerView,
            TurnBannerView turnBannerView)
        {
            _floatingTextLayerView = floatingTextLayerView;
            _turnBannerView = turnBannerView;
        }

        public UniTask ShowFloatingDamageAsync(
            FloatingDamageRequest request,
            CancellationToken cancellationToken)
        {
            return _floatingTextLayerView != null
                ? _floatingTextLayerView.ShowFloatingDamageAsync(request, cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask ShowTurnBannerAsync(
            string message,
            float duration,
            CancellationToken cancellationToken)
        {
            return _turnBannerView != null
                ? _turnBannerView.ShowTurnBannerAsync(message, duration, cancellationToken)
                : UniTask.CompletedTask;
        }
    }
}
