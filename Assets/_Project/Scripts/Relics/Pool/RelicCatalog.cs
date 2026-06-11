using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.Relics.Pool
{
    /// <summary>
    /// v20.3 유물 풀 — 코드 카탈로그(63종). HTML(relic_pool_v20_3) RELICS 배열을 1:1로 옮긴 단일 출처.
    /// 밸런스/이름/효과는 임의로 바꾸지 않는다.
    ///
    /// "카탈로그 등록"과 "보상풀 등장"은 분리한다: 모든 63종은 <see cref="All"/>에 있지만,
    /// Phase 1 보상풀(<see cref="RewardPool"/>)·시작 선택지(<see cref="Starters"/>)에는
    /// <see cref="RelicDefinition.Phase1"/>인 것만 등장한다.
    /// </summary>
    public static class RelicCatalog
    {
        private static readonly RelicDefinition[] AllRelics = BuildAll();
        private static readonly Dictionary<string, RelicDefinition> ById = BuildIndex(AllRelics);

        /// <summary>카탈로그 전체(63종, 미구현 포함).</summary>
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
                    desc: "7 3개 이상 족보 → HP +4 회복", intent: "덜 뜨는 7이라 회복 보정. S-03과 기대값 동급."),

                // ===== Common (18) =====
                R("C-01", RelicGrade.Common, "체리 칼집", RelicRole.Damage, RelicTriggerType.MatchSymbol, RelicEffectType.AddDamage,
                    value: 4, symbol: SlotSymbolType.Cherry, phase1: true, desc: "체리 3개 이상 족보 → 피해 +4"),
                R("C-02", RelicGrade.Common, "클로버 가시막", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.AddBlock,
                    value: 4, symbol: SlotSymbolType.Clover, phase1: true, desc: "클로버 3개 이상 족보 → 방어도 +4"),
                R("C-03", RelicGrade.Common, "종 연고", RelicRole.Heal, RelicTriggerType.MatchSymbol, RelicEffectType.Heal,
                    value: 3, symbol: SlotSymbolType.Bell, phase1: true, desc: "종 3개 이상 족보 → HP +3 회복"),
                R("C-04", RelicGrade.Common, "레몬 절단기", RelicRole.Damage, RelicTriggerType.MatchSymbol, RelicEffectType.AddDamage,
                    value: 7, symbol: SlotSymbolType.Lemon, phase1: true, desc: "레몬 3개 이상 족보 → 피해 +7"),
                R("C-05", RelicGrade.Common, "다이아 장갑", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.AddBlock,
                    value: 7, symbol: SlotSymbolType.Diamond, phase1: true, desc: "다이아 3개 이상 족보 → 방어도 +7"),
                R("C-06", RelicGrade.Common, "세븐 응급약", RelicRole.Heal, RelicTriggerType.MatchSymbol, RelicEffectType.Heal,
                    value: 5, symbol: SlotSymbolType.Seven, phase1: true, desc: "7 3개 이상 족보 → HP +5 회복"),
                R("C-07", RelicGrade.Common, "붉은 성냥", RelicRole.Status, RelicTriggerType.MatchTag, RelicEffectType.ApplyBurn,
                    value: 1, tag: SymbolTag.Red, required: 4, phase1: true, desc: "붉은 계열 4개 이상 족보 → 화상 1 부여"),
                R("C-08", RelicGrade.Common, "푸른 녹가루", RelicRole.Status, RelicTriggerType.MatchTag, RelicEffectType.ApplyCorrosion,
                    value: 1, tag: SymbolTag.Blue, required: 4, phase1: false, desc: "푸른 계열 4개 이상 족보 → 부식 1 부여",
                    intent: "Phase 2: 부식은 전투 미지원이라 보상풀 제외."),
                R("C-09", RelicGrade.Common, "노란 정전기", RelicRole.Status, RelicTriggerType.MatchTag, RelicEffectType.ApplyShock,
                    value: 1, tag: SymbolTag.Yellow, required: 4, phase1: false, desc: "노란 계열 4개 이상 족보 → 감전 1 부여",
                    intent: "Phase 2: 감전은 전투 미지원이라 보상풀 제외."),
                R("C-10", RelicGrade.Common, "과일 칼집", RelicRole.Damage, RelicTriggerType.MatchTag, RelicEffectType.AddDamage,
                    value: 6, tag: SymbolTag.Fruit, required: 4, phase1: true, desc: "과일 태그 4개 이상 족보 → 피해 +6"),
                R("C-11", RelicGrade.Common, "행운 우산", RelicRole.Defense, RelicTriggerType.MatchTag, RelicEffectType.AddBlock,
                    value: 6, tag: SymbolTag.Luck, required: 4, phase1: true, desc: "행운 태그 4개 이상 족보 → 방어도 +6"),
                R("C-12", RelicGrade.Common, "보물 붕대", RelicRole.Heal, RelicTriggerType.MatchTag, RelicEffectType.Heal,
                    value: 4, tag: SymbolTag.Treasure, required: 4, phase1: true, desc: "보물 태그 4개 이상 족보 → HP +4 회복"),
                R("C-13", RelicGrade.Common, "체리 보호막", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.AddBlock,
                    value: 4, symbol: SlotSymbolType.Cherry, phase1: true, desc: "체리 3개 이상 족보 → 방어도 +4"),
                R("C-14", RelicGrade.Common, "클로버 투척날", RelicRole.Damage, RelicTriggerType.MatchSymbol, RelicEffectType.AddDamage,
                    value: 4, symbol: SlotSymbolType.Clover, phase1: true, desc: "클로버 3개 이상 족보 → 피해 +4"),
                R("C-15", RelicGrade.Common, "종 망치", RelicRole.Damage, RelicTriggerType.MatchSymbol, RelicEffectType.AddDamage,
                    value: 4, symbol: SlotSymbolType.Bell, phase1: true, desc: "종 3개 이상 족보 → 피해 +4"),
                R("C-16", RelicGrade.Common, "레몬 연고", RelicRole.Heal, RelicTriggerType.MatchSymbol, RelicEffectType.Heal,
                    value: 5, symbol: SlotSymbolType.Lemon, phase1: true, desc: "레몬 3개 이상 족보 → HP +5 회복"),
                R("C-17", RelicGrade.Common, "다이아 망치", RelicRole.Damage, RelicTriggerType.MatchSymbol, RelicEffectType.AddDamage,
                    value: 7, symbol: SlotSymbolType.Diamond, phase1: true, desc: "다이아 3개 이상 족보 → 피해 +7"),
                R("C-18", RelicGrade.Common, "세븐 방패", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.AddBlock,
                    value: 7, symbol: SlotSymbolType.Seven, phase1: true, desc: "7 3개 이상 족보 → 방어도 +7"),

                // ===== Uncommon (15) =====
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
                    desc: "7 3개 이상 족보 → HP +6 회복. HP 50% 이하면 +3 추가"),
                R("U-05", RelicGrade.Uncommon, "종 울림방패", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.AddBlock,
                    value: 6, symbol: SlotSymbolType.Bell, phase1: true, qa: "m", desc: "종 3개 이상 족보 → 방어도 +6"),
                R("U-06", RelicGrade.Uncommon, "다이아 요새", RelicRole.Defense, RelicTriggerType.MatchSymbol, RelicEffectType.AddBlock,
                    value: 10, symbol: SlotSymbolType.Diamond, phase1: true, qa: "m", desc: "다이아 3개 이상 족보 → 방어도 +10"),
                R("U-07", RelicGrade.Uncommon, "붉은 점화탄", RelicRole.Status, RelicTriggerType.MatchTag, RelicEffectType.ApplyBurn,
                    value: 2, tag: SymbolTag.Red, required: 4, phase1: true, qa: "m", desc: "붉은 계열 4개 이상 족보 → 화상 2 부여"),
                R("U-08", RelicGrade.Uncommon, "푸른 침식액", RelicRole.Status, RelicTriggerType.MatchTag, RelicEffectType.ApplyCorrosion,
                    value: 2, tag: SymbolTag.Blue, required: 4, phase1: false, qa: "m", desc: "푸른 계열 4개 이상 족보 → 부식 2 부여",
                    intent: "Phase 2: 부식은 전투 미지원이라 보상풀 제외."),
                R("U-09", RelicGrade.Uncommon, "노란 번개석", RelicRole.Status, RelicTriggerType.MatchTag, RelicEffectType.ApplyShock,
                    value: 2, tag: SymbolTag.Yellow, required: 4, phase1: false, qa: "m", desc: "노란 계열 4개 이상 족보 → 감전 2 부여",
                    intent: "Phase 2: 감전은 전투 미지원이라 보상풀 제외."),
                R("U-10", RelicGrade.Uncommon, "과일 보급상자", RelicRole.Heal, RelicTriggerType.MatchTag, RelicEffectType.Heal,
                    value: 6, tag: SymbolTag.Fruit, required: 4, phase1: true, qa: "m", desc: "과일 태그 4개 이상 족보 → HP +6 회복"),
                R("U-11", RelicGrade.Uncommon, "행운 폭격", RelicRole.Damage, RelicTriggerType.MatchTag, RelicEffectType.AddDamage,
                    value: 8, tag: SymbolTag.Luck, required: 4, phase1: true, qa: "m", desc: "행운 태그 4개 이상 족보 → 피해 +8"),
                R("U-12", RelicGrade.Uncommon, "보물 장벽", RelicRole.Defense, RelicTriggerType.MatchTag, RelicEffectType.AddBlock,
                    value: 8, tag: SymbolTag.Treasure, required: 4, phase1: true, qa: "m", desc: "보물 태그 4개 이상 족보 → 방어도 +8"),
                R("U-13", RelicGrade.Uncommon, "상처 추격자", RelicRole.Damage, RelicTriggerType.Conditional, RelicEffectType.AddDamage,
                    value: 3, enemyStatus: true, phase1: true, qa: "m",
                    desc: "상태이상 걸린 적에게 피해 효과 발동 시 그 피해 +3"),
                R("U-14", RelicGrade.Uncommon, "두꺼운 붕대", RelicRole.Heal, RelicTriggerType.Conditional, RelicEffectType.Heal,
                    value: 6, phase1: false, qa: "m", desc: "전투당 1회, HP 40% 이하가 될 때 HP +6 회복",
                    intent: "Phase 2: 전투당 1회 발동 상태 필요."),
                R("U-15", RelicGrade.Uncommon, "단단한 외피", RelicRole.Defense, RelicTriggerType.BattleStart, RelicEffectType.AddBlock,
                    value: 5, phase1: false, qa: "m", desc: "전투 시작 시 방어도 +5", intent: "Phase 2: 전투 시작 훅 필요."),

                // ===== Rare (10) — Phase 2 =====
                R("R-01", RelicGrade.Rare, "화염 요리사", RelicRole.Status, RelicTriggerType.Passive, RelicEffectType.AmplifyStatus,
                    value: 1, tag: SymbolTag.Red, qa: "h", desc: "패시브: 화상 부여량 +1, 화상 최대 스택 +3"),
                R("R-02", RelicGrade.Rare, "침식 지팡이", RelicRole.Status, RelicTriggerType.Passive, RelicEffectType.AmplifyStatus,
                    value: 1, tag: SymbolTag.Blue, qa: "h", desc: "패시브: 부식 부여량 +1, 부식 최대 스택 +3"),
                R("R-03", RelicGrade.Rare, "번개 도체", RelicRole.Status, RelicTriggerType.Passive, RelicEffectType.AmplifyStatus,
                    value: 1, tag: SymbolTag.Yellow, qa: "h", desc: "패시브: 감전 부여량 +1, 감전 최대 스택 +3"),
                R("R-04", RelicGrade.Rare, "과일 증폭기", RelicRole.Heal, RelicTriggerType.Passive, RelicEffectType.ModifyDamageMultiplier,
                    value: 25, tag: SymbolTag.Fruit, qa: "h", desc: "패시브: 과일 태그 유물의 회복/방어 효과 +25%"),
                R("R-05", RelicGrade.Rare, "행운 예리화", RelicRole.Damage, RelicTriggerType.Passive, RelicEffectType.ModifyDamageMultiplier,
                    value: 25, tag: SymbolTag.Luck, qa: "h", desc: "패시브: 행운 태그 유물의 피해 효과 +25%"),
                R("R-06", RelicGrade.Rare, "보물 수호자", RelicRole.Defense, RelicTriggerType.Passive, RelicEffectType.ModifyDamageMultiplier,
                    value: 25, tag: SymbolTag.Treasure, qa: "h", desc: "패시브: 보물 태그 유물의 방어 효과 +25%"),
                R("R-07", RelicGrade.Rare, "고확률 절제", RelicRole.Damage, RelicTriggerType.Passive, RelicEffectType.ModifyDamageMultiplier,
                    value: 20, qa: "h", desc: "높은 확률 심볼(체리/클로버/종) 족보 효과 발동 시 +20%"),
                R("R-08", RelicGrade.Rare, "저확률 대박", RelicRole.Damage, RelicTriggerType.Passive, RelicEffectType.ModifyDamageMultiplier,
                    value: 35, qa: "h", desc: "낮은 확률 심볼(레몬/다이아/7) 족보 효과 발동 시 +35%"),
                R("R-09", RelicGrade.Rare, "보상의 문", RelicRole.Damage, RelicTriggerType.Reward, RelicEffectType.AddRewardChoice,
                    value: 1, qa: "h", desc: "엘리트/보스 전투 클리어 시 유물 선택지 +1"),
                R("R-10", RelicGrade.Rare, "응급 기도", RelicRole.Heal, RelicTriggerType.Conditional, RelicEffectType.ReviveOnce,
                    value: 5, qa: "h", desc: "전투당 1회, 치명 피해를 받을 때 HP 1로 생존 후 HP +5 회복"),

                // ===== Legendary (6) — Phase 2 =====
                R("L-01", RelicGrade.Legendary, "잭팟 심장", RelicRole.Damage, RelicTriggerType.Conditional, RelicEffectType.ModifyDamageMultiplier,
                    value: 50, qa: "h", desc: "5개 이상 족보 달성 시 그 족보 수치 +50%"),
                R("L-02", RelicGrade.Legendary, "희귀성의 왕관", RelicRole.Damage, RelicTriggerType.Conditional, RelicEffectType.ModifyDamageMultiplier,
                    value: 60, qa: "h", desc: "낮은 확률 심볼 3개 이상 족보 달성 시 +60%"),
                R("L-03", RelicGrade.Legendary, "상태의 왕관", RelicRole.Status, RelicTriggerType.Conditional, RelicEffectType.AmplifyStatus,
                    value: 1, qa: "h", desc: "상태이상 부여 시 1 추가 부여. 각 상태이상 최대 스택 +5"),
                R("L-04", RelicGrade.Legendary, "수호 천칭", RelicRole.Defense, RelicTriggerType.Conditional, RelicEffectType.BlockToHeal,
                    value: 25, qa: "h", desc: "방어도 획득 시 그 25%만큼 HP 회복"),
                R("L-05", RelicGrade.Legendary, "흡혈 왕관", RelicRole.Heal, RelicTriggerType.Conditional, RelicEffectType.Lifesteal,
                    value: 20, qa: "h", desc: "피해 효과 발동 시 입힌 피해의 20%만큼 HP 회복"),
                R("L-06", RelicGrade.Legendary, "운명의 리롤", RelicRole.Damage, RelicTriggerType.Reward, RelicEffectType.AddRewardReroll,
                    value: 1, qa: "h", desc: "보스 클리어 시 유물 보상 1개를 한 번 다시 굴림"),

                // ===== Curse (8) — Phase 2 =====
                R("K-01", RelicGrade.Curse, "선혈 계약", RelicRole.Status, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 2, tag: SymbolTag.Red, qa: "h", desc: "화상 부여량 +2. 전투 시작 시 자신에게 화상 1"),
                R("K-02", RelicGrade.Curse, "녹슨 심장", RelicRole.Status, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 2, tag: SymbolTag.Blue, qa: "h", desc: "부식 부여량 +2. 받는 회복량 -30%"),
                R("K-03", RelicGrade.Curse, "감전된 손", RelicRole.Status, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 2, tag: SymbolTag.Yellow, qa: "h", desc: "감전 부여량 +2. 전투 시작 방어도 -4"),
                R("K-04", RelicGrade.Curse, "굶주린 릴", RelicRole.Damage, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 35, qa: "h", desc: "모든 피해 효과 +35%. 모든 회복 효과 -35%"),
                R("K-05", RelicGrade.Curse, "금 간 갑옷", RelicRole.Defense, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 50, qa: "h", desc: "방어도 획득량 +50%. 피격 피해 +2"),
                R("K-06", RelicGrade.Curse, "깨진 보상판", RelicRole.Damage, RelicTriggerType.Reward, RelicEffectType.Special,
                    value: 1, qa: "h", desc: "보상 선택지 -1. 대신 선택 유물 등급 상승 확률 증가"),
                R("K-07", RelicGrade.Curse, "불운한 주사위", RelicRole.Damage, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 80, qa: "h", desc: "3개 족보 효과 -20%. 5개 이상 족보 효과 +80%"),
                R("K-08", RelicGrade.Curse, "보스의 낙인", RelicRole.Damage, RelicTriggerType.Passive, RelicEffectType.Special,
                    value: 40, qa: "h", desc: "엘리트/보스에게 주는 피해 +40%. 엘리트/보스 공격력 +20%"),
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
            bool enemyStatus = false,
            bool starter = false,
            bool phase1 = false,
            string desc = "",
            string intent = "",
            string qa = "l")
        {
            return new RelicDefinition(
                id, grade, name, role, trigger, effect, symbol, tag, required,
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
