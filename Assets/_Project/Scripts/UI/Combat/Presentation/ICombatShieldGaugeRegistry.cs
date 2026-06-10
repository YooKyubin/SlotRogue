using System.Threading;
using Cysharp.Threading.Tasks;

namespace SlotRogue.UI.Combat.Presentation
{
    public interface ICombatShieldGaugeRegistry
    {
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
    }
}
