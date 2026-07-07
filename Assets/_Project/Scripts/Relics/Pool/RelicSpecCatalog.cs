using System.Collections.Generic;
using SlotRogue.Slot.Data;
using S = SlotRogue.Slot.Data.SlotSymbolType;

namespace SlotRogue.Relics.Pool
{
    /// <summary>
    /// 유물(별조각 상점) v29 기획표(slot_roguelite_relics_spec_v29) 41종을 부품 조합(<see cref="RelicSpec"/>)으로
    /// 정의하는 카탈로그. 숫자·조건은 데이터, 행동은 트리거별 핸들러가 담당한다(핸들러는 P1에서 배선).
    /// 순수 조합이 안 되는(복합·동적 대상) 소수는 <see cref="RelicEffectKind.SpecialRule"/> + 전용 핸들러(id 키)로 처리한다.
    /// v29엔 시작(Starter) 등급이 없다 — 전부 별조각 상점에서 구매하는 이번 런 전용 파츠.
    /// 수명: perm=상시 / euse=소멸·횟수(ConsumableUses) / ewave=소멸·웨이브(ConsumableWaves).
    /// </summary>
    public static class RelicSpecCatalog
    {
        public static IReadOnlyList<RelicSpec> All { get; } = new[]
        {
            // ── 재발동(retrig) ──────────────────────────────────────────
            R("R-01", "앙코르", "전투마다 한 번, 이번 스핀 최고 족보를 한 번 더 터뜨린다.",
              RelicGrade.Common, "retrig", 3, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.RetriggerHighestPattern)),
              lifetime: Life(RelicLifetimeKind.OncePerBattle, 1), devNote: "최고 족보 재발동(전투당 1회)."),
            R("R-02", "낙인술사", "지금 가장 센 심볼이 나올 때 가끔 [다시] 표식을 달고 나온다 — 그 심볼로 족보를 맞추면 한 번 더 터진다!",
              RelicGrade.Uncommon, "retrig", 4, 1,
              RelicTrigger.OnSpinGenerate, Fx(E(RelicEffectKind.SpecialRule, 0.25f, sr: "R-02")),
              devNote: "현재 최고 기본값 심볼 등장 시 25% [다시]. 동적 대상이라 전용 핸들러."),
            R("R-03", "고장난 계산기", "버튼을 누르면 이번 스핀 최고 족보를 한 번 더 터뜨린다. 3번 쓰면 사라진다.",
              RelicGrade.Uncommon, "retrig", 3, 1,
              RelicTrigger.RuleModifier, Fx(E(RelicEffectKind.RetriggerHighestPattern)),
              lifetime: Life(RelicLifetimeKind.ConsumableUses, 3), devNote: "능동 사용(버튼). 소멸·3회."),
            R("R-04", "7의 낙인", "<sprite index=1>세븐은 항상 [다시] 표식을 달고 나온다 — <sprite index=1>세븐 족보는 두 번 터진다!",
              RelicGrade.Rare, "retrig", 6, 1,
              RelicTrigger.OnSpinGenerate, Fx(E(RelicEffectKind.AddAgainMark, 1, s: Sy(S.Seven), chance: 1f)),
              devNote: "7 등장 시 항상 [다시]."),
            R("R-05", "메아리 부적", "자리 바꾸기를 아낀 스핀은 모든 족보가 한 번 더 터진다. (전투당 1회)",
              RelicGrade.Rare, "retrig", 6, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.RetriggerAllPatterns)),
              Cx(C(RelicConditionKind.NoSwapThisSpin)), lifetime: Life(RelicLifetimeKind.OncePerBattle, 1),
              devNote: "no-swap 스핀 전체 재발동(전투 1회)."),
            R("R-06", "다시의 눈", "매 스핀 몇몇 칸에 무작위로 [다시] 표식이 뜬다 — 그 칸이 족보에 들면 한 번 더!",
              RelicGrade.Rare, "retrig", 6, 1,
              RelicTrigger.OnSpinGenerate, Fx(E(RelicEffectKind.AddAgainMark, 1, chance: 0.12f)),
              devNote: "매 스핀 각 칸 12% [다시]."),
            R("R-07", "이중 각인", "지금 가장 센 심볼이 아주 자주 [다시] 표식을 달고 나온다!",
              RelicGrade.Legendary, "retrig", 9, 1,
              RelicTrigger.OnSpinGenerate, Fx(E(RelicEffectKind.SpecialRule, 0.40f, sr: "R-07")),
              devNote: "현재 최고 기본값 심볼 등장 시 40% [다시]. 동적 대상이라 전용 핸들러."),
            R("R-08", "집착", "모든 족보가 매번 두 번 터진다! 대신 몬스터가 강해지고, 별조각을 덜 받는다.",
              RelicGrade.Curse, "retrig", 3, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.SpecialRule, sr: "R-08")),
              devNote: "전 족보 재발동 / 몬스터 HP+25%, 별조각 -1. 다중 훅이라 전용 핸들러."),
            R("R-40", "빌린 재발동", "다음 3웨이브 동안 최고 족보를 한 번 더 터뜨린다. 그 뒤 사라진다.",
              RelicGrade.Rare, "retrig", 4, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.RetriggerHighestPattern)),
              lifetime: Life(RelicLifetimeKind.ConsumableWaves, 3), devNote: "소멸·3웨이브: 최고 족보 재발동."),

            // ── 배율(mult) ──────────────────────────────────────────────
            R("R-09", "체리 증폭기", "<sprite index=0>체리가 들어간 족보가 더 세게 터진다.",
              RelicGrade.Common, "mult", 2, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.ComboMultAdd, 0.3f)),
              Cx(C(RelicConditionKind.PatternContainsSymbol, s: Sy(S.Cherry)), C(RelicConditionKind.PatternSizeAtLeast, 3)),
              devNote: "체리 3개↑ → Mult +0.3."),
            R("R-10", "세븐 뇌관", "<sprite index=1>세븐이 들어간 족보가 훨씬 세게 터진다.",
              RelicGrade.Uncommon, "mult", 4, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.ComboMultAdd, 0.6f)),
              Cx(C(RelicConditionKind.PatternContainsSymbol, s: Sy(S.Seven)), C(RelicConditionKind.PatternSizeAtLeast, 3)),
              devNote: "7 3개↑ → Mult +0.6."),
            R("R-11", "패턴 학자", "5개 이상 맞춘 큰 족보가 더 세게 터진다.",
              RelicGrade.Uncommon, "mult", 4, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.ComboMultAdd, 0.8f)),
              Cx(C(RelicConditionKind.PatternSizeAtLeast, 5)), devNote: "5개↑ 족보 → Mult +0.8."),
            R("R-12", "세븐 과충전", "<sprite index=1>세븐이 들어간 족보의 피해가 1.5배!",
              RelicGrade.Rare, "mult", 6, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.SpecialMultTimes, 1.5f)),
              Cx(C(RelicConditionKind.PatternContainsSymbol, s: Sy(S.Seven))), devNote: "7 포함 족보 → 특수배율 ×1.5."),
            R("R-13", "잭팟 코어", "한 줄을 같은 심볼로 꽉 채우면 피해가 3배!",
              RelicGrade.Rare, "mult", 6, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FinalMultTimes, 3f)),
              Cx(C(RelicConditionKind.WholeLineSameSymbol)), devNote: "한 줄 5칸 동일 → ×3."),
            R("R-14", "콤보 왕관", "한 번에 여러 족보를 터뜨릴수록 피해가 더 크게 불어난다.",
              RelicGrade.Legendary, "mult", 9, 1,
              RelicTrigger.RuleModifier, Fx(E(RelicEffectKind.SpecialRule, sr: "R-14")),
              devNote: "콤보배율 상한 ×5→×7, 3개↑면 +1. 상한 변경+다중족보라 전용 핸들러."),
            R("R-15", "불안정 코어", "모든 족보가 더 세게 터진다. 대신 매 스핀 칸 하나가 꽝이 된다.",
              RelicGrade.Curse, "mult", 4, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.SpecialRule, sr: "R-15")),
              devNote: "전 족보 Mult +1 / 매 스핀 랜덤 1칸 꽝. 다중 훅이라 전용 핸들러."),
            R("R-39", "폭죽 다발", "다음 2웨이브 동안 모든 족보가 세게 터진다. 그 뒤 사라진다.",
              RelicGrade.Uncommon, "mult", 3, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 8f)),
              lifetime: Life(RelicLifetimeKind.ConsumableWaves, 2), devNote: "소멸·2웨이브: 모든 족보 피해 +8."),

            // ── 스왑(swap) ──────────────────────────────────────────────
            R("R-16", "교환 장갑", "자리 바꾸기를 쓴 스핀은 피해가 오른다.",
              RelicGrade.Common, "swap", 2, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 4f)),
              Cx(C(RelicConditionKind.SwapUsedThisSpin)), devNote: "swap 쓴 스핀 합산 피해 +4."),
            R("R-17", "스왑 장인", "자리 바꾸기를 웨이브마다 한 번 더 쓸 수 있다.",
              RelicGrade.Uncommon, "swap", 4, 1,
              RelicTrigger.RuleModifier, Fx(E(RelicEffectKind.SwapCountDelta, 1)), devNote: "넛지(swap) +1회."),
            R("R-18", "연쇄 교환", "자리 바꾸기로 새 족보를 완성하면, 그 스핀 최고 족보가 한 번 더 터진다!",
              RelicGrade.Rare, "swap", 6, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.RetriggerHighestPattern)),
              Cx(C(RelicConditionKind.PatternMadeBySwap)), devNote: "swap으로 새 족보 완성 시 최고 족보 재발동."),
            R("R-19", "시간의 여유", "자리 바꾸기를 한 번 더. 게다가 전투마다 한 번은 자리를 바꿔도 별조각을 잃지 않는다.",
              RelicGrade.Legendary, "swap", 9, 1,
              RelicTrigger.RuleModifier, Fx(E(RelicEffectKind.SwapCountDelta, 1), E(RelicEffectKind.SpecialRule, sr: "R-19")),
              devNote: "swap +1회, 전투당 1회 swap 별조각 유지. 첫 스왑 면제는 전용 핸들러."),
            R("R-20", "강제 교환", "매 스핀 자동으로 자리 바꾸기가 한 번 일어난다. 대신 그 스핀 피해가 크게 오른다.",
              RelicGrade.Curse, "swap", 3, 1,
              RelicTrigger.OnSpinGenerate, Fx(E(RelicEffectKind.SpecialRule, sr: "R-20")),
              devNote: "매 스핀 강제 swap / 그 스핀 피해 +30%. 자동 스왑+결합 배율이라 전용 핸들러."),

            // ── 경제/보유형(econ) ───────────────────────────────────────
            R("R-21", "별가루 주머니", "전투를 시작할 때 별조각 하나를 받는다.",
              RelicGrade.Common, "econ", 2, 1,
              RelicTrigger.OnBattleStart, Fx(E(RelicEffectKind.GainCoins, 1)), devNote: "전투 시작 별조각 +1(전투당 1회)."),
            R("R-22", "별 수집가", "자리 바꾸기 없이 이기면 별조각을 하나 더 받는다.",
              RelicGrade.Uncommon, "econ", 4, 1,
              RelicTrigger.OnKill, Fx(E(RelicEffectKind.GainCoins, 1)),
              Cx(C(RelicConditionKind.NoSwapThisBattle)), devNote: "no-swap 승리 시 별조각 +1."),
            R("R-23", "저축가의 반지", "별조각을 5개 넘게 갖고 있으면 계속 더 세게 때린다.",
              RelicGrade.Rare, "econ", 6, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 2f)),
              Cx(C(RelicConditionKind.CoinsAtLeast, 5)), devNote: "보유 별조각 5+면 합산 피해 +2."),
            R("R-24", "별핵", "별조각을 6개 넘게 갖고 있으면 모든 심볼이 강해진다.",
              RelicGrade.Rare, "econ", 6, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.SpecialRule, sr: "R-24")),
              Cx(C(RelicConditionKind.CoinsAtLeast, 6)), devNote: "보유 별조각 6+면 모든 심볼 효과 +1. 조건부 전 심볼 강화라 전용 핸들러."),
            R("R-25", "은하 금고", "별조각을 벌 때마다 가장 많이 쓴 심볼이 강해진다.",
              RelicGrade.Legendary, "econ", 9, 1,
              RelicTrigger.OnCoinGain, Fx(E(RelicEffectKind.SpecialRule, sr: "R-25")),
              lifetime: Life(RelicLifetimeKind.OncePerBattle, 1), devNote: "별조각 획득 시 최다 심볼 효과 +1(전투당 1회). 동적 대상."),
            R("R-26", "검은 별조각", "자리 바꾸기 없이 별조각을 벌면 하나 더 받는다. 대신 체력을 1 잃는다.",
              RelicGrade.Curse, "econ", 2, 1,
              RelicTrigger.OnCoinGain, Fx(E(RelicEffectKind.GainCoins, 1), E(RelicEffectKind.PayHp, 1)),
              Cx(C(RelicConditionKind.NoSwapThisBattle)), devNote: "no-swap 별조각 시 +1 / HP -1."),
            R("R-41", "한 줌의 별", "이번 웨이브만 별조각을 더 준다. 그 뒤 사라진다.",
              RelicGrade.Common, "econ", 1, 1,
              RelicTrigger.OnCoinGain, Fx(E(RelicEffectKind.SpecialRule, 2f, sr: "R-41")),
              lifetime: Life(RelicLifetimeKind.ConsumableWaves, 1), devNote: "소멸·1웨이브: 별조각 획득 +2. 획득량 가산(재귀 주의)이라 전용 핸들러."),

            // ── 상점/리롤(shop) ────────────────────────────────────────
            R("R-27", "낡은 할인권", "상점에서 첫 구매가 1 싸진다.",
              RelicGrade.Common, "shop", 2, 1,
              RelicTrigger.RuleModifier, Fx(E(RelicEffectKind.ShopDiscount, 1f, 1f)), devNote: "상점 첫 구매 -1(최소1)."),
            R("R-28", "단골 카드", "상점의 모든 유물이 1씩 싸진다.",
              RelicGrade.Uncommon, "shop", 4, 1,
              RelicTrigger.RuleModifier, Fx(E(RelicEffectKind.ShopDiscount, 1f)), devNote: "상점 유물 가격 전부 -1(최소1)."),
            R("R-29", "상인의 눈", "상점에 유물이 하나 더 보인다. 안 사고 나가면 다음에 더 싸게.",
              RelicGrade.Rare, "shop", 6, 1,
              RelicTrigger.RuleModifier, Fx(E(RelicEffectKind.ShopOfferDelta, 1), E(RelicEffectKind.SpecialRule, sr: "R-29")),
              devNote: "선택지 +1, 미구매 시 다음 첫 구매 -1. 미구매 조건은 전용 핸들러."),
            R("R-30", "VIP 상점권", "상점에 유물이 두 개 더 보인다.",
              RelicGrade.Legendary, "shop", 9, 1,
              RelicTrigger.RuleModifier, Fx(E(RelicEffectKind.ShopOfferDelta, 2)), devNote: "상점 선택지 +2."),
            R("R-31", "부채 장부", "돈이 없어도 외상으로 살 수 있다. 나중에 별조각 4개로 갚는다. (한 번 쓰면 사라짐)",
              RelicGrade.Curse, "shop", 1, 1,
              RelicTrigger.RuleModifier, Fx(E(RelicEffectKind.SpecialRule, 4f, sr: "R-31")),
              lifetime: Life(RelicLifetimeKind.ConsumableUses, 1), devNote: "소멸·1회: 외상 구매 / 별조각 4 자동 차감."),

            // ── 전투/생존(combat) ──────────────────────────────────────
            R("R-32", "작은 망원경", "매 전투 첫 스핀이 더 강하게 터진다.",
              RelicGrade.Common, "combat", 2, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 2f)),
              Cx(C(RelicConditionKind.IsFirstSpinOfBattle)), devNote: "첫 스핀 합산 피해 +2."),
            R("R-33", "응급 붕대", "몬스터를 잡을 때마다 체력을 조금 회복한다.",
              RelicGrade.Common, "combat", 2, 1,
              RelicTrigger.OnKill, Fx(E(RelicEffectKind.Heal, 2)), devNote: "처치 시 HP +2."),
            R("R-34", "마지막 탄환", "5턴째 스핀이 강하게 터진다.",
              RelicGrade.Uncommon, "combat", 4, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 7f)),
              Cx(C(RelicConditionKind.TurnIndexEquals, 5)), devNote: "5턴째 합산 피해 +7."),
            R("R-35", "폭주 기관차", "오래 못 잡을수록 5턴째 한 방이 커진다.",
              RelicGrade.Rare, "combat", 6, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 12f)),
              Cx(C(RelicConditionKind.TurnIndexEquals, 5)), devNote: "4턴째까지 못 잡으면(=5턴 도달) 5턴째 피해 +12."),
            R("R-36", "부활의 부적", "죽을 뻔하면 체력 1로 버틴다. 2번 버티면 사라진다.",
              RelicGrade.Rare, "combat", 5, 1,
              RelicTrigger.OnLethalDamage, Fx(E(RelicEffectKind.SurviveLethal, 1)),
              lifetime: Life(RelicLifetimeKind.ConsumableUses, 2), devNote: "소멸·2회: 치명 피해 시 HP1 생존."),
            R("R-37", "불사의 별", "전투마다 한 번, 죽을 피해를 무효로 하고 체력을 회복한다.",
              RelicGrade.Legendary, "combat", 9, 1,
              RelicTrigger.OnLethalDamage, Fx(E(RelicEffectKind.SurviveLethal, 0f, 5f)),
              lifetime: Life(RelicLifetimeKind.OncePerBattle, 1), devNote: "전투당 1회 치명 무효 + HP +5."),
            R("R-38", "유리 대포", "내 피해가 크게 오른다. 대신 받는 피해도 커진다.",
              RelicGrade.Curse, "combat", 3, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.SpecialMultTimes, 1.4f), E(RelicEffectKind.IncomingDamageMul, 1.3f)),
              devNote: "모든 피해 +40% / 받는 피해 +30%."),

            // ── 엔진 확장(클로버핏 착안, 순수 조합) ─────────────────────
            R("R-42", "콤보 왕관", "한 번에 족보를 3개 이상 터뜨리면 그 스핀 피해가 1.5배!",
              RelicGrade.Rare, "mult", 6, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.SpecialMultTimes, 1.5f)),
              Cx(C(RelicConditionKind.ActivePatternCountAtLeast, 3)), devNote: "Pentacle 착안: 족보 3개↑ → 특수배율 ×1.5."),
            R("R-43", "과일 바구니", "<sprite index=0>체리·<sprite index=5>레몬이 들어간 족보가 더 세게 터진다.",
              RelicGrade.Common, "mult", 3, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.ComboMultAdd, 0.4f)),
              Cx(C(RelicConditionKind.PatternContainsSymbol, s: Sy(S.Cherry, S.Lemon))), devNote: "Fruit Basket 착안: 과일 족보 배율 +0.4."),
            R("R-44", "큰 손", "5칸을 꽉 채운 큰 족보의 피해가 1.5배!",
              RelicGrade.Uncommon, "mult", 4, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.SpecialMultTimes, 1.5f)),
              Cx(C(RelicConditionKind.PatternSizeAtLeast, 5)), devNote: "큰 족보 보상: 5칸 → 특수배율 ×1.5."),
            R("R-45", "최후의 불꽃", "5턴째 스핀의 피해가 2배로 터진다!",
              RelicGrade.Rare, "combat", 6, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.SpecialMultTimes, 2f)),
              Cx(C(RelicConditionKind.TurnIndexEquals, 5)), devNote: "막턴 역전: 5턴째 특수배율 ×2."),
            R("R-46", "무결의 일격", "자리 바꾸기를 아낀 스핀은 더 세게 터진다.",
              RelicGrade.Uncommon, "swap", 4, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.ComboMultAdd, 0.5f)),
              Cx(C(RelicConditionKind.NoSwapThisSpin)), devNote: "무스왑 스핀 콤보배율 +0.5."),
            R("R-47", "재정비", "한 번에 족보를 4개 이상 터뜨리면 체력을 2 회복한다.",
              RelicGrade.Common, "combat", 3, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.Heal, 2f)),
              Cx(C(RelicConditionKind.ActivePatternCountAtLeast, 4)), devNote: "콤보 회복: 족보 4개↑ → HP +2."),
            R("R-48", "저축 왕", "별조각을 8개 넘게 모으면 피해가 1.3배!",
              RelicGrade.Rare, "econ", 6, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.SpecialMultTimes, 1.3f)),
              Cx(C(RelicConditionKind.CoinsAtLeast, 8)), devNote: "Lost Wallet 착안: 별조각 8+ → 특수배율 ×1.3."),
            R("R-49", "첫 끗발", "전투 첫 스핀의 피해가 1.5배!",
              RelicGrade.Common, "combat", 3, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.SpecialMultTimes, 1.5f)),
              Cx(C(RelicConditionKind.IsFirstSpinOfBattle)), devNote: "선공 보정: 첫 스핀 특수배율 ×1.5."),
            R("R-50", "다이아 세공사", "<sprite index=2>다이아가 들어간 족보의 피해가 1.4배!",
              RelicGrade.Rare, "mult", 6, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.SpecialMultTimes, 1.4f)),
              Cx(C(RelicConditionKind.PatternContainsSymbol, s: Sy(S.Diamond))), devNote: "다이아 족보 특수배율 ×1.4."),
            R("R-51", "종·클로버 공명", "<sprite index=3>종·<sprite index=4>클로버 족보가 더 세게 터진다.",
              RelicGrade.Common, "mult", 3, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.ComboMultAdd, 0.3f)),
              Cx(C(RelicConditionKind.PatternContainsSymbol, s: Sy(S.Bell, S.Clover))), devNote: "종·클로버 족보 배율 +0.3."),
            R("R-52", "완벽한 정렬", "한 줄을 같은 심볼로 꽉 채우면, 최고 족보를 한 번 더 터뜨린다!",
              RelicGrade.Rare, "retrig", 6, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.RetriggerHighestPattern)),
              Cx(C(RelicConditionKind.WholeLineSameSymbol)), devNote: "한 줄 대박 → 최고 족보 재발동."),
            R("R-53", "막판 뒤집기", "5턴째엔 모든 족보를 한 번 더 터뜨린다!",
              RelicGrade.Rare, "retrig", 6, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.RetriggerAllPatterns)),
              Cx(C(RelicConditionKind.TurnIndexEquals, 5)), devNote: "5턴째 전 족보 재발동(막턴 역전)."),
            R("R-54", "일확천금", "별조각을 10개 넘게 모으면 최종 피해가 1.5배!",
              RelicGrade.Rare, "econ", 6, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FinalMultTimes, 1.5f)),
              Cx(C(RelicConditionKind.CoinsAtLeast, 10)), devNote: "별조각 10+ → 최종 배율 ×1.5."),
            R("R-55", "큰 거 한 방", "5칸을 꽉 채운 족보에 피해가 크게 더해진다.",
              RelicGrade.Uncommon, "combat", 4, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 8f)),
              Cx(C(RelicConditionKind.PatternSizeAtLeast, 5)), devNote: "5칸 족보 합산 피해 +8."),
        };

        private static readonly Dictionary<string, RelicSpec> ById = BuildIndex(All);

        /// <summary>id로 유물 명세 조회. 없으면 null.</summary>
        public static RelicSpec GetById(string id) =>
            !string.IsNullOrEmpty(id) && ById.TryGetValue(id, out RelicSpec spec) ? spec : null;

        // ── 제안(처치 보상) 엔진 효과 스펙 — 제안 카탈로그의 P-id와 매칭 ──────────
        // 제안은 픽하면 영구 누적되어 유물과 함께 전투 엔진이 소비한다(등급/가격은 미사용).
        public static IReadOnlyList<RelicSpec> Proposals { get; } = new[]
        {
            R("P-07", "3연격 훈련", "", RelicGrade.Common, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 1f)),
              Cx(C(RelicConditionKind.PatternSizeEquals, 3))),
            R("P-21", "4연격 훈련", "", RelicGrade.Uncommon, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 3f)),
              Cx(C(RelicConditionKind.PatternSizeEquals, 4))),
            R("P-32", "완성된 문양", "", RelicGrade.Rare, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 8f)),
              Cx(C(RelicConditionKind.PatternSizeEquals, 5))),
            R("P-33", "문양 공명", "", RelicGrade.Rare, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 5f)),
              Cx(C(RelicConditionKind.ActivePatternCountAtLeast, 3))),
            R("P-41", "별의 축복", "", RelicGrade.Legendary, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 3f)),
              Cx(C(RelicConditionKind.PatternSizeEquals, 3))),
            R("P-35", "막타 정산", "", RelicGrade.Rare, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.RetriggerHighestPattern)),
              Cx(C(RelicConditionKind.TurnIndexEquals, 5))),
            R("P-42", "황금 손", "", RelicGrade.Legendary, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.RetriggerAllPatterns)),
              Cx(C(RelicConditionKind.IsFirstSpinOfBattle))),
            R("P-12", "첫 수의 감각", "", RelicGrade.Common, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 2f)),
              Cx(C(RelicConditionKind.IsFirstSpinOfBattle))),
            R("P-13", "절제 보상", "", RelicGrade.Common, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 1f)),
              Cx(C(RelicConditionKind.NoSwapThisSpin))),
            R("P-22", "연쇄 계산", "", RelicGrade.Uncommon, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 2f)),
              Cx(C(RelicConditionKind.ActivePatternCountAtLeast, 2))),
            R("P-24", "교환 타격", "", RelicGrade.Uncommon, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 2f)),
              Cx(C(RelicConditionKind.SwapUsedThisSpin))),
            R("P-26", "마무리 본능", "", RelicGrade.Uncommon, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 5f)),
              Cx(C(RelicConditionKind.TurnIndexEquals, 5))),
            R("P-36", "저축 습관", "", RelicGrade.Rare, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.FlatDamageAdd, 2f)),
              Cx(C(RelicConditionKind.CoinsAtLeast, 5))),
            R("P-37", "최후의 별빛", "", RelicGrade.Rare, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.SpecialMultTimes, 2f)),
              Cx(C(RelicConditionKind.TurnIndexEquals, 5))),

            // ── 속성(상태이상) 제안 ─────────────────────────────────────
            R("P-14", "붉은 성냥", "", RelicGrade.Common, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.ApplyBurn, 2f)),
              Cx(C(RelicConditionKind.PatternContainsSymbol, s: Sy(S.Cherry, S.Lemon)),
                 C(RelicConditionKind.PatternSizeAtLeast, 4))),
            R("P-15", "푸른 감염가루", "", RelicGrade.Common, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.ApplyInfection, 3f)),
              Cx(C(RelicConditionKind.PatternContainsSymbol, s: Sy(S.Clover, S.Diamond)),
                 C(RelicConditionKind.PatternSizeAtLeast, 4))),
            R("P-16", "균열음", "", RelicGrade.Common, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.ApplyVulnerable, 1f)),
              Cx(C(RelicConditionKind.PatternContainsSymbol, s: Sy(S.Bell)),
                 C(RelicConditionKind.PatternSizeAtLeast, 3))),
            R("P-28", "가시 잎사귀", "", RelicGrade.Uncommon, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.GainThorns, 3f)),
              Cx(C(RelicConditionKind.PatternContainsSymbol, s: Sy(S.Clover)),
                 C(RelicConditionKind.PatternSizeAtLeast, 3))),
            R("P-30", "약화 분말", "", RelicGrade.Uncommon, "proposal", 0, 1,
              RelicTrigger.OnDamageResolve, Fx(E(RelicEffectKind.ApplyWeaken, 1f)),
              Cx(C(RelicConditionKind.PatternContainsSymbol, s: Sy(S.Bell, S.Clover)),
                 C(RelicConditionKind.PatternSizeAtLeast, 3))),
        };

        private static readonly Dictionary<string, RelicSpec> ProposalById = BuildIndex(Proposals);

        /// <summary>제안 id로 엔진 효과 스펙 조회. 엔진 효과 제안이 아니면 null.</summary>
        public static RelicSpec GetProposalById(string id) =>
            !string.IsNullOrEmpty(id) && ProposalById.TryGetValue(id, out RelicSpec spec) ? spec : null;

        private static Dictionary<string, RelicSpec> BuildIndex(IReadOnlyList<RelicSpec> specs)
        {
            var map = new Dictionary<string, RelicSpec>(specs.Count);
            for (int index = 0; index < specs.Count; index++)
            {
                map[specs[index].Id] = specs[index];
            }

            return map;
        }

        // ── 작성 헬퍼(간결성) ──────────────────────────────────────────
        private static RelicSpec R(
            string id, string name, string desc, RelicGrade grade, string category, int price, int maxCopies,
            RelicTrigger trigger, RelicEffect[] effects, RelicCondition[] conditions = null,
            RelicLifetime lifetime = default, string devNote = null)
            => new(id, name, desc, grade, category, price, maxCopies, iconKey: "",
                trigger, effects, conditions, lifetime, unlock: default, devNote: devNote);

        private static RelicEffect E(
            RelicEffectKind kind, float value1 = 0f, float value2 = 0f,
            S[] s = null, float chance = 1f, string sr = null)
            => new(kind, value1, value2, s, chance, sr);

        private static RelicCondition C(RelicConditionKind kind, int value = 0, S[] s = null)
            => new(kind, value, s);

        private static RelicLifetime Life(RelicLifetimeKind kind, int amount) => new(kind, amount);

        private static RelicEffect[] Fx(params RelicEffect[] effects) => effects;

        private static RelicCondition[] Cx(params RelicCondition[] conditions) => conditions;

        private static S[] Sy(params S[] symbols) => symbols;
    }
}
