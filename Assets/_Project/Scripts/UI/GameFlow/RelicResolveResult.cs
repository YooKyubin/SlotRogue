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
            string activationSummary,
            IReadOnlyList<RelicContributionDelta> contributions = null,
            IReadOnlyList<RelicDerivedHeal> derivedHeals = null)
        {
            AdditionalDamage = additionalDamage;
            AdditionalBlock = additionalBlock;
            HealAmount = healAmount;
            StatusEffectsToApply = statusEffectsToApply ?? System.Array.Empty<StatusEffectRequest>();
            ActivationSummary = activationSummary ?? string.Empty;
            Contributions = contributions ?? System.Array.Empty<RelicContributionDelta>();
            DerivedHeals = derivedHeals ?? System.Array.Empty<RelicDerivedHeal>();
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

        public IReadOnlyList<RelicContributionDelta> Contributions { get; }

        /// <summary>
        /// 이번 턴 "최종 피해/방어가 확정된 뒤"에 회복으로 환산되는 규칙(흡혈/방어전환).
        /// 회복량은 족보 시점이 아니라 최종 피해·방어가 정해진 뒤에야 알 수 있어,
        /// <see cref="CombatTurnRequestBuilder"/>가 마지막에 계산한다.
        /// </summary>
        public IReadOnlyList<RelicDerivedHeal> DerivedHeals { get; }
    }

    /// <summary>파생 회복 종류.</summary>
    public enum RelicDerivedHealKind
    {
        /// <summary>입힌 피해의 일정 비율을 회복(턴당 상한 있음).</summary>
        Lifesteal = 0,

        /// <summary>획득한 방어도의 일정 비율을 회복.</summary>
        BlockToHeal = 1,
    }

    /// <summary>
    /// 최종 피해/방어 확정 후 회복으로 환산할 규칙 1건.
    /// 흡혈은 "입힌 피해 × Percent%"(TurnCap 상한), 방어전환은 "획득 방어도 × Percent%".
    /// </summary>
    public readonly struct RelicDerivedHeal
    {
        public RelicDerivedHeal(
            string relicId,
            string relicName,
            RelicDerivedHealKind kind,
            int percent,
            int turnCap,
            int triggerPatternIndex = -1)
        {
            RelicId = relicId ?? string.Empty;
            RelicName = relicName ?? string.Empty;
            Kind = kind;
            Percent = System.Math.Max(0, percent);
            TurnCap = System.Math.Max(0, turnCap);
            TriggerPatternIndex = triggerPatternIndex;
        }

        public string RelicId { get; }

        public string RelicName { get; }

        public RelicDerivedHealKind Kind { get; }

        public int Percent { get; }

        /// <summary>턴당 회복 상한(0이면 무제한 — 방어전환에 사용).</summary>
        public int TurnCap { get; }

        public int TriggerPatternIndex { get; }
    }
}
