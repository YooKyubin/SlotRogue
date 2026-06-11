using System.Collections.Generic;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 보유 유물을 이번 스핀 족보 결과에 적용해 "이번 턴 전투에 넘길 값"만 계산한 결과.
    /// 유물 시스템은 전투를 직접 실행하지 않고 이 델타 값만 만든다.
    /// 실제 피해/방어/회복/상태이상 처리는 기존 전투 시스템이 담당한다.
    /// </summary>
    public sealed class RelicResolveResult
    {
        public RelicResolveResult(
            int additionalDamage,
            int additionalBlock,
            int healAmount,
            IReadOnlyList<StatusEffectRequest> statusEffectsToApply,
            string activationSummary)
        {
            AdditionalDamage = additionalDamage;
            AdditionalBlock = additionalBlock;
            HealAmount = healAmount;
            StatusEffectsToApply = statusEffectsToApply ?? System.Array.Empty<StatusEffectRequest>();
            ActivationSummary = activationSummary ?? string.Empty;
        }

        /// <summary>이번 턴 추가 피해량.</summary>
        public int AdditionalDamage { get; }

        /// <summary>이번 턴 추가 방어도.</summary>
        public int AdditionalBlock { get; }

        /// <summary>이번 턴 추가 회복량.</summary>
        public int HealAmount { get; }

        /// <summary>이번 턴 적에게 부여할 상태이상 목록(종류 + 스택).</summary>
        public IReadOnlyList<StatusEffectRequest> StatusEffectsToApply { get; }

        /// <summary>발동한 유물 요약(UI/로그 표시용).</summary>
        public string ActivationSummary { get; }
    }
}
