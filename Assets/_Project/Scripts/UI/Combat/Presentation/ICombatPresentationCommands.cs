using System.Threading;
using Cysharp.Threading.Tasks;

namespace SlotRogue.UI.Combat.Presentation
{
    public interface ICombatPresentationCommands
    {
        UniTask ShowFloatingDamageAsync(
            FloatingDamageRequest request,
            CancellationToken cancellationToken);

        UniTask ShowTurnBannerAsync(
            string message,
            float duration,
            CancellationToken cancellationToken);
    }
}
