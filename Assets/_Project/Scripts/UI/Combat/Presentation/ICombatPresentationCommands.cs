using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public interface ICombatPresentationCommands
    {
        UniTask PlayEnemyActionAsync(
            CombatParticipantId participantId,
            string actionName,
            CancellationToken cancellationToken);

        UniTask ShowFloatingDamageAsync(
            FloatingDamageRequest request,
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
}
