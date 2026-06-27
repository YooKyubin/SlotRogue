using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class NullCombatPresentationCommands : ICombatPresentationCommands
    {
        public static readonly NullCombatPresentationCommands Instance = new();

        private NullCombatPresentationCommands()
        {
        }

        public UniTask PlayEnemyActionUntilEffectPointAsync(
            CombatParticipantId participantId,
            string actionName,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public UniTask WaitEnemyActionCompletedAsync(
            CombatParticipantId participantId,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public UniTask ShowFloatingCombatTextAsync(
            FloatingCombatTextRequest request,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public UniTask WaitHealthBarAsync(
            CombatParticipantId participantId,
            bool isPlayerTarget,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public UniTask ShowTurnBannerAsync(
            string message,
            float duration,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public UniTask ShowShieldGainAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public UniTask ShowShieldHitAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public UniTask ShowShieldBreakAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public UniTask ShowShieldExpireAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }
    }
}
