using System;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    public enum RunRewardKind
    {
        Stat = 0,    // 영구 스탯/효과 (RunRewardType)
        Symbol = 1,  // 슬롯 풀에 심볼 추가
        Relic = 2,   // v23 유물 획득
    }

    public sealed class RunRewardDefinition : IEquatable<RunRewardDefinition>
    {
        /// <summary>v23 유물 보상.</summary>
        public RunRewardDefinition(RelicDefinition relic)
        {
            Kind = RunRewardKind.Relic;
            Relic = relic;
            DisplayName = relic != null ? relic.Name : "유물";
            Description = RelicDisplay.BuildDescription(relic);
            IconKey = relic != null ? relic.IconKey : string.Empty;
        }

        /// <summary>스탯/효과 보상.</summary>
        public RunRewardDefinition(RunRewardType type, string displayName, string description)
        {
            Kind = RunRewardKind.Stat;
            Type = type;
            DisplayName = displayName;
            Description = description;
            IconKey = string.Empty;
        }

        /// <summary>슬롯 풀 심볼 추가 보상.</summary>
        public RunRewardDefinition(SlotSymbolType symbol, int amount, string displayName, string description)
        {
            Kind = RunRewardKind.Symbol;
            Symbol = symbol;
            Amount = amount;
            DisplayName = displayName;
            Description = description;
            IconKey = string.Empty;
        }

        public RunRewardKind Kind { get; }

        public RunRewardType Type { get; }

        public RelicDefinition Relic { get; }

        public SlotSymbolType Symbol { get; }

        public int Amount { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public string IconKey { get; }

        public bool Equals(RunRewardDefinition other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null || Kind != other.Kind)
            {
                return false;
            }

            return Kind switch
            {
                RunRewardKind.Relic => string.Equals(
                    Relic?.Id,
                    other.Relic?.Id,
                    StringComparison.Ordinal),
                RunRewardKind.Symbol => Symbol == other.Symbol && Amount == other.Amount,
                _ => Type == other.Type,
            };
        }

        public override bool Equals(object obj)
        {
            return obj is RunRewardDefinition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Kind;
                return Kind switch
                {
                    RunRewardKind.Relic =>
                        (hashCode * 397) ^
                        StringComparer.Ordinal.GetHashCode(Relic?.Id ?? string.Empty),
                    RunRewardKind.Symbol =>
                        ((hashCode * 397) ^ (int)Symbol) * 397 ^ Amount,
                    _ => (hashCode * 397) ^ (int)Type,
                };
            }
        }
    }
}
