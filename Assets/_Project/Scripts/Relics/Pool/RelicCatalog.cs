using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.Relics.Pool
{
    /// <summary>
    /// v23.0 유물 풀 — 코드 카탈로그(80종). HTML(relic_pool_v23_status_balance_patch)의
    /// ID, 이름, 조건, 효과 수치를 런타임 필드로 옮긴 단일 출처다.
    /// 확률군과 상세 설계 의도는 기획 원본에서 관리한다.
    ///
    /// "카탈로그 등록"과 "보상풀 등장"은 분리한다: 모든 80종은 <see cref="All"/>에 있지만,
    /// Phase 1 보상풀(<see cref="RewardPool"/>)·시작 선택지(<see cref="Starters"/>)에는
    /// <see cref="RelicDefinition.Phase1"/>인 것만 등장한다.
    ///
    /// 상태이상 유물은 전투 코어 연결 범위가 확정될 때까지 보상풀에서 제외한다.
    /// 상태이상 계열은 카탈로그에 등록하되 전투 담당 구현이 정합해질 때까지 Phase1=false로 제외한다.
    /// </summary>
    public static class RelicCatalog
    {
        private static readonly RelicDefinition[] AllRelics = BuildAll();
        private static readonly Dictionary<string, RelicDefinition> ById = BuildIndex(AllRelics);

        /// <summary>카탈로그 전체(80종, 미구현 포함).</summary>
        public static IReadOnlyList<RelicDefinition> All => AllRelics;

        /// <summary>시작 유물 선택지(grade=Starter, Phase 1).</summary>
        public static IReadOnlyList<RelicDefinition> Starters { get; } = Filter(AllRelics, r => r.IsStarter && r.Phase1);

        /// <summary>보상 유물 풀(시작 유물 제외, Phase 1 구현분만).</summary>
        public static IReadOnlyList<RelicDefinition> RewardPool { get; } =
            Filter(AllRelics, r => !r.IsStarter && r.Phase1);

        public static RelicDefinition GetById(string id) =>
            !string.IsNullOrEmpty(id) && ById.TryGetValue(id, out RelicDefinition relic) ? relic : null;

        /// <summary>지정 등급의 보상 후보(Phase 1 구현분만).</summary>
        public static IReadOnlyList<RelicDefinition> RewardByGrade(RelicGrade grade) =>
            Filter(AllRelics, r => !r.IsStarter && r.Phase1 && r.Grade == grade);

        // ── 빌드 ────────────────────────────────────────────────────────

        private static RelicDefinition[] BuildAll()
        {
            return new[]
            {
                // ===== Starter (6) — 시작 선택지 =====
                R("S-01", RelicGrade.Starter, "체리 단검", RelicRole.Damage, RelicTriggerType.MatchSymbol, RelicEffectType.AddDamage,
                    value: 3, symbol: SlotSymbolType.Cherry, starter: true, phase1: true,
                    desc: "체리 3개 이상 족보 → 피해 +3", intent: "자주 뜨는 체리라 원시 피해는 낮게. 시작 피해 입문."),
                R("S-02", RelicGrade.Starter, "클로버 방패", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.AddBlock,
                    value: 3, symbol: SlotSymbolType.Clover, starter: true, phase1: true,
                    desc: "클로버 3개 이상 족보 → 방어도 +3", intent: "자주 뜨는 클로버라 방어 수치 낮게. 초반 안정성."),
                R("S-03", RelicGrade.Starter, "종 치료제", RelicRole.Heal, RelicTriggerType.MatchSymbol, RelicEffectType.Heal,
                    value: 2, symbol: SlotSymbolType.Bell, starter: true, phase1: true,
                    desc: "종 3개 이상 족보 → HP +2 회복", intent: "자주 뜨는 종이라 회복량 낮게."),
                R("S-04", RelicGrade.Starter, "레몬 칼날", RelicRole.Damage, RelicTriggerType.MatchSymbol, RelicEffectType.AddDamage,
                    value: 5, symbol: SlotSymbolType.Lemon, starter: true, phase1: true,
                    desc: "레몬 3개 이상 족보 → 피해 +5", intent: "덜 뜨는 레몬이라 원시 피해 보정. S-01과 기대값 동급."),
                R("S-05", RelicGrade.Starter, "다이아 갑옷", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.AddBlock,
                    value: 5, symbol: SlotSymbolType.Diamond, starter: true, phase1: true,
                    desc: "다이아 3개 이상 족보 → 방어도 +5", intent: "덜 뜨는 다이아라 방어 보정. S-02와 기대값 동급."),
                R("S-06", RelicGrade.Starter, "세븐 붕대", RelicRole.Heal, RelicTriggerType.MatchSymbol, RelicEffectType.Heal,
                    value: 4, symbol: SlotSymbolType.Seven, starter: true, phase1: true,
                    desc: "세븐 3개 이상 족보 → HP +4 회복", intent: "덜 뜨는 세븐이라 회복 보정. S-03과 기대값 동급."),

                // ===== Common (22) =====
                R("C-01", RelicGrade.Common, "체리 칼집", RelicRole.Damage, RelicTriggerType.MatchSymbol, RelicEffectType.AddDamage,
                    value: 4, symbol: SlotSymbolType.Cherry, phase1: true, desc: "체리 3개 이상 족보 → 피해 +4"),
                R("C-02", RelicGrade.Common, "클로버 가시막", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.GainThorns,
                    value: 2, symbol: SlotSymbolType.Clover, phase1: true,
                    desc: "클로버 3개 이상 족보 → 가시 2 (직접 공격 피격 시 50% 확률로 2 반사, 적 팀 턴 종료 시 제거)"),
                R("C-03", RelicGrade.Common, "종 연고", RelicRole.Heal, RelicTriggerType.MatchSymbol, RelicEffectType.Heal,
                    value: 3, symbol: SlotSymbolType.Bell, phase1: true, desc: "종 3개 이상 족보 → HP +3 회복"),
                R("C-04", RelicGrade.Common, "레몬 절단기", RelicRole.Damage, RelicTriggerType.MatchSymbol, RelicEffectType.AddDamage,
                    value: 7, symbol: SlotSymbolType.Lemon, phase1: true, desc: "레몬 3개 이상 족보 → 피해 +7"),
                R("C-05", RelicGrade.Common, "다이아 장갑", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.AddBlock,
                    value: 7, symbol: SlotSymbolType.Diamond, phase1: true, desc: "다이아 3개 이상 족보 → 방어도 +7"),
                R("C-06", RelicGrade.Common, "세븐 응급약", RelicRole.Heal, RelicTriggerType.MatchSymbol, RelicEffectType.Heal,
                    value: 5, symbol: SlotSymbolType.Seven, phase1: true, desc: "세븐 3개 이상 족보 → HP +5 회복"),
                R("C-07", RelicGrade.Common, "붉은 성냥", RelicRole.Status, RelicTriggerType.MatchTag, RelicEffectType.ApplyBurn,
                    value: 2, tag: SymbolTag.Red, required: 4, phase1: true,
                    desc: "붉은 계열 4개 이상 족보 → 화상 2 (즉시 2 피해, 대상 팀 턴 종료 시 2 피해 후 제거)"),
                R("C-08", RelicGrade.Common, "푸른 감염가루", RelicRole.Status, RelicTriggerType.MatchTag, RelicEffectType.ApplyInfect,
                    value: 2, tag: SymbolTag.Blue, required: 4, phase1: true,
                    desc: "푸른 계열 4개 이상 족보 → 감염 2 (대상 팀 턴 종료 시 현재 감염만큼 피해 후 1 감소)"),
                R("C-09", RelicGrade.Common, "노란 균열분말", RelicRole.Status, RelicTriggerType.MatchTag, RelicEffectType.ApplyVulnerable,
                    value: 1, tag: SymbolTag.Yellow, required: 4, phase1: true,
                    desc: "노란 계열 4개 이상 족보 → 취약 1 (다음 피격 행동 1회 동안 직접 피해 +20%)"),
                R("C-10", RelicGrade.Common, "과일 칼집", RelicRole.Damage, RelicTriggerType.MatchTag, RelicEffectType.AddDamage,
                    value: 6, tag: SymbolTag.Fruit, required: 4, phase1: true, desc: "과일 태그 4개 이상 족보 → 피해 +6"),
                R("C-11", RelicGrade.Common, "행운 가시우산", RelicRole.Defense, RelicTriggerType.MatchTag, RelicEffectType.GainThorns,
                    value: 3, tag: SymbolTag.Luck, required: 4, phase1: true,
                    desc: "행운 태그 4개 이상 족보 → 가시 3 (직접 공격 피격 시 50% 확률로 3 반사, 적 팀 턴 종료 시 제거)"),
                R("C-12", RelicGrade.Common, "보물 붕대", RelicRole.Heal, RelicTriggerType.MatchTag, RelicEffectType.Heal,
                    value: 4, tag: SymbolTag.Treasure, required: 4, phase1: true, desc: "보물 태그 4개 이상 족보 → HP +4 회복"),
                R("C-13", RelicGrade.Common, "체리 보호막", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.AddBlock,
                    value: 4, symbol: SlotSymbolType.Cherry, phase1: true, desc: "체리 3개 이상 족보 → 방어도 +4"),
                R("C-14", RelicGrade.Common, "클로버 투척날", RelicRole.Damage, RelicTriggerType.MatchSymbol, RelicEffectType.AddDamage,
                    value: 4, symbol: SlotSymbolType.Clover, phase1: true, desc: "클로버 3개 이상 족보 → 피해 +4"),
                R("C-15", RelicGrade.Common, "종 망치", RelicRole.Damage, RelicTriggerType.MatchSymbol, RelicEffectType.AddDamage,
                    value: 4, symbol: SlotSymbolType.Bell, phase1: true, desc: "종 3개 이상 족보 → 피해 +4"),
                R("C-16", RelicGrade.Common, "레몬 흡혈액", RelicRole.Heal, RelicTriggerType.MatchSymbol, RelicEffectType.Lifesteal,
                    value: 12, value2: 6, symbol: SlotSymbolType.Lemon, phase1: true,
                    desc: "레몬 3개 이상 족보로 피해 시 → 입힌 피해의 12% 회복 (턴당 최대 6)",
                    intent: "흡혈=입힌 피해→회복 환산이라 전투 코어 없이 성립. CombatTurnRequestBuilder가 계산."),
                R("C-17", RelicGrade.Common, "다이아 망치", RelicRole.Damage, RelicTriggerType.MatchSymbol, RelicEffectType.AddDamage,
                    value: 7, symbol: SlotSymbolType.Diamond, phase1: true, desc: "다이아 3개 이상 족보 → 피해 +7"),
                R("C-18", RelicGrade.Common, "세븐 방패", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.AddBlock,
                    value: 7, symbol: SlotSymbolType.Seven, phase1: true, desc: "세븐 3개 이상 족보 → 방어도 +7"),
                R("C-19", RelicGrade.Common, "체리 흡혈침", RelicRole.Heal, RelicTriggerType.MatchSymbol, RelicEffectType.Lifesteal,
                    value: 8, value2: 4, symbol: SlotSymbolType.Cherry, phase1: true,
                    desc: "체리 3개 이상 족보로 피해 시 → 입힌 피해의 8% 회복 (턴당 최대 4)",
                    intent: "고확률 체리 흡혈. 턴당 상한 4로 불사 방지. 전투 코어 불필요(피해→회복 환산)."),
                R("C-20", RelicGrade.Common, "다이아 가시갑옷", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.GainThorns,
                    value: 4, symbol: SlotSymbolType.Diamond, phase1: true,
                    desc: "다이아 3개 이상 족보 → 가시 4 (직접 공격 피격 시 50% 확률로 4 반사, 적 팀 턴 종료 시 제거)"),
                R("C-21", RelicGrade.Common, "레몬 약화제", RelicRole.Status, RelicTriggerType.MatchSymbol, RelicEffectType.ApplyWeak,
                    value: 1, symbol: SlotSymbolType.Lemon, phase1: true,
                    desc: "레몬 3개 이상 족보 → 약화 1 (다음 공격 행동 1회 동안 직접 피해 -20%)"),
                R("C-22", RelicGrade.Common, "종 균열음", RelicRole.Status, RelicTriggerType.MatchSymbol, RelicEffectType.ApplyVulnerable,
                    value: 1, symbol: SlotSymbolType.Bell, phase1: true,
                    desc: "종 3개 이상 족보 → 취약 1 (다음 피격 행동 1회 동안 직접 피해 +20%)"),

                // ===== Uncommon (20) =====
                R("U-01", RelicGrade.Uncommon, "체리 폭발칼", RelicRole.Damage, RelicTriggerType.MatchSymbol, RelicEffectType.AddDamage,
                    value: 7, symbol: SlotSymbolType.Cherry, enemyHpBelow: 50, phase1: true, qa: "m",
                    desc: "체리 3개 이상 족보 + 적 HP 50% 이하 → 피해 +7"),
                R("U-02", RelicGrade.Uncommon, "레몬 전격도", RelicRole.Damage, RelicTriggerType.MatchSymbol, RelicEffectType.AddDamage,
                    value: 11, symbol: SlotSymbolType.Lemon, enemyHpBelow: 50, phase1: true, qa: "m",
                    desc: "레몬 3개 이상 족보 + 적 HP 50% 이하 → 피해 +11"),
                R("U-03", RelicGrade.Uncommon, "클로버 응급술", RelicRole.Heal, RelicTriggerType.MatchSymbol, RelicEffectType.Heal,
                    value: 4, value2: 2, symbol: SlotSymbolType.Clover, playerHpBelowBonus: 50, phase1: true, qa: "m",
                    desc: "클로버 3개 이상 족보 → HP +4 회복. HP 50% 이하면 +2 추가"),
                R("U-04", RelicGrade.Uncommon, "세븐 구급상자", RelicRole.Heal, RelicTriggerType.MatchSymbol, RelicEffectType.Heal,
                    value: 6, value2: 3, symbol: SlotSymbolType.Seven, playerHpBelowBonus: 50, phase1: true, qa: "m",
                    desc: "세븐 3개 이상 족보 → HP +6 회복. HP 50% 이하면 +3 추가"),
                R("U-05", RelicGrade.Uncommon, "종 울림방패", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.AddBlock,
                    value: 6, symbol: SlotSymbolType.Bell, phase1: true, qa: "m", desc: "종 3개 이상 족보 → 방어도 +6"),
                R("U-06", RelicGrade.Uncommon, "다이아 요새", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.AddBlock,
                    value: 10, symbol: SlotSymbolType.Diamond, phase1: true, qa: "m", desc: "다이아 3개 이상 족보 → 방어도 +10"),
                R("U-07", RelicGrade.Uncommon, "붉은 점화탄", RelicRole.Status, RelicTriggerType.MatchTag, RelicEffectType.ApplyBurn,
                    value: 3, tag: SymbolTag.Red, required: 4, phase1: true, qa: "m",
                    desc: "붉은 계열 4개 이상 족보 → 화상 3 (즉시 3 피해, 대상 팀 턴 종료 시 3 피해 후 제거)"),
                R("U-08", RelicGrade.Uncommon, "푸른 배양액", RelicRole.Status, RelicTriggerType.MatchTag, RelicEffectType.ApplyInfect,
                    value: 3, tag: SymbolTag.Blue, required: 4, phase1: true, qa: "m",
                    desc: "푸른 계열 4개 이상 족보 → 감염 3 (대상 팀 턴 종료 시 현재 감염만큼 피해 후 1 감소)"),
                R("U-09", RelicGrade.Uncommon, "노란 무력석", RelicRole.Status, RelicTriggerType.MatchTag, RelicEffectType.ApplyWeak,
                    value: 2, tag: SymbolTag.Yellow, required: 4, phase1: true, qa: "m",
                    desc: "노란 계열 4개 이상 족보 → 약화 2 (다음 공격 행동 2회 동안 직접 피해 -20%)"),
                R("U-10", RelicGrade.Uncommon, "과일 보급상자", RelicRole.Heal, RelicTriggerType.MatchTag, RelicEffectType.Heal,
                    value: 6, tag: SymbolTag.Fruit, required: 4, phase1: true, qa: "m", desc: "과일 태그 4개 이상 족보 → HP +6 회복"),
                R("U-11", RelicGrade.Uncommon, "행운 폭격", RelicRole.Damage, RelicTriggerType.MatchTag, RelicEffectType.AddDamage,
                    value: 8, tag: SymbolTag.Luck, required: 4, phase1: true, qa: "m", desc: "행운 태그 4개 이상 족보 → 피해 +8"),
                R("U-12", RelicGrade.Uncommon, "보물 균열장벽", RelicRole.Status, RelicTriggerType.MatchTag, RelicEffectType.ApplyVulnerable,
                    value: 2, tag: SymbolTag.Treasure, required: 4, phase1: true, qa: "m",
                    desc: "보물 태그 4개 이상 족보 → 취약 2 (다음 피격 행동 2회 동안 직접 피해 +20%)"),
                R("U-13", RelicGrade.Uncommon, "상태 추격자", RelicRole.Damage, RelicTriggerType.Conditional, RelicEffectType.AddDamage,
                    value: 2, value2: 1, enemyStatus: EnemyStatusRequirement.Any, phase1: false, qa: "m",
                    desc: "상태이상 적에게 피해 효과 발동 시 그 피해 +2. 화상/감염 상태면 추가 +1",
                    intent: "Phase 2: 모든 대상 상태 조회와 조건부 피해 발동 규칙이 필요해 보상풀 제외."),
                R("U-14", RelicGrade.Uncommon, "두꺼운 붕대", RelicRole.Heal, RelicTriggerType.Conditional, RelicEffectType.Heal,
                    value: 6, phase1: false, qa: "m", desc: "전투당 1회, HP 40% 이하가 될 때 HP +6 회복",
                    intent: "Phase 2: 전투당 1회 발동 상태 필요."),
                R("U-15", RelicGrade.Uncommon, "단단한 외피", RelicRole.Defense, RelicTriggerType.BattleStart, RelicEffectType.AddBlock,
                    value: 5, phase1: false, qa: "m", desc: "전투 시작 시 방어도 +5", intent: "Phase 2: 전투 시작 훅 필요."),
                R("U-16", RelicGrade.Uncommon, "그을린 추격자", RelicRole.Damage, RelicTriggerType.Conditional, RelicEffectType.AddDamage,
                    value: 3, tag: SymbolTag.Red, enemyStatus: EnemyStatusRequirement.Burn, phase1: false, qa: "m",
                    desc: "화상 상태인 적에게 피해 효과 발동 시 그 피해 +3",
                    intent: "Phase 2: 상태 기반 조건부 피해 발동 범위가 이번 작업에 포함되지 않아 보상풀 제외."),
                R("U-17", RelicGrade.Uncommon, "감염 추격자", RelicRole.Damage, RelicTriggerType.Conditional, RelicEffectType.AddDamage,
                    value: 3, tag: SymbolTag.Blue, enemyStatus: EnemyStatusRequirement.Infect, phase1: false, qa: "m",
                    desc: "감염 상태인 적에게 피해 효과 발동 시 그 피해 +3",
                    intent: "Phase 2: 상태 기반 조건부 피해 발동 범위가 이번 작업에 포함되지 않아 보상풀 제외."),
                R("U-18", RelicGrade.Uncommon, "무력 추격자", RelicRole.Damage, RelicTriggerType.Conditional, RelicEffectType.AddDamage,
                    value: 2, value2: 1, tag: SymbolTag.Yellow, phase1: false, qa: "m",
                    desc: "약화 상태인 적에게 피해 효과 발동 시 그 피해 +2. 다음 공격 예고 상태면 추가 +1",
                    intent: "Phase 2: 약화 상태 조회와 다음 공격 예고 조건이 없어 보상풀 제외."),
                R("U-19", RelicGrade.Uncommon, "가시 재킷", RelicRole.Defense, RelicTriggerType.BattleStart, RelicEffectType.GainThorns,
                    value: 2, value2: 1, phase1: false, qa: "m",
                    desc: "전투 시작 시 가시 2. 보스전이면 추가 +1",
                    intent: "Phase 2: 전투 시작 발동 흐름이 없어 보상풀 제외."),
                R("U-20", RelicGrade.Uncommon, "피맛 증폭기", RelicRole.Heal, RelicTriggerType.Conditional, RelicEffectType.Special,
                    value: 1, value2: 2, phase1: false, qa: "m",
                    desc: "흡혈로 HP 회복 시 추가 HP +1 회복 (턴당 2회, 흡혈 상한 초과 불가)",
                    intent: "Phase 2: 흡혈은 전투 미지원이라 보상풀 제외."),

                // ===== Rare (16) — Phase 2 =====
                R("R-01", RelicGrade.Rare, "화염 요리사", RelicRole.Status, RelicTriggerType.Passive, RelicEffectType.AmplifyStatus,
                    value: 1, value2: 3, tag: SymbolTag.Red, qa: "h",
                    desc: "패시브: 화상 부여량 +1. 화상 상태 적에게 주는 첫 피해 +3 (턴당 1회)"),
                R("R-02", RelicGrade.Rare, "감염 지팡이", RelicRole.Status, RelicTriggerType.Passive, RelicEffectType.AmplifyStatus,
                    value: 1, value2: 4, tag: SymbolTag.Blue, qa: "h",
                    desc: "패시브: 감염 부여량 +1, 감염 피해 +1, 감염 최대 스택 +4"),
                R("R-03", RelicGrade.Rare, "균열 도체", RelicRole.Status, RelicTriggerType.Passive, RelicEffectType.AmplifyStatus,
                    value: 5, value2: 1, tag: SymbolTag.Yellow, qa: "h",
                    desc: "패시브: 취약 피해 증가율 +5%p, 취약 최대 적용 횟수 +1"),
                R("R-04", RelicGrade.Rare, "과일 증폭기", RelicRole.Heal, RelicTriggerType.Passive, RelicEffectType.ModifyDamageMultiplier,
                    value: 25, tag: SymbolTag.Fruit, qa: "h", desc: "패시브: 과일 태그 유물의 회복/방어 효과 +25%"),
                R("R-05", RelicGrade.Rare, "행운 약점추적", RelicRole.Damage, RelicTriggerType.Conditional, RelicEffectType.ModifyDamageMultiplier,
                    value: 12, tag: SymbolTag.Luck, qa: "h",
                    desc: "행운 태그 유물로 피해 시 대상에 취약이 있으면 그 피해 +12% (턴당 1회)"),
                R("R-06", RelicGrade.Rare, "보물 가시핵", RelicRole.Defense, RelicTriggerType.Conditional, RelicEffectType.GainThorns,
                    value: 1, value2: 3, tag: SymbolTag.Treasure, qa: "h",
                    desc: "보물 태그 유물로 방어도 획득 시 가시 +1 (턴당 최대 +3)",
                    intent: "Phase 2: 방어도 획득 감지와 턴당 발동 제한이 없어 보상풀 제외."),
                R("R-07", RelicGrade.Rare, "고확률 절제", RelicRole.Damage, RelicTriggerType.Passive, RelicEffectType.ModifyDamageMultiplier,
                    value: 15, qa: "h", desc: "높은 확률 심볼(체리/클로버/종) 족보 효과 +15%"),
                R("R-08", RelicGrade.Rare, "저확률 대박", RelicRole.Damage, RelicTriggerType.Passive, RelicEffectType.ModifyDamageMultiplier,
                    value: 30, qa: "h", desc: "낮은 확률 심볼(레몬/다이아/세븐) 족보 효과 +30%"),
                R("R-09", RelicGrade.Rare, "보상의 문", RelicRole.Damage, RelicTriggerType.Reward, RelicEffectType.AddRewardChoice,
                    value: 1, qa: "h", desc: "엘리트/보스 전투 클리어 시 유물 선택지 +1"),
                R("R-10", RelicGrade.Rare, "응급 기도", RelicRole.Heal, RelicTriggerType.Conditional, RelicEffectType.ReviveOnce,
                    value: 5, qa: "h", desc: "전투당 1회, 치명 피해를 받을 때 HP 1로 생존 후 HP +5 회복"),
                // U-16과 동명(기획 원본 그대로). ID로 구분한다.
                R("R-11", RelicGrade.Rare, "그을린 추격자", RelicRole.Damage, RelicTriggerType.Conditional, RelicEffectType.AddDamage,
                    value: 4, tag: SymbolTag.Red, enemyStatus: EnemyStatusRequirement.Burn, qa: "h",
                    desc: "화상 상태인 적에게 피해 효과 발동 시 그 피해 +4 (턴당 1회)",
                    intent: "Phase 2: 레어 등급은 보상풀 확장 시 활성화."),
                R("R-12", RelicGrade.Rare, "배양 접시", RelicRole.Status, RelicTriggerType.Conditional, RelicEffectType.Special,
                    value: 1, value2: 2, tag: SymbolTag.Blue, qa: "h",
                    desc: "감염 피해 발생 시 감염 피해 +1. 대상의 감염이 5 이상이면 추가 피해 +2"),
                R("R-13", RelicGrade.Rare, "송곳니 부적", RelicRole.Heal, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 20, value2: 2, tag: SymbolTag.Red, qa: "h",
                    desc: "패시브: 흡혈 회복량 +20%, 흡혈 회복 턴당 최대치 +2"),
                R("R-14", RelicGrade.Rare, "가시 왕관", RelicRole.Defense, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 25, value2: 2, tag: SymbolTag.Blue, qa: "h",
                    desc: "패시브: 가시 피해 +25%. 전투 시작 시 이번 턴 가시 2"),
                R("R-15", RelicGrade.Rare, "약점 확대경", RelicRole.Damage, RelicTriggerType.Conditional, RelicEffectType.ModifyDamageMultiplier,
                    value: 8, tag: SymbolTag.Yellow, qa: "h",
                    desc: "취약 상태인 적에게 주는 피해 +8% (턴당 1회)"),
                R("R-16", RelicGrade.Rare, "무력화의 향", RelicRole.Status, RelicTriggerType.Conditional, RelicEffectType.AmplifyStatus,
                    value: 1, value2: 5, tag: SymbolTag.Yellow, qa: "h",
                    desc: "약화 부여량 +1. 약화 상태 적의 다음 공격 피해 추가 -5%p (총 감소율 최대 40%)"),

                // ===== Legendary (8) — Phase 2 =====
                R("L-01", RelicGrade.Legendary, "잭팟 심장", RelicRole.Damage, RelicTriggerType.Conditional, RelicEffectType.ModifyDamageMultiplier,
                    value: 40, qa: "h", desc: "5개 이상 족보 달성 시 그 족보의 피해/회복/방어 수치 +40%"),
                R("L-02", RelicGrade.Legendary, "희귀성의 왕관", RelicRole.Damage, RelicTriggerType.Conditional, RelicEffectType.ModifyDamageMultiplier,
                    value: 50, qa: "h", desc: "낮은 확률 심볼 3개 이상 족보 달성 시 효과 +50%"),
                R("L-03", RelicGrade.Legendary, "상태의 왕관", RelicRole.Status, RelicTriggerType.Conditional, RelicEffectType.AmplifyStatus,
                    value: 1, value2: 3, qa: "h", desc: "화상 또는 감염 부여 시 그 수치 +1 (턴당 최대 3회)"),
                R("L-04", RelicGrade.Legendary, "수호 천칭", RelicRole.Defense, RelicTriggerType.Conditional, RelicEffectType.BlockToHeal,
                    value: 25, phase1: true, qa: "h", desc: "방어도 획득 시 그 25%만큼 HP 회복",
                    intent: "방어→회복 환산이라 전투 코어 없이 성립. 획득 방어도 기준이라 상한 불필요."),
                R("L-05", RelicGrade.Legendary, "흡혈 왕관", RelicRole.Heal, RelicTriggerType.Conditional, RelicEffectType.Lifesteal,
                    value: 10, value2: 10, phase1: true, qa: "h",
                    desc: "피해 효과 발동 시 입힌 피해의 10%만큼 HP 회복 (턴당 최대 10)",
                    intent: "공격→회복 전환 전설. 턴당 상한 10으로 후반 과회복 방지. 전투 코어 불필요."),
                R("L-06", RelicGrade.Legendary, "운명의 리롤", RelicRole.Damage, RelicTriggerType.Reward, RelicEffectType.AddRewardReroll,
                    value: 1, qa: "h", desc: "보스 클리어 시 유물 보상 1개를 한 번 다시 굴림"),
                R("L-07", RelicGrade.Legendary, "연소 감염로", RelicRole.Status, RelicTriggerType.Conditional, RelicEffectType.Special,
                    value: 6, value2: 1, tag: SymbolTag.Red, qa: "h",
                    desc: "화상/감염 부여 시 대상에 다른 지속 피해 속성이 있으면 추가 피해 6, 부여 수치 +1"),
                R("L-08", RelicGrade.Legendary, "피와 가시의 순환", RelicRole.Damage, RelicTriggerType.Conditional, RelicEffectType.Special,
                    value: 15, value2: 1, qa: "h",
                    desc: "흡혈 회복/가시 피해 발생 시 다음 피해 효과 +15% (최대 1회 저장, 턴당 1회)"),

                // ===== Curse (8) — Phase 2 =====
                R("K-01", RelicGrade.Curse, "선혈 계약", RelicRole.Status, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 2, value2: 2, tag: SymbolTag.Red, qa: "h", desc: "화상 부여량 +2. 전투 시작 시 자신에게 화상 2"),
                R("K-02", RelicGrade.Curse, "녹슨 심장", RelicRole.Status, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 2, value2: 35, tag: SymbolTag.Blue, qa: "h", desc: "감염 부여량 +2. 받는 회복량 -35%"),
                R("K-03", RelicGrade.Curse, "무력한 손", RelicRole.Status, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 2, value2: 4, tag: SymbolTag.Yellow, qa: "h", desc: "약화 부여량 +2. 전투 시작 방어도 -4"),
                R("K-04", RelicGrade.Curse, "굶주린 릴", RelicRole.Damage, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 35, qa: "h", desc: "모든 피해 효과 +35%. 모든 회복 효과 -35%"),
                R("K-05", RelicGrade.Curse, "금 간 갑옷", RelicRole.Defense, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 40, value2: 2, qa: "h", desc: "방어도 획득량 +40%. 피격 피해 +2"),
                R("K-06", RelicGrade.Curse, "깨진 보상판", RelicRole.Damage, RelicTriggerType.Reward, RelicEffectType.Special,
                    value: 1, qa: "h", desc: "보상 선택지 -1. 대신 선택 유물 등급 상승 확률 증가"),
                R("K-07", RelicGrade.Curse, "불운한 주사위", RelicRole.Damage, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 60, value2: 15, qa: "h", desc: "3개 족보 효과 -15%. 5개 이상 족보 효과 +60%"),
                R("K-08", RelicGrade.Curse, "보스의 낙인", RelicRole.Damage, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 40, value2: 20, qa: "h", desc: "엘리트/보스에게 주는 피해 +40%. 엘리트/보스 공격력 +20%"),
            };
        }

        private static RelicDefinition R(
            string id,
            RelicGrade grade,
            string name,
            RelicRole role,
            RelicTriggerType trigger,
            RelicEffectType effect,
            int value = 0,
            int value2 = 0,
            int required = 3,
            SlotSymbolType? symbol = null,
            SymbolTag? tag = null,
            int enemyHpBelow = 0,
            int playerHpBelowBonus = 0,
            EnemyStatusRequirement enemyStatus = EnemyStatusRequirement.None,
            bool starter = false,
            bool phase1 = false,
            string desc = "",
            string intent = "",
            string qa = "l",
            string iconKey = "")
        {
            return new RelicDefinition(
                id, grade, name,
                string.IsNullOrEmpty(iconKey) ? RelicIconKeys.DefaultFor(role) : iconKey,
                role, trigger, effect, symbol, tag, required,
                value, value2, enemyHpBelow, playerHpBelowBonus, enemyStatus,
                starter, phase1, desc, intent, qa);
        }

        private static Dictionary<string, RelicDefinition> BuildIndex(RelicDefinition[] relics)
        {
            var map = new Dictionary<string, RelicDefinition>(relics.Length);
            for (int i = 0; i < relics.Length; i++)
            {
                map[relics[i].Id] = relics[i];
            }

            return map;
        }

        private static RelicDefinition[] Filter(RelicDefinition[] source, System.Predicate<RelicDefinition> predicate)
        {
            var list = new List<RelicDefinition>();
            for (int i = 0; i < source.Length; i++)
            {
                if (predicate(source[i]))
                {
                    list.Add(source[i]);
                }
            }

            return list.ToArray();
        }
    }
}
