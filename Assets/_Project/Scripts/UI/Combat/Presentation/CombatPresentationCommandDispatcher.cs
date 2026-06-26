using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class CombatPresentationCommandDispatcher :
        ICombatPresentationCommands,
        ICombatStatusPresentationCommands
    {
        private readonly FloatingCombatTextLayerView _floatingTextLayerView;
        private readonly TurnBannerView _turnBannerView;
        private readonly ICombatShieldGaugeRegistry _shieldGaugeRegistry;
        private readonly IEnemyCombatVisualPresentationTarget _enemyCombatVisualTarget;
        private readonly ICombatHealthBarPresentationTarget _healthBarPresentationTarget;
        private readonly ICombatStatusPresentationCommands _statusPresentationCommands;

        public CombatPresentationCommandDispatcher(
            FloatingCombatTextLayerView floatingTextLayerView,
            TurnBannerView turnBannerView,
            ICombatShieldGaugeRegistry shieldGaugeRegistry,
            IEnemyCombatVisualPresentationTarget enemyCombatVisualTarget,
            ICombatHealthBarPresentationTarget healthBarPresentationTarget,
            ICombatStatusPresentationCommands statusPresentationCommands)
        {
            _floatingTextLayerView = floatingTextLayerView;
            _turnBannerView = turnBannerView;
            _shieldGaugeRegistry = shieldGaugeRegistry;
            _enemyCombatVisualTarget = enemyCombatVisualTarget;
            _healthBarPresentationTarget = healthBarPresentationTarget;
            _statusPresentationCommands = statusPresentationCommands;
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

        public UniTask WaitHealthBarAsync(
            CombatParticipantId participantId,
            bool isPlayerTarget,
            CancellationToken cancellationToken)
        {
            return _healthBarPresentationTarget != null
                ? _healthBarPresentationTarget.WaitHealthBarAsync(
                    participantId,
                    isPlayerTarget,
                    cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask PlayPlayerHitFeedbackAsync(CancellationToken cancellationToken)
        {
            return _healthBarPresentationTarget != null
                ? _healthBarPresentationTarget.PlayPlayerHitFeedbackAsync(cancellationToken)
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

        public UniTask AddEnemyStatusAsync(
            CombatParticipantId participantId,
            StatusEffectViewData status,
            CancellationToken cancellationToken)
        {
            return _statusPresentationCommands.AddEnemyStatusAsync(
                participantId,
                status,
                cancellationToken);
        }

        public UniTask UpdateEnemyStatusValueAsync(
            CombatParticipantId participantId,
            StatusEffectViewData status,
            CancellationToken cancellationToken)
        {
            return _statusPresentationCommands.UpdateEnemyStatusValueAsync(
                participantId,
                status,
                cancellationToken);
        }

        public UniTask PlayEnemyStatusActivationAsync(
            CombatParticipantId participantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            return _statusPresentationCommands.PlayEnemyStatusActivationAsync(
                participantId,
                kind,
                cancellationToken);
        }

        public UniTask PlayEnemyStatusModifierActivationAsync(
            CombatParticipantId ownerParticipantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            return _statusPresentationCommands.PlayEnemyStatusModifierActivationAsync(
                ownerParticipantId,
                kind,
                cancellationToken);
        }

        public UniTask RemoveEnemyStatusAsync(
            CombatParticipantId participantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            return _statusPresentationCommands.RemoveEnemyStatusAsync(
                participantId,
                kind,
                cancellationToken);
        }
    }
}
