using SlotRogue.Slot.Data;

namespace SlotRogue.UI.GameFlow
{
    internal static class TutorialSlotSpinFactory
    {
        internal static SlotSpinResult CreateOpeningSpin()
        {
            return CreateFirstSpin();
        }

        internal static SlotSpinResult CreateSpin(int zeroBasedSpinIndex)
        {
            switch (zeroBasedSpinIndex)
            {
                case 0:
                    return CreateFirstSpin();
                case 1:
                    return CreateSecondSpin();
                default:
                    return null;
            }
        }

        internal static SlotSpinResult CreateFirstSpin()
        {
            return new SlotSpinResult(new[]
            {
                SlotSymbolType.Cherry,
                SlotSymbolType.Cherry,
                SlotSymbolType.Cherry,
                SlotSymbolType.Seven,
                SlotSymbolType.Diamond,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Bell,
                SlotSymbolType.Clover,
                SlotSymbolType.Seven,
                SlotSymbolType.Diamond,
                SlotSymbolType.Bell,
                SlotSymbolType.Clover,
                SlotSymbolType.Seven,
            });
        }

        internal static SlotSpinResult CreateSecondSpin()
        {
            return new SlotSpinResult(new[]
            {
                SlotSymbolType.Cherry,
                SlotSymbolType.Cherry,
                SlotSymbolType.Cherry,
                SlotSymbolType.Cherry,
                SlotSymbolType.Cherry,
                SlotSymbolType.Seven,
                SlotSymbolType.Lemon,
                SlotSymbolType.Diamond,
                SlotSymbolType.Bell,
                SlotSymbolType.Clover,
                SlotSymbolType.Lemon,
                SlotSymbolType.Seven,
                SlotSymbolType.Clover,
                SlotSymbolType.Diamond,
                SlotSymbolType.Bell,
            });
        }
    }
}
