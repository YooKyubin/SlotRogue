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
            SlotSymbolType.Diamond,
            SlotSymbolType.Bell,
            SlotSymbolType.Clover,
            SlotSymbolType.Lemon
        };

        public SlotMachineService()
            : this(new Random(), null)
        {
        }

        public SlotMachineService(Random random)
            : this(random, null)
        {
        }

        /// <summary>
        /// 가변 심볼 풀을 사용하려면 <paramref name="pool"/>을 전달합니다.
        /// null이면 종래의 균등 추첨(고정 6심볼)을 사용합니다.
        /// </summary>
        public SlotMachineService(Random random, SlotSymbolPool pool)
        {
            _random = random ?? new Random();
            _pool = pool;
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
            // 가변 풀이 있으면 개수 비례 가중 추첨.
            if (_pool != null)
            {
                return _pool.Draw(_random, exclude);
            }

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
        private readonly SlotSymbolPool _pool;
    }
}
