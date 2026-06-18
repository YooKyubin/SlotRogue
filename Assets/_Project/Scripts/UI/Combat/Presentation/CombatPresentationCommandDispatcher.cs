using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class CombatPresentationCommandDispatcher : ICombatPresentationCommands
    {
        private readonly FloatingCombatTextLayerView _floatingTextLayerView;
        private readonly TurnBannerView _turnBannerView;
        private readonly ICombatShieldGaugeRegistry _shieldGaugeRegistry;
        private readonly IEnemyCombatVisualPresentationTarget _enemyCombatVisualTarget;

        public CombatPresentationCommandDispatcher(
            FloatingCombatTextLayerView floatingTextLayerView,
            TurnBannerView turnBannerView,
            ICombatShieldGaugeRegistry shieldGaugeRegistry,
            IEnemyCombatVisualPresentationTarget enemyCombatVisualTarget)
        {
            _floatingTextLayerView = floatingTextLayerView;
            _turnBannerView = turnBannerView;
            _shieldGaugeRegistry = shieldGaugeRegistry;
            _enemyCombatVisualTarget = enemyCombatVisualTarget;
        }

        public UniTask PlayEnemyActionUntilEffectPointAsync(
            CombatParticipantId participantId,
            string actionName,
            CancellationToken cancellationToken)
        {
            return _enemyCombatVisualTarget != null
                ? _enemyCombatVisualTarget.PlayEnemyCombatVisualActionUntilEffectPointAsync(
                    participantId,
                    actionName,
                    cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask WaitEnemyActionCompletedAsync(
            CombatParticipantId participantId,
            CancellationToken cancellationToken)
        {
            return _enemyCombatVisualTarget != null
                ? _enemyCombatVisualTarget.WaitEnemyCombatVisualActionCompletedAsync(
                    participantId,
                    cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask ShowFloatingDamageAsync(
            FloatingDamageRequest request,
            CancellationToken cancellationToken)
        {
            return _floatingTextLayerView != null
                ? _floatingTextLayerView.ShowFloatingDamageAsync(request, cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask ShowShieldGainAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken)
        {
            return _shieldGaugeRegistry != null
                ? _shieldGaugeRegistry.ShowShieldGainAsync(request, cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask ShowShieldHitAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken)
        {
            return _shieldGaugeRegistry != null
                ? _shieldGaugeRegistry.ShowShieldHitAsync(request, cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask ShowShieldBreakAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken)
        {
            return _shieldGaugeRegistry != null
                ? _shieldGaugeRegistry.ShowShieldBreakAsync(request, cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask ShowShieldExpireAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken)
        {
            return _shieldGaugeRegistry != null
                ? _shieldGaugeRegistry.ShowShieldExpireAsync(request, cancellationToken)
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
