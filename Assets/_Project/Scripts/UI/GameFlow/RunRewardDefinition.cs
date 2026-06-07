using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    public enum RunRewardKind
    {
        Stat = 0,    // 영구 스탯/효과 (RunRewardType)
        Symbol = 1,  // 슬롯 풀에 심볼 추가
    }

    public sealed class RunRewardDefinition
    {
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

        public SlotSymbolType Symbol { get; }

        public int Amount { get; }

        public string DisplayName { get; }

        public string Description { get; }
    }
}
