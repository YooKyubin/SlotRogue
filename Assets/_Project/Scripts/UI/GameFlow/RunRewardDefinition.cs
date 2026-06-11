using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    public enum RunRewardKind
    {
        Stat = 0,    // 영구 스탯/효과 (RunRewardType)
        Symbol = 1,  // 슬롯 풀에 심볼 추가
        Relic = 2,   // v20.3 유물 획득
    }

    public sealed class RunRewardDefinition
    {
        /// <summary>v20.3 유물 보상.</summary>
        public RunRewardDefinition(RelicDefinition relic)
        {
            Kind = RunRewardKind.Relic;
            Relic = relic;
            DisplayName = relic != null ? relic.Name : "유물";
            Description = RelicDisplay.BuildDescription(relic);
        }

        /// <summary>스탯/효과 보상.</summary>
        public RunRewardDefinition(RunRewardType type, string displayName, string description)
        {
            Kind = RunRewardKind.Stat;
            Type = type;
            DisplayName = displayName;
            Description = description;
        }

        /// <summary>슬롯 풀 심볼 추가 보상.</summary>
        public RunRewardDefinition(SlotSymbolType symbol, int amount, string displayName, string description)
        {
            Kind = RunRewardKind.Symbol;
            Symbol = symbol;
            Amount = amount;
            DisplayName = displayName;
            Description = description;
        }

        public RunRewardKind Kind { get; }

        public RunRewardType Type { get; }

        public RelicDefinition Relic { get; }

        public SlotSymbolType Symbol { get; }

        public int Amount { get; }

        public string DisplayName { get; }

        public string Description { get; }
    }
}
