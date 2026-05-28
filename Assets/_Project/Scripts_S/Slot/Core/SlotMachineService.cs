using System;

using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Core
{
    public sealed class SlotMachineService
    {
        private static readonly SlotSymbolType[] SymbolPool =
        {
            SlotSymbolType.Sword,
            SlotSymbolType.Shield,
            SlotSymbolType.Heart,
            SlotSymbolType.Coin,
            SlotSymbolType.Gem,
            SlotSymbolType.Skull
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
            var symbols = new SlotSymbolType[SlotSpinResult.CellCount];

            for (int index = 0; index < symbols.Length; index++)
            {
                int symbolIndex = _random.Next(0, SymbolPool.Length);
                symbols[index] = SymbolPool[symbolIndex];
            }

            return new SlotSpinResult(symbols);
        }

        private readonly Random _random;
    }
}
