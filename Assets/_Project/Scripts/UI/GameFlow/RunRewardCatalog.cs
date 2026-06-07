using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 보상 카탈로그.
    /// - 일반 전투: 슬롯 풀에 심볼을 추가하는 "빌드 재료" 보상.
    /// - 엘리트/보스(큰 보상): 심볼 추가 + 영구 스탯 강화 혼합. (추후 유물 보상 연결 예정)
    /// 회복은 보상 선택지가 아니라 전투 승리 시 자동 처리(기본 제공)합니다.
    /// </summary>
    public static class RunRewardCatalog
    {
        /// <summary>심볼 추가 보상 1회당 개수. 기획 미정 — 플레이테스트로 조정.</summary>
        private const int SymbolAddAmount = 2;

        // ── 심볼 추가 (일반 전투 보상) ───────────────────────────────────
        private static readonly RunRewardDefinition[] SymbolRewardDefinitions =
        {
            Symbol(SlotSymbolType.Cherry, "체리 더미"),
            Symbol(SlotSymbolType.Seven,  "세븐 더미"),
            Symbol(SlotSymbolType.Grape,  "포도 더미"),
            Symbol(SlotSymbolType.Bell,   "종 더미"),
            Symbol(SlotSymbolType.Clover, "클로버 더미"),
            Symbol(SlotSymbolType.Lemon,  "레몬 더미"),
        };

        // ── 영구 스탯 (큰 보상에 혼합) ───────────────────────────────────
        private static readonly RunRewardDefinition[] StatRewardDefinitions =
        {
            new(RunRewardType.GreaterDamage, "강철 숫돌", "이후 스핀에서 피해 +4."),
            new(RunRewardType.GreaterDefense, "강화 연마제", "이후 스핀에서 방어 +4."),
            new(RunRewardType.MaxHpUp, "영양제", "최대 HP +5 (즉시 5 회복)."),
        };

        /// <summary>일반 전투 보상 풀: 심볼 추가만.</summary>
        public static IReadOnlyList<RunRewardDefinition> NormalRewards => SymbolRewardDefinitions;

        /// <summary>큰 보상 풀(엘리트/보스): 심볼 추가 + 스탯 강화.</summary>
        public static IReadOnlyList<RunRewardDefinition> BigRewards { get; } = BuildBigRewards();

        /// <summary>하위 호환용 전체 목록.</summary>
        public static IReadOnlyList<RunRewardDefinition> All => BigRewards;

        private static RunRewardDefinition Symbol(SlotSymbolType symbol, string displayName) =>
            new(symbol, SymbolAddAmount, displayName, $"슬롯 풀에 {symbol} 심볼 +{SymbolAddAmount}.");

        private static RunRewardDefinition[] BuildBigRewards()
        {
            var list = new List<RunRewardDefinition>();
            list.AddRange(SymbolRewardDefinitions);
            list.AddRange(StatRewardDefinitions);
            return list.ToArray();
        }
    }
}
