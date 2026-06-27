using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public interface ICombatPresentationCommands
    {
        UniTask PlayEnemyActionUntilEffectPointAsync(
            CombatParticipantId participantId,
            string actionName,
            CancellationToken cancellationToken);

        UniTask WaitEnemyActionCompletedAsync(
            CombatParticipantId participantId,
            CancellationToken cancellationToken);

        UniTask ShowFloatingCombatTextAsync(
            FloatingCombatTextRequest request,
            CancellationToken cancellationToken);

        UniTask WaitHealthBarAsync(
            CombatParticipantId participantId,
            bool isPlayerTarget,
            CancellationToken cancellationToken);

        UniTask ShowShieldGainAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken);

        UniTask ShowShieldHitAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken);

        UniTask ShowShieldBreakAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken);

        UniTask ShowShieldExpireAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken);

        UniTask ShowTurnBannerAsync(
            string message,
            float duration,
            CancellationToken cancellationToken);
    }

    public interface ICombatHealthBarPresentationTarget
    {
        UniTask WaitHealthBarAsync(
            CombatParticipantId participantId,
            bool isPlayerTarget,
            CancellationToken cancellationToken);
    }

    public interface ICombatStatusPresentationCommands
    {
        UniTask AddEnemyStatusAsync(
            CombatParticipantId participantId,
            StatusEffectViewData status,
            CancellationToken cancellationToken);

        UniTask UpdateEnemyStatusValueAsync(
            CombatParticipantId participantId,
            StatusEffectViewData status,
            CancellationToken cancellationToken);

        UniTask PlayEnemyStatusActivationAsync(
            CombatParticipantId participantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken);

        UniTask PlayEnemyStatusModifierActivationAsync(
            CombatParticipantId ownerParticipantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken);

        UniTask RemoveEnemyStatusAsync(
            CombatParticipantId participantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken);
    }
}
