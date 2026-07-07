using System;
using System.Collections.Generic;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    public enum RunProposalEffectKind
    {
        None = 0,
        SymbolWeight = 1,
        SymbolBaseDamage = 2,
        RunCoins = 3,
        RelicSlotCapacity = 4,

        /// <summary>전투 엔진(RelicSpecRunner) 효과 — 제안 id로 스펙을 찾아 영구 누적(족보 피해·재발동 등).</summary>
        EngineEffect = 5,
    }

    public enum RunRewardKind
    {
        Stat = 0,    // 영구 스탯/효과 (RunRewardType)
        Symbol = 1,  // 심볼별 한 칸 출현 확률값 변경
        Relic = 2,   // v23 유물 획득
        Proposal = 3,
    }

    public sealed class RunRewardDefinition : IEquatable<RunRewardDefinition>
    {
        private readonly SlotSymbolType[] _symbols;

        /// <summary>v23 유물 보상.</summary>
        public RunRewardDefinition(RelicDefinition relic)
        {
            Kind = RunRewardKind.Relic;
            Relic = relic;
            DisplayName = relic != null ? relic.Name : "유물";
            Description = RelicDisplay.BuildSelectionDescription(relic);
            Rarity = relic != null
                ? RewardRarityMap.FromGrade(relic.Grade)
                : RewardRarity.Common;
            _symbols = Array.Empty<SlotSymbolType>();
        }

        /// <summary>스탯/효과 보상.</summary>
        public RunRewardDefinition(RunRewardType type, string displayName, string description)
        {
            Kind = RunRewardKind.Stat;
            Type = type;
            DisplayName = displayName;
            Description = description;
            Rarity = RewardRarity.Common;
            _symbols = Array.Empty<SlotSymbolType>();
        }

        /// <summary>심볼별 한 칸 출현 확률값 증가/감소 보상.</summary>
        public RunRewardDefinition(SlotSymbolType symbol, int amount, string displayName, string description)
        {
            Kind = RunRewardKind.Symbol;
            Symbol = symbol;
            Amount = amount;
            DisplayName = displayName;
            Description = description;
            Rarity = RewardRarity.Common;
            _symbols = new[] { symbol };
        }

        public RunRewardDefinition(
            string proposalId,
            string category,
            RewardRarity rarity,
            RunProposalEffectKind proposalEffect,
            IReadOnlyList<SlotSymbolType> symbols,
            int amount,
            string displayName,
            string description)
        {
            Kind = RunRewardKind.Proposal;
            ProposalId = proposalId ?? string.Empty;
            Category = category ?? string.Empty;
            ProposalEffect = proposalEffect;
            Amount = amount;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            Rarity = rarity;
            _symbols = CopySymbols(symbols);
        }

        public RunRewardKind Kind { get; }

        public RunRewardType Type { get; }

        public RelicDefinition Relic { get; }

        public SlotSymbolType Symbol { get; }

        public int Amount { get; }

        public string ProposalId { get; }

        public string Category { get; }

        public RunProposalEffectKind ProposalEffect { get; }

        public IReadOnlyList<SlotSymbolType> Symbols => _symbols;

        public string DisplayName { get; }

        public string Description { get; }

        /// <summary>표시용 등급(테두리/배경 색 결정).</summary>
        public RewardRarity Rarity { get; }

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
                RunRewardKind.Proposal => string.Equals(
                    ProposalId,
                    other.ProposalId,
                    StringComparison.Ordinal),
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
                    RunRewardKind.Proposal =>
                        (hashCode * 397) ^
                        StringComparer.Ordinal.GetHashCode(ProposalId ?? string.Empty),
                    _ => (hashCode * 397) ^ (int)Type,
                };
            }
        }

        private static SlotSymbolType[] CopySymbols(IReadOnlyList<SlotSymbolType> symbols)
        {
            if (symbols == null || symbols.Count == 0)
            {
                return Array.Empty<SlotSymbolType>();
            }

            var copy = new SlotSymbolType[symbols.Count];
            for (int index = 0; index < symbols.Count; index++)
            {
                copy[index] = symbols[index];
            }

            return copy;
        }

    }

    public static class SlotSymbolIconKeys
    {
        public const string HighlightSheetAddress = "Symbol Sheet Highlight";
        public const string NormalSheetAddress = "Symbol Sheet Normal";
        public const string AnimationSheetAddress = "Symbol Sheet Animation";
        public const string TmpSpriteAssetAddress = "Symbols-Sheet-TMP";

        public const string Cherry = HighlightSheetAddress + "[icon-Sheet2_0]";
        public const string Seven = HighlightSheetAddress + "[icon-Sheet2_1]";
        public const string Diamond = HighlightSheetAddress + "[icon-Sheet2_2]";
        public const string Bell = HighlightSheetAddress + "[icon-Sheet2_3]";
        public const string Clover = HighlightSheetAddress + "[icon-Sheet2_4]";
        public const string Lemon = HighlightSheetAddress + "[icon-Sheet2_5]";

        public static readonly string[] HighlightSpriteKeys =
        {
            Cherry,
            Seven,
            Diamond,
            Bell,
            Clover,
            Lemon,
        };

        public static readonly string[] NormalSpriteKeys =
        {
            NormalSheetAddress + "[Icon-Sheet_0]",
            NormalSheetAddress + "[Icon-Sheet_1]",
            NormalSheetAddress + "[Icon-Sheet_2]",
            NormalSheetAddress + "[Icon-Sheet_3]",
            NormalSheetAddress + "[Icon-Sheet_4]",
            NormalSheetAddress + "[Icon-Sheet_5]",
        };

        public static readonly string[] AnimationSpriteKeys =
        {
            AnimationSheetAddress + "[icon-Sheet-ani_0]",
            AnimationSheetAddress + "[icon-Sheet-ani_1]",
            AnimationSheetAddress + "[icon-Sheet-ani_2]",
            AnimationSheetAddress + "[icon-Sheet-ani_3]",
            AnimationSheetAddress + "[icon-Sheet-ani_4]",
            AnimationSheetAddress + "[icon-Sheet-ani_5]",
        };

        public static string For(SlotSymbolType symbol)
        {
            switch (symbol)
            {
                case SlotSymbolType.Seven:
                    return Seven;
                case SlotSymbolType.Diamond:
                    return Diamond;
                case SlotSymbolType.Bell:
                    return Bell;
                case SlotSymbolType.Clover:
                    return Clover;
                case SlotSymbolType.Lemon:
                    return Lemon;
                default:
                    return Cherry;
            }
        }

        public static string NormalFor(SlotSymbolType symbol)
        {
            int index = (int)symbol;
            if (index >= 0 && index < NormalSpriteKeys.Length)
            {
                return NormalSpriteKeys[index];
            }

            return NormalSpriteKeys[0];
        }

        public static int TmpSpriteIndexFor(SlotSymbolType symbol)
        {
            int index = (int)symbol;
            if (index >= 0 && index < NormalSpriteKeys.Length)
            {
                return index;
            }

            return 0;
        }
    }
}
