using System;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 플레이어 활력(체력/보너스) 스냅샷. 불변 값 타입이라 보상 적용을 부수효과 없이 계산할 수 있습니다.
    /// </summary>
    public readonly struct RunVitals
    {
        public RunVitals(int maxHp, int currentHp, int damageBonus, int defenseBonus)
        {
            MaxHp = maxHp;
            CurrentHp = currentHp;
            DamageBonus = damageBonus;
            DefenseBonus = defenseBonus;
        }

        public int MaxHp { get; }
        public int CurrentHp { get; }
        public int DamageBonus { get; }
        public int DefenseBonus { get; }

        public RunVitals WithHeal(int amount) =>
            new(MaxHp, Math.Min(CurrentHp + amount, MaxHp), DamageBonus, DefenseBonus);

        public RunVitals WithFullHeal() =>
            new(MaxHp, MaxHp, DamageBonus, DefenseBonus);

        public RunVitals WithDamageBonus(int step) =>
            new(MaxHp, CurrentHp, DamageBonus + step, DefenseBonus);

        public RunVitals WithDefenseBonus(int step) =>
            new(MaxHp, CurrentHp, DamageBonus, DefenseBonus + step);

        public RunVitals WithMaxHpUp(int amount) =>
            new(MaxHp + amount, Math.Min(CurrentHp + amount, MaxHp + amount), DamageBonus, DefenseBonus);
    }

    /// <summary>
    /// 보상 타입별 수치 규칙(회복량/보너스 증가량)을 한곳에 모은 순수 계산기입니다.
    /// 출시 후 밸런싱은 여기 상수만 조정하면 되고, 상태 보관/적용 시점은 GameFlowSession이 담당합니다.
    /// 순수 함수라 단위 테스트로 각 보상 결과를 검증할 수 있습니다.
    /// </summary>
    public static class RewardEconomy
    {
        /// <summary>일반 전투 승리 시 자동 회복량. (엘리트/보스는 유물로만 보상)</summary>
        public const int NormalWinHeal = 4;

        public const int HealAmount = 8;
        public const int BigHealAmount = 16;
        public const int DamageBonusStep = 2;
        public const int GreaterDamageStep = 4;
        public const int DefenseBonusStep = 2;
        public const int GreaterDefenseStep = 4;
        public const int MaxHpUpAmount = 5;

        public static RunVitals Apply(RunVitals vitals, RunRewardType rewardType)
        {
            return rewardType switch
            {
                RunRewardType.Heal => vitals.WithHeal(HealAmount),
                RunRewardType.DamageBonus => vitals.WithDamageBonus(DamageBonusStep),
                RunRewardType.DefenseBonus => vitals.WithDefenseBonus(DefenseBonusStep),
                RunRewardType.MaxHpUp => vitals.WithMaxHpUp(MaxHpUpAmount),
                RunRewardType.BigHeal => vitals.WithHeal(BigHealAmount),
                RunRewardType.GreaterDamage => vitals.WithDamageBonus(GreaterDamageStep),
                RunRewardType.GreaterDefense => vitals.WithDefenseBonus(GreaterDefenseStep),
                RunRewardType.FullHeal => vitals.WithFullHeal(),
                _ => vitals,
            };
        }
    }
}
