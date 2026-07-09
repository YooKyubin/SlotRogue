using System.Threading;
using Cysharp.Threading.Tasks;

namespace SlotRogue.UI.Combat.Presentation
{
    /// <summary>
    /// 조합형 Damage VFX에서 재사용 가능한 단일 module의 계약이다.
    /// </summary>
    public interface ICombatDamageVFXModule
    {
        /// <summary>
        /// 전달받은 대상 context로 module을 재생하고, 보이는 연출이 끝나면 완료된다.
        /// </summary>
        UniTask PlayAsync(CombatDamageVFXContext context, CancellationToken cancellationToken);
    }
}
