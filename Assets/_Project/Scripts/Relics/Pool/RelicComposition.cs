using System;
using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.Relics.Pool
{
    /// <summary>
    /// 유물을 "발동(Trigger) × 조건(Condition) × 효과(Effect) × 수명(Lifetime)"의 조합으로 표현하는
    /// 부품 시스템 어휘. 데이터(어떤 부품을 어떤 값으로)와 행동(부품별 코드 핸들러)을 분리한다.
    /// 신규 유물 = 이 조합을 채운 데이터 하나. 신규 메커니즘 = enum 값 1개 + 핸들러 1개.
    /// </summary>
    public enum RelicTrigger
    {
        /// <summary>구매/획득 즉시 1회.</summary>
        OnAcquire,

        /// <summary>각 전투 시작 시.</summary>
        OnBattleStart,

        /// <summary>스핀 보드를 만들 때(다시 표식/자동 스왑/꽝 등 보드 변형).</summary>
        OnSpinGenerate,

        /// <summary>족보 → 피해 계산 시(배율/보정).</summary>
        OnDamageResolve,

        /// <summary>몬스터 처치 시.</summary>
        OnKill,

        /// <summary>별조각 획득 시.</summary>
        OnCoinGain,

        /// <summary>죽을 피해를 받을 때.</summary>
        OnLethalDamage,

        /// <summary>상시 규칙/설정 값(스왑 횟수·상점 선택지 등). 이벤트가 아니라 조회형.</summary>
        RuleModifier,
    }

    public enum RelicEffectKind
    {
        None = 0,
        GainCoins,
        PayHp,
        Heal,
        SymbolBaseDelta,
        SymbolWeightDelta,

        /// <summary>발동 족보의 합산 피해에 정수 가산(예: swap 스핀 피해 +4). value1=가산량.</summary>
        FlatDamageAdd,
        ComboMultAdd,
        SpecialMultTimes,
        FinalMultTimes,

        /// <summary>이번 스핀 최고 족보 1개를 한 번 더 발동(재발동은 [다시]를 검사하지 않음).</summary>
        RetriggerHighestPattern,

        /// <summary>이번 스핀에 발동한 모든 족보를 한 번 더 발동.</summary>
        RetriggerAllPatterns,
        AddAgainMark,
        AddBlankCell,
        AutoSwap,
        SwapCountDelta,
        ShopOfferDelta,
        ShopDiscount,
        SurviveLethal,
        IncomingDamageMul,
        EnemyHpMul,

        // ── 상태이상 부여(적에게, 가시만 자신) ─────────────────────
        ApplyBurn,
        ApplyInfection,
        ApplyVulnerable,
        ApplyWeaken,
        GainThorns,

        /// <summary>순수 조합으로 안 되는 특수 규칙. 유물 id로 전용 핸들러가 처리한다(예: R-08/R-14).</summary>
        SpecialRule,
    }

    public enum RelicConditionKind
    {
        None = 0,
        SwapUsedThisSpin,
        NoSwapThisSpin,
        NoSwapThisBattle,
        PatternMadeBySwap,
        PatternSizeEquals,
        PatternSizeAtLeast,
        PatternContainsSymbol,
        WholeLineSameSymbol,
        IsFirstSpinOfBattle,
        TurnIndexEquals,
        CoinsAtLeast,
        ActivePatternCountAtLeast,
    }

    public enum RelicLifetimeKind
    {
        Permanent = 0,
        ConsumableUses,
        ConsumableWaves,
        OncePerBattle,
    }

    /// <summary>효과 부품 하나. 값·심볼 필터·확률을 데이터로 담는다.</summary>
    public readonly struct RelicEffect
    {
        public RelicEffect(
            RelicEffectKind kind,
            float value1 = 0f,
            float value2 = 0f,
            IReadOnlyList<SlotSymbolType> symbols = null,
            float chance = 1f,
            string specialRuleId = null)
        {
            Kind = kind;
            Value1 = value1;
            Value2 = value2;
            Chance = chance;
            SpecialRuleId = specialRuleId ?? string.Empty;
            Symbols = CopySymbols(symbols);
        }

        public RelicEffectKind Kind { get; }
        public float Value1 { get; }
        public float Value2 { get; }

        /// <summary>0~1 발동 확률(1이면 항상). AddAgainMark 등 확률 효과용.</summary>
        public float Chance { get; }

        /// <summary><see cref="RelicEffectKind.SpecialRule"/>일 때 전용 핸들러를 찾는 키.</summary>
        public string SpecialRuleId { get; }

        public IReadOnlyList<SlotSymbolType> Symbols { get; }

        private static IReadOnlyList<SlotSymbolType> CopySymbols(IReadOnlyList<SlotSymbolType> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<SlotSymbolType>();
            }

            var copy = new SlotSymbolType[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }
    }

    /// <summary>발동 조건 부품 하나. 모두 통과해야 효과가 적용된다(AND).</summary>
    public readonly struct RelicCondition
    {
        public RelicCondition(
            RelicConditionKind kind,
            int value = 0,
            IReadOnlyList<SlotSymbolType> symbols = null)
        {
            Kind = kind;
            Value = value;
            Symbols = CopySymbols(symbols);
        }

        public RelicConditionKind Kind { get; }
        public int Value { get; }
        public IReadOnlyList<SlotSymbolType> Symbols { get; }

        private static IReadOnlyList<SlotSymbolType> CopySymbols(IReadOnlyList<SlotSymbolType> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<SlotSymbolType>();
            }

            var copy = new SlotSymbolType[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }
    }

    /// <summary>수명 규칙. 소모형은 <see cref="Amount"/>가 사용/웨이브 횟수.</summary>
    public readonly struct RelicLifetime
    {
        public static readonly RelicLifetime Permanent =
            new(RelicLifetimeKind.Permanent, 0);

        public RelicLifetime(RelicLifetimeKind kind, int amount)
        {
            Kind = kind;
            Amount = Math.Max(0, amount);
        }

        public RelicLifetimeKind Kind { get; }
        public int Amount { get; }
    }

    /// <summary>
    /// 유물 해금 조건 종류. 해금 "여부" 상태(저장)와 별개로, "어떻게 해금되는가"는 콘텐츠 정의다.
    /// 해금 시스템 도입 전에는 모든 유물이 <see cref="AlwaysUnlocked"/>.
    /// </summary>
    public enum RelicUnlockKind
    {
        /// <summary>처음부터 해금.</summary>
        AlwaysUnlocked = 0,

        /// <summary>특정 도전과제 달성 시(<see cref="RelicUnlock.Id"/> = 도전과제 id).</summary>
        Achievement,

        /// <summary>한 런에서 특정 웨이브 도달 시(<see cref="RelicUnlock.Value"/> = 웨이브).</summary>
        ReachWave,

        /// <summary>누적 처치 수 도달 시(<see cref="RelicUnlock.Value"/> = 킬 수).</summary>
        TotalKills,

        /// <summary>누적 런 완료 수 도달 시(<see cref="RelicUnlock.Value"/> = 런 수).</summary>
        RunsCompleted,
    }

    /// <summary>해금 조건 데이터. 기본은 처음부터 해금.</summary>
    public readonly struct RelicUnlock
    {
        public static readonly RelicUnlock Always =
            new(RelicUnlockKind.AlwaysUnlocked, 0, null);

        public RelicUnlock(RelicUnlockKind kind, int value = 0, string id = null)
        {
            Kind = kind;
            Value = value;
            Id = id ?? string.Empty;
        }

        public RelicUnlockKind Kind { get; }
        public int Value { get; }

        /// <summary>도전과제 id 등 문자열 키.</summary>
        public string Id { get; }
    }

    /// <summary>
    /// 유물 하나의 조합 명세(저장 형식 무관). 코드 카탈로그든 SO든 이 형태로 수렴시켜
    /// 효과 시스템이 단일 계약으로 소비한다.
    /// </summary>
    public sealed class RelicSpec
    {
        private readonly RelicEffect[] _effects;
        private readonly RelicCondition[] _conditions;

        public RelicSpec(
            string id,
            string displayName,
            string description,
            RelicGrade grade,
            string category,
            int price,
            int maxCopies,
            string iconKey,
            RelicTrigger trigger,
            IReadOnlyList<RelicEffect> effects,
            IReadOnlyList<RelicCondition> conditions = null,
            RelicLifetime lifetime = default,
            RelicUnlock unlock = default,
            string devNote = null)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            Grade = grade;
            Category = category ?? string.Empty;
            Price = Math.Max(0, price);
            MaxCopies = Math.Max(0, maxCopies);
            IconKey = iconKey ?? string.Empty;
            Trigger = trigger;
            Lifetime = lifetime.Kind == RelicLifetimeKind.Permanent && lifetime.Amount == 0
                ? RelicLifetime.Permanent
                : lifetime;
            Unlock = unlock;
            DevNote = devNote ?? string.Empty;
            _effects = Copy(effects);
            _conditions = Copy(conditions);
        }

        public string Id { get; }
        public string DisplayName { get; }

        /// <summary>플레이어용 설명. 개발 메모(<see cref="DevNote"/>)와 분리한다.</summary>
        public string Description { get; }

        public RelicGrade Grade { get; }
        public string Category { get; }
        public int Price { get; }
        public int MaxCopies { get; }
        public string IconKey { get; }
        public RelicTrigger Trigger { get; }
        public RelicLifetime Lifetime { get; }

        /// <summary>해금 조건(콘텐츠). 해금 여부 상태(저장)와는 별개.</summary>
        public RelicUnlock Unlock { get; }

        /// <summary>빌드에 노출하지 않는 설계/QA 메모.</summary>
        public string DevNote { get; }

        public IReadOnlyList<RelicEffect> Effects => _effects;
        public IReadOnlyList<RelicCondition> Conditions => _conditions;

        private static RelicEffect[] Copy(IReadOnlyList<RelicEffect> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<RelicEffect>();
            }

            var copy = new RelicEffect[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }

        private static RelicCondition[] Copy(IReadOnlyList<RelicCondition> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<RelicCondition>();
            }

            var copy = new RelicCondition[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }
    }
}
