using System.Threading;
using Cysharp.Threading.Tasks;

namespace SlotRogue.UI.GameFlow
{
    public interface IEnemyCombatVisual
    {
        void PlayIdle();

        UniTask PlayActionUntilEffectPointAsync(string actionName, CancellationToken cancellationToken);
        UniTask WaitForActionCompletedAsync(CancellationToken cancellationToken);
    }
}
