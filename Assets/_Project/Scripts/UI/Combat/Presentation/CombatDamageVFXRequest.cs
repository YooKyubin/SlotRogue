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
            int damageAmount)
        {
            Profile = profile;
            TargetParticipantId = targetParticipantId;
            DamageAmount = damageAmount;
        }

        public CombatDamageVFXProfile Profile { get; }

        public CombatParticipantId TargetParticipantId { get; }

        public int DamageAmount { get; }
    }
}
