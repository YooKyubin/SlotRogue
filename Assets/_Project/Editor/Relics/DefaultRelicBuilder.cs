using System.Collections.Generic;
using System.Reflection;
using SlotRogue.Relics.Data;
using SlotRogue.Slot.Data;
using UnityEditor;
using UnityEngine;

namespace SlotRogue.Editor.Relics
{
    /// <summary>
    /// 1차 구현 유물 66종을 <see cref="RelicDataSO"/> 에셋으로 자동 생성한다.
    /// 데이터 클래스의 private 직렬화 필드는 리플렉션으로 채운다(런타임 API는 데이터 전용 유지).
    /// </summary>
    public static class DefaultRelicBuilder
    {
        private const string RelicFolder = "Assets/_Project/Data/Relics";

        // 재화(골드·재스핀·상점/보상) 유물은 이번 기획에서 빠질 수 있어 별도 폴더/메뉴로 분리한다.
        private const string CurrencyFolder = "Assets/_Project/Data/Relics/Currency";

        private static int _built;
        private static string _folder = RelicFolder;

        [MenuItem("Tools/Relic/Create Default Relics")]
        public static void CreateDefaultRelics()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Create Default Relics",
                $"{RelicFolder}/ 아래에 전투 유물 54종(재화 유물 제외)을 생성/갱신합니다.\n\n계속하겠습니까?",
                "Create",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            _folder = RelicFolder;
            EnsureFolder(RelicFolder);
            _built = 0;

