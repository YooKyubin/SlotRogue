using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;

namespace SlotRogue.UI.Combat.Presentation
{
    /// <summary>
    /// 선택된 Damage VFX profile과 대상 정보를 presentation command 경로로 전달한다.
    /// </summary>
    public readonly struct CombatDamageVFXRequest
    {
        /// <summary>
        /// 하나의 전투 대상에 하나의 Damage VFX profile 재생을 요청한다.
        /// </summary>
        public CombatDamageVFXRequest(
            CombatDamageVFXProfile profile,
            CombatParticipantId targetParticipantId,
            int damageAmount,
            Func<CancellationToken, UniTask> impactHandler = null)
        {
            Profile = profile;
            TargetParticipantId = targetParticipantId;
            DamageAmount = damageAmount;
            _impactHandler = impactHandler;
        }

        private readonly Func<CancellationToken, UniTask> _impactHandler;

        public CombatDamageVFXProfile Profile { get; }

        public CombatParticipantId TargetParticipantId { get; }

        public int DamageAmount { get; }

        public bool HasImpactHandler => _impactHandler != null;

        /// <summary>
        /// Damage VFX의 명중 프레임에서 실행할 presentation 후처리를 요청한다.
        /// </summary>
        public UniTask HandleImpactAsync(CancellationToken cancellationToken)
        {
            return _impactHandler != null
                ? _impactHandler(cancellationToken)
                : UniTask.CompletedTask;
        }
    }
}
