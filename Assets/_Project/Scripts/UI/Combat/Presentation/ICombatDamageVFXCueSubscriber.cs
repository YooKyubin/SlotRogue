using System;
using System.Threading;

namespace SlotRogue.UI.Combat.Presentation
{
    /// <summary>
    /// Damage VFX 실행 중 animation cue를 수신하는 module의 계약이다.
    /// </summary>
    public interface ICombatDamageVFXCueSubscriber
    {
        IDisposable Subscribe(
            CombatDamageVFXContext context,
            CancellationToken cancellationToken);
    }
}