            BuildSymbolRelics();
            BuildGroupRelics();
            BuildPatternRelics();
            BuildStatusRelics();
            BuildDefensiveRelics();
            BuildGrowthRelics();
            BuildRiskRelics();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Relic] 전투 유물 생성 완료: {_built}종 @ {RelicFolder}");
        }

        [MenuItem("Tools/Relic/Create Currency Relics")]
        public static void CreateCurrencyRelics()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Create Currency Relics",
                $"{CurrencyFolder}/ 아래에 재화 유물 12종(골드·재스핀·상점/보상)을 생성/갱신합니다.\n" +
                "이번 기획에서 재화 시스템을 안 쓰면 이 폴더만 빼면 됩니다.\n\n계속하겠습니까?",
                "Create",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            _folder = CurrencyFolder;
            EnsureFolder(CurrencyFolder);
            _built = 0;

            BuildCurrencyRelics();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Relic] 재화 유물 생성 완료: {_built}종 @ {CurrencyFolder}");
        }

        // ---- A. 심볼 전용 ----
        private static void BuildSymbolRelics()
        {
            Build("cherry_syrup", "체리 시럽", "체리 족보 1개당 HP 3 회복.",
                RelicRarity.Common, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Cherry), Eff(RelicEffectType.HealPlayer, amount: 3));

            Build("cherry_fang", "체리 송곳니", "이번 공격으로 준 피해의 20%만큼 회복.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnAfterDamage, RelicApplyMode.Once,
                CondSymbol(SlotSymbolType.Cherry), Eff(RelicEffectType.HealByDamagePercent, value: 0.2f));

            Build("red_juice", "붉은 과즙", "체리 족보 1개당 추가 피해 +5.",
                RelicRarity.Common, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Cherry), Eff(RelicEffectType.AddFlatDamage, amount: 5));

            Build("lemon_blade", "레몬 칼날", "레몬 족보 1개당 추가 피해 +6.",
                RelicRarity.Common, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Lemon), Eff(RelicEffectType.AddFlatDamage, amount: 6));

            Build("sour_bomb", "신맛 폭탄", "레몬 족보 발동 시 적에게 화상 부여.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondSymbol(SlotSymbolType.Lemon), Eff(RelicEffectType.ApplyBurn));

            Build("lemon_press", "레몬 압축기", "한 턴에 레몬 족보 2개 이상이면 추가 피해 +15.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondSymbol(SlotSymbolType.Lemon, min: 2), Eff(RelicEffectType.AddFlatDamage, amount: 15));

            Build("lucky_seven", "행운의 7", "7 족보 피해 25% 증가.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Seven), Eff(RelicEffectType.MultiplyDamage, value: 1.25f));

            Build("jackpot_hunch", "잭팟 예감", "7 족보당 25% 확률로 추가 피해 +20.",
                RelicRarity.Rare, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Seven), Eff(RelicEffectType.ChanceAddDamage, amount: 20, chance: 0.25f));

            Build("clover_charm", "네잎 부적", "클로버 족보 1개당 방어도 +4.",
                RelicRarity.Common, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Clover), Eff(RelicEffectType.AddShield, amount: 4));

            Build("clover_barrier", "클로버 보호막", "클로버 족보 발동 시 20% 확률로 방어도 +10.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondSymbol(SlotSymbolType.Clover), Eff(RelicEffectType.AddShield, amount: 10, chance: 0.2f));

            Build("golden_bell", "황금 종", "종 족보 1개당 다음 턴 기본 공격력 +2.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Bell), Eff(RelicEffectType.AddNextTurnBaseDamage, amount: 2));

            Build("echo_amplifier", "울림 증폭기", "종 족보 1개당 이번 턴 추가 피해 +3.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Bell), Eff(RelicEffectType.AddFlatDamage, amount: 3));

            Build("warning_bell", "경고의 종", "종 족보 발동 시 다음 적 공격 피해 -2.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondSymbol(SlotSymbolType.Bell), Eff(RelicEffectType.ReduceEnemyAttack, amount: 2));

            Build("sharp_diamond", "날카로운 다이아", "다이아 족보 1개당 추가 피해 +5.",
                RelicRarity.Common, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Diamond), Eff(RelicEffectType.AddFlatDamage, amount: 5));

            Build("frozen_diamond", "얼어붙은 다이아", "다이아 족보 발동 시 30% 확률로 빙결 부여.",
                RelicRarity.Rare, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondSymbol(SlotSymbolType.Diamond), Eff(RelicEffectType.ApplyFreeze, chance: 0.3f));
        }

        // ---- B. 그룹 ----
        private static void BuildGroupRelics()
        {
            Build("fruit_basket", "과일바구니", "과일 족보 1개당 추가 피해 +3.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondGroup(RelicSymbolGroup.Fruit), Eff(RelicEffectType.AddFlatDamage, amount: 3));

            Build("sweet_sour_set", "새콤달콤 세트", "같은 턴 체리·레몬 족보 모두 발동 시 HP 5 회복 + 추가 피해 +10.",
                RelicRarity.Rare, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondMulti(SlotSymbolType.Cherry, SlotSymbolType.Lemon),
                Eff(RelicEffectType.HealPlayer, amount: 5), Eff(RelicEffectType.AddFlatDamage, amount: 10));

            Build("juice_burst", "과즙 폭발", "한 턴에 과일 족보 2개 이상이면 추가 피해 +12.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondGroup(RelicSymbolGroup.Fruit, min: 2), Eff(RelicEffectType.AddFlatDamage, amount: 12));

            Build("fermented_juice", "발효 과즙", "과일 족보 1개당 독 1스택 부여.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondGroup(RelicSymbolGroup.Fruit), Eff(RelicEffectType.ApplyPoison, amount: 1));

            Build("fruit_concentrate", "과일 농축액", "슬롯 풀에 과일 심볼 8개 이상이면 과일 피해 20% 증가.",
                RelicRarity.Rare, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondPoolGroup(RelicSymbolGroup.Fruit, 8), Eff(RelicEffectType.MultiplyDamage, value: 1.2f));

            Build("luck_charm", "행운의 부적", "행운 족보당 20% 확률로 추가 피해 +10.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondGroup(RelicSymbolGroup.Luck), Eff(RelicEffectType.ChanceAddDamage, amount: 10, chance: 0.2f));

            Build("gamblers_hand", "도박사의 손", "행운 족보 발동 시 이번 피해에 50%~150% 랜덤 배율.",
                RelicRarity.Rare, RelicTriggerTiming.OnBeforeDamage, RelicApplyMode.Once,
                CondGroup(RelicSymbolGroup.Luck), Eff(RelicEffectType.Custom, customId: "gamblers_hand"));

            Build("fate_rigger", "운명 조작기", "슬롯 풀에 행운 심볼 8개 이상이면 행운 피해 30% 증가.",
                RelicRarity.Rare, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondPoolGroup(RelicSymbolGroup.Luck, 8), Eff(RelicEffectType.MultiplyDamage, value: 1.3f));

            Build("golden_echo", "황금 울림", "보물 족보 1개당 추가 피해 +4, 골드 +4.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondGroup(RelicSymbolGroup.Treasure),
                Eff(RelicEffectType.AddFlatDamage, amount: 4), Eff(RelicEffectType.AddGold, amount: 4));

            Build("gem_chime", "보석 종소리", "보물 족보 1개당 방어도 +3.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondGroup(RelicSymbolGroup.Treasure), Eff(RelicEffectType.AddShield, amount: 3));

            Build("greed_vault", "탐욕의 금고", "슬롯 풀에 보물 심볼 8개 이상이면 보물 피해 20% 증가, 골드 +5.",
                RelicRarity.Rare, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondPoolGroup(RelicSymbolGroup.Treasure, 8),
                Eff(RelicEffectType.MultiplyDamage, value: 1.2f), Eff(RelicEffectType.AddGold, amount: 5));
        }

        // ---- C. 패턴 ----
        private static void BuildPatternRelics()
        {
            Build("horizontal_sense", "수평 감각", "가로 족보 1개당 추가 피해 +4.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondPattern(SlotPatternRank.HorizontalSm, SlotPatternRank.HorizontalLg, SlotPatternRank.HorizontalXL),
                Eff(RelicEffectType.AddFlatDamage, amount: 4));

            Build("vertical_sense", "수직 감각", "세로 족보 1개당 방어도 +4.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondPattern(SlotPatternRank.Vertical), Eff(RelicEffectType.AddShield, amount: 4));

            Build("long_line_bonus", "긴 줄 보너스", "가로 L/XL 족보 피해 30% 증가.",
                RelicRarity.Rare, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondPattern(SlotPatternRank.HorizontalLg, SlotPatternRank.HorizontalXL),
                Eff(RelicEffectType.MultiplyDamage, value: 1.3f));

            Build("zigzag_circuit", "지그재그 회로", "지그/재그 족보 1개당 독 1스택 + 추가 피해 +5.",
                RelicRarity.Rare, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondPattern(SlotPatternRank.Zig, SlotPatternRank.Zag),
                Eff(RelicEffectType.ApplyPoison, amount: 1), Eff(RelicEffectType.AddFlatDamage, amount: 5));

            Build("perfect_array", "완벽한 배열", "한 턴에 족보 3개 이상이면 최종 피해 20% 증가.",
                RelicRarity.Rare, RelicTriggerTiming.OnBeforeDamage, RelicApplyMode.Once,
                CondPatternCount(3, PatternCountMode.Total), Eff(RelicEffectType.MultiplyDamage, value: 1.2f));

            Build("combo_engine", "콤보 엔진", "한 턴에 서로 다른 심볼 족보 2종 이상이면 추가 피해 +10.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondPatternCount(2, PatternCountMode.DistinctSymbols), Eff(RelicEffectType.AddFlatDamage, amount: 10));

            Build("duplicate_calc", "중복 계산기", "같은 심볼 족보 2개 이상이면 추가 피해 +10.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondPatternCount(2, PatternCountMode.SameSymbolRepeat), Eff(RelicEffectType.AddFlatDamage, amount: 10));

            Build("pattern_copier", "패턴 복사기", "족보 1개 이상 발동 시 가장 높은 피해 족보를 한 번 더 추가.",
                RelicRarity.Rare, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondAny(), Eff(RelicEffectType.Custom, customId: "copy_highest_pattern_damage"));
        }

        // ---- D. 속성 ----
        private static void BuildStatusRelics()
        {
            Build("burning_lemon", "불타는 레몬", "레몬 족보 발동 시 화상 부여.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondSymbol(SlotSymbolType.Lemon), Eff(RelicEffectType.ApplyBurn));

            Build("cherry_poison", "체리 독주", "체리 족보 1개당 독 1스택 부여.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Cherry), Eff(RelicEffectType.ApplyPoison, amount: 1));

            Build("frozen_bell", "얼어붙은 종", "종 족보 발동 시 빙결 부여.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondSymbol(SlotSymbolType.Bell), Eff(RelicEffectType.ApplyFreeze));

            Build("toxic_fruit_basket", "독성 과일바구니", "과일 족보 발동 시 독 1스택 부여.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondGroup(RelicSymbolGroup.Fruit), Eff(RelicEffectType.ApplyPoison, amount: 1));

            Build("cold_treasure", "차가운 보물", "보물 족보 발동 시 25% 확률로 빙결 부여.",
                RelicRarity.Rare, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondGroup(RelicSymbolGroup.Treasure), Eff(RelicEffectType.ApplyFreeze, chance: 0.25f));

            Build("ember_spreader", "불씨 확산기", "적이 화상 상태일 때 족보 발동 시 추가 피해 +6.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondAny(RelicStatusType.Burn), Eff(RelicEffectType.AddFlatDamage, amount: 6));

            Build("venom_amplifier", "맹독 증폭기", "적이 독 상태일 때 족보 발동 시 독 1스택 추가.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondAny(RelicStatusType.Poison), Eff(RelicEffectType.ApplyPoison, amount: 1));

            Build("frost_shard", "냉기 파편", "적이 빙결 상태일 때 족보 발동 시 추가 피해 +10.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondAny(RelicStatusType.Freeze), Eff(RelicEffectType.AddFlatDamage, amount: 10));
        }

        // ---- E. 방어 / 생존 ----
        private static void BuildDefensiveRelics()
        {
            Build("protective_bell", "보호의 종", "종 족보 1개당 방어도 +5.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Bell), Eff(RelicEffectType.AddShield, amount: 5));

            Build("gem_armor", "보석 갑옷", "다이아 족보 1개당 방어도 +4.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Diamond), Eff(RelicEffectType.AddShield, amount: 4));

            Build("fruit_first_aid", "과일 응급상자", "한 턴에 과일 족보 2개 이상이면 HP 5 회복.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondGroup(RelicSymbolGroup.Fruit, min: 2), Eff(RelicEffectType.HealPlayer, amount: 5));

            Build("last_cherry", "마지막 체리", "HP 30% 이하에서 체리 족보 발동 시 HP 8 회복.",
                RelicRarity.Rare, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondSymbol(SlotSymbolType.Cherry, hpGate: 30), Eff(RelicEffectType.HealPlayer, amount: 8));

            Build("safety_bell", "안전벨", "한 턴에 족보가 하나도 없으면 방어도 +3.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondNoPattern(), Eff(RelicEffectType.AddShield, amount: 3));

            Build("emergency_button", "비상 버튼", "전투당 1회, 사망 시 HP 1로 부활.",
                RelicRarity.Legendary, RelicTriggerTiming.OnPlayerDeath, RelicApplyMode.Once,
                CondAlways(), Eff(RelicEffectType.Custom, customId: "revive_once"));
        }

        // ---- F. 성장 (재화 유물은 BuildCurrencyRelics로 분리) ----
        private static void BuildGrowthRelics()
        {
            Build("compact_storage", "압축 보관함", "슬롯 풀 심볼 종류가 4종 이하면 최종 피해 25% 증가.",
                RelicRarity.Rare, RelicTriggerTiming.OnBeforeDamage, RelicApplyMode.Once,
                CondPoolDistinctAtMost(4), Eff(RelicEffectType.MultiplyDamage, value: 1.25f));
        }

        // ---- 재화 / 경제 (별도 메뉴·폴더. 이번 기획에서 제외 가능) ----
        private static void BuildCurrencyRelics()
        {
            Build("luck_vault", "행운 저장고", "클로버 족보 1개당 골드 +8.",
                RelicRarity.Common, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Clover), Eff(RelicEffectType.AddGold, amount: 8));

            Build("gem_pouch", "보석 주머니", "다이아 족보 1개당 골드 +12.",
                RelicRarity.Common, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Diamond), Eff(RelicEffectType.AddGold, amount: 12));

            Build("diagonal_luck", "대각선의 행운", "대각 족보 1개당 골드 +10.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondPattern(SlotPatternRank.Diagonal), Eff(RelicEffectType.AddGold, amount: 10));

            Build("odd_odds", "별난 확률표", "행운 족보당 15% 확률로 골드 +30.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondGroup(RelicSymbolGroup.Luck), Eff(RelicEffectType.AddGold, amount: 30, chance: 0.15f));

            Build("treasure_chest", "보물 상자", "보물 족보 1개당 골드 +8.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondGroup(RelicSymbolGroup.Treasure), Eff(RelicEffectType.AddGold, amount: 8));

            Build("kings_vault", "왕의 금고", "같은 턴 종·다이아 족보 모두 발동 시 골드 +25.",
                RelicRarity.Rare, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondMulti(SlotSymbolType.Bell, SlotSymbolType.Diamond), Eff(RelicEffectType.AddGold, amount: 25));

            Build("lucky_chain", "럭키 체인", "같은 턴 7·클로버 족보 모두 발동 시 재스핀 티켓 1개 획득.",
                RelicRarity.Rare, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.Once,
                CondMulti(SlotSymbolType.Seven, SlotSymbolType.Clover), Eff(RelicEffectType.GainRespin, amount: 1));

            Build("piggy_bank", "저금통", "전투 승리 시 골드 +10.",
                RelicRarity.Common, RelicTriggerTiming.OnBattleWin, RelicApplyMode.Once,
                CondAlways(), Eff(RelicEffectType.AddGold, amount: 10));

            Build("rich_sword", "부자의 검", "보유 골드 10당 추가 피해 +1(최대 +10).",
                RelicRarity.Rare, RelicTriggerTiming.OnBeforeDamage, RelicApplyMode.Once,
                CondAlways(), Eff(RelicEffectType.Custom, customId: "rich_sword_damage"));

            Build("merchant_eye", "상인의 눈", "보상 선택지 +1. (보상 시스템 연결 전 placeholder)",
                RelicRarity.Uncommon, RelicTriggerTiming.OnBattleWin, RelicApplyMode.Once,
                CondAlways(), Eff(RelicEffectType.Custom, customId: "merchant_eye"));

            Build("cleanup_expert", "정리 전문가", "상점 심볼 제거 비용 50% 감소. (상점 시스템 연결 전 placeholder)",
                RelicRarity.Uncommon, RelicTriggerTiming.OnBattleStart, RelicApplyMode.Once,
                CondAlways(), Eff(RelicEffectType.Custom, customId: "shop_removal_discount"));

            Build("collectors_bag", "수집가의 가방", "슬롯 풀 총량 25개 이상이면 전투 승리 시 골드 +10.",
                RelicRarity.Uncommon, RelicTriggerTiming.OnBattleWin, RelicApplyMode.Once,
                CondPoolTotalAtLeast(25), Eff(RelicEffectType.AddGold, amount: 10));
        }

        // ---- G. 리스크 보상 ----
        private static void BuildRiskRelics()
        {
            Build("cursed_seven", "저주받은 7", "7 족보 피해 50% 증가. (패널티: 전투 시작 시 저주 심볼 추가 — 연결 예정)",
                RelicRarity.Cursed, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Seven), Eff(RelicEffectType.MultiplyDamage, value: 1.5f));

            Build("broken_diamond", "깨진 다이아", "다이아 족보 1개당 추가 피해 +15. (다이아 골드 효과 비활성 — 단순화)",
                RelicRarity.Cursed, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondSymbol(SlotSymbolType.Diamond), Eff(RelicEffectType.AddFlatDamage, amount: 15));

            Build("rotten_fruit_basket", "썩은 과일바구니", "과일 족보 피해 40% 증가. 과일 족보 1개당 플레이어 HP 1 감소.",
                RelicRarity.Cursed, RelicTriggerTiming.OnPatternResolved, RelicApplyMode.PerMatchedPattern,
                CondGroup(RelicSymbolGroup.Fruit),
                Eff(RelicEffectType.MultiplyDamage, value: 1.4f), Eff(RelicEffectType.PlayerTakeDamage, amount: 1));

            Build("blood_slot", "피의 슬롯", "모든 족보 피해 30% 증가. 공격마다 플레이어 HP 1 감소.",
                RelicRarity.Cursed, RelicTriggerTiming.OnBeforeDamage, RelicApplyMode.Once,
                CondAlways(),
                Eff(RelicEffectType.MultiplyDamage, value: 1.3f), Eff(RelicEffectType.PlayerTakeDamage, amount: 1));

            Build("unstable_machine", "불안정한 머신", "족보 1개 이상이면 최종 피해 50% 증가, 없으면 플레이어 5 피해.",
                RelicRarity.Cursed, RelicTriggerTiming.OnBeforeDamage, RelicApplyMode.Once,
                CondAlways(), Eff(RelicEffectType.Custom, customId: "unstable_machine"));
        }

        // ======================= 조건 팩토리 =======================

        private static RelicConditionData CondAlways()
        {
            return MakeCondition(RelicConditionType.Always);
        }

        private static RelicConditionData CondAny(RelicStatusType statusGate = RelicStatusType.None)
        {
            RelicConditionData condition = MakeCondition(RelicConditionType.AnyPattern);
            SetField(condition, "_enemyStatusGate", statusGate);
            return condition;
        }

        private static RelicConditionData CondSymbol(
            SlotSymbolType symbol, int min = 1, int hpGate = 0, RelicStatusType statusGate = RelicStatusType.None)
        {
            RelicConditionData condition = MakeCondition(RelicConditionType.SpecificSymbol);
            SetField(condition, "_targetSymbol", symbol);
            SetField(condition, "_minCount", min);
            SetField(condition, "_hpBelowPercentGate", hpGate);
            SetField(condition, "_enemyStatusGate", statusGate);
            return condition;
        }

        private static RelicConditionData CondGroup(RelicSymbolGroup group, int min = 1)
        {
            RelicConditionData condition = MakeCondition(RelicConditionType.SymbolGroup);
            SetField(condition, "_group", group);
            SetField(condition, "_minCount", min);
            return condition;
        }

        private static RelicConditionData CondMulti(params SlotSymbolType[] symbols)
        {
            RelicConditionData condition = MakeCondition(RelicConditionType.MultipleSpecificSymbolsInSameTurn);
            SetField(condition, "_symbols", new List<SlotSymbolType>(symbols));
            return condition;
        }

        private static RelicConditionData CondPattern(params SlotPatternRank[] ranks)
        {
            RelicConditionData condition = MakeCondition(RelicConditionType.SpecificPattern);
            SetField(condition, "_patternRanks", new List<SlotPatternRank>(ranks));
            return condition;
        }

        private static RelicConditionData CondPatternCount(int min, PatternCountMode mode)
        {
            RelicConditionData condition = MakeCondition(RelicConditionType.PatternCount);
            SetField(condition, "_minCount", min);
            SetField(condition, "_patternCountMode", mode);
            return condition;
        }

        private static RelicConditionData CondNoPattern()
        {
            return MakeCondition(RelicConditionType.NoPattern);
        }

        private static RelicConditionData CondPoolGroup(RelicSymbolGroup group, int min)
        {
            RelicConditionData condition = MakeCondition(RelicConditionType.SymbolCountInPool);
            SetField(condition, "_poolQueryMode", PoolQueryMode.GroupSymbolCountAtLeast);
            SetField(condition, "_group", group);
            SetField(condition, "_minCount", min);
            return condition;
        }

        private static RelicConditionData CondPoolDistinctAtMost(int max)
        {
            RelicConditionData condition = MakeCondition(RelicConditionType.SymbolCountInPool);
            SetField(condition, "_poolQueryMode", PoolQueryMode.DistinctSymbolTypesAtMost);
            SetField(condition, "_minCount", max);
            return condition;
        }

        private static RelicConditionData CondPoolTotalAtLeast(int min)
        {
            RelicConditionData condition = MakeCondition(RelicConditionType.SymbolCountInPool);
            SetField(condition, "_poolQueryMode", PoolQueryMode.TotalSymbolCountAtLeast);
            SetField(condition, "_minCount", min);
            return condition;
        }

        private static RelicConditionData MakeCondition(RelicConditionType type)
        {
            var condition = new RelicConditionData();
            SetField(condition, "_type", type);
            return condition;
        }

        // ======================= 효과 팩토리 =======================

        private static RelicEffectData Eff(
            RelicEffectType type, int amount = 0, float value = 0f, float chance = 0f, string customId = "")
        {
            var effect = new RelicEffectData();
            SetField(effect, "_type", type);
            SetField(effect, "_amount", amount);
            SetField(effect, "_value", value);
            SetField(effect, "_chance", chance);
            SetField(effect, "_customId", customId ?? string.Empty);
            return effect;
        }

        // ======================= 에셋 빌드 =======================

        private static void Build(
            string id,
            string name,
            string description,
            RelicRarity rarity,
            RelicTriggerTiming timing,
            RelicApplyMode mode,
            RelicConditionData condition,
            params RelicEffectData[] effects)
        {
            string path = $"{_folder}/{id}.asset";
            RelicDataSO so = AssetDatabase.LoadAssetAtPath<RelicDataSO>(path);
            bool isNew = so == null;

            if (isNew)
            {
                so = ScriptableObject.CreateInstance<RelicDataSO>();
            }

            SetField(so, "_relicId", id);
            SetField(so, "_relicName", name);
            SetField(so, "_description", description);
            SetField(so, "_rarity", rarity);
            SetField(so, "_triggerTiming", timing);
            SetField(so, "_applyMode", mode);
            SetField(so, "_condition", condition);
            SetField(so, "_effects", new List<RelicEffectData>(effects));

            if (isNew)
            {
                AssetDatabase.CreateAsset(so, path);
            }
            else
            {
                EditorUtility.SetDirty(so);
            }

            _built++;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                Debug.LogError($"[Relic] 필드를 찾지 못했습니다: {target.GetType().Name}.{fieldName}");
                return;
            }

            field.SetValue(target, value);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
