using System;
using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Core
{
    public sealed class SlotMachineService
    {
        private static readonly SlotSymbolType[] SymbolPool =
        {
            SlotSymbolType.Cherry,
            SlotSymbolType.Seven,
            SlotSymbolType.Grape,
            SlotSymbolType.Bell,
            SlotSymbolType.Clover,
            SlotSymbolType.Lemon
        };

        public SlotMachineService()
            : this(new Random())
        {
        }

        public SlotMachineService(Random random)
        {
            _random = random ?? new Random();
        }

        public SlotSpinResult Spin()
        {
            return Spin(luck: 0, jackpotExclude: null);
        }

        public SlotSpinResult Spin(int luck, ISet<SlotSymbolType> jackpotExclude = null)
        {
            if (luck >= SlotSpinResult.CellCount)
            {
                return SpinAllSame(jackpotExclude);
            }

            if (luck <= 0)
            {
                return SpinAllRandom();
            }

            SlotPatternDefinition forcedPattern = SlotPatternCatalog.PickForcedPattern(_random, luck);

            if (forcedPattern == null)
            {
                return SpinAllRandom();
            }

            return SpinWithForcedPattern(forcedPattern);
        }

        private SlotSpinResult SpinAllRandom()
        {
            var symbols = new SlotSymbolType[SlotSpinResult.CellCount];

            for (int index = 0; index < symbols.Length; index++)
            {
                symbols[index] = PickSymbol(null);
            }

            return new SlotSpinResult(symbols);
        }

        private SlotSpinResult SpinAllSame(ISet<SlotSymbolType> exclude)
        {
            SlotSymbolType symbol = PickSymbol(exclude);
            var symbols = new SlotSymbolType[SlotSpinResult.CellCount];

            for (int index = 0; index < symbols.Length; index++)
            {
                symbols[index] = symbol;
            }

            return new SlotSpinResult(symbols);
        }

        private SlotSpinResult SpinWithForcedPattern(SlotPatternDefinition forcedPattern)
        {
            SlotSymbolType symbol = PickSymbol(null);
            var forcedIndices = new HashSet<int>();

            foreach (SlotCell cell in forcedPattern.Cells)
            {
                forcedIndices.Add(SlotSpinResult.ToIndex(cell.Col, cell.Row));
            }

            var symbols = new SlotSymbolType[SlotSpinResult.CellCount];

            for (int index = 0; index < symbols.Length; index++)
            {
                symbols[index] = forcedIndices.Contains(index) ? symbol : PickSymbol(null);
            }

            return new SlotSpinResult(symbols);
        }

        private SlotSymbolType PickSymbol(ISet<SlotSymbolType> exclude)
        {
            if (exclude == null || exclude.Count == 0)
            {
                return SymbolPool[_random.Next(SymbolPool.Length)];
            }

            var allowedSymbols = new List<SlotSymbolType>(SymbolPool.Length);

            foreach (SlotSymbolType symbol in SymbolPool)
            {
                if (!exclude.Contains(symbol))
                {
                    allowedSymbols.Add(symbol);
                }
            }

            if (allowedSymbols.Count == 0)
            {
                return SymbolPool[_random.Next(SymbolPool.Length)];
            }

            return allowedSymbols[_random.Next(allowedSymbols.Count)];
        }

        private readonly Random _random;
    }
}
