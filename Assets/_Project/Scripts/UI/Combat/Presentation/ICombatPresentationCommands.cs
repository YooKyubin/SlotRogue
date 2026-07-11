using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    /// <summary>
    /// Combat Presenter가 구체적인 View 계층을 직접 참조하지 않고 전투 연출 명령을 요청하는 경계다.
    /// </summary>
    public interface ICombatPresentationCommands
    {
        UniTask PlayEnemyActionUntilEffectPointAsync(
            CombatParticipantId participantId,
            string actionName,
            CancellationToken cancellationToken);

        UniTask WaitEnemyActionCompletedAsync(
            CombatParticipantId participantId,
            CancellationToken cancellationToken);

        UniTask PlayEnemyDeathAsync(
            CombatParticipantId participantId,
            CancellationToken cancellationToken);

        UniTask ShowFloatingCombatTextAsync(
            FloatingCombatTextRequest request,
            CancellationToken cancellationToken);

        UniTask ShowCombatDamageVFXAsync(
            CombatDamageVFXRequest request,
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

    public interface ICombatDamageVFXPresentationTarget
    {
        UniTask ShowCombatDamageVFXAsync(CombatDamageVFXRequest request, CancellationToken cancellationToken);
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
