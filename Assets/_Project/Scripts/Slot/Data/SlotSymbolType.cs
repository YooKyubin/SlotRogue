using System;

namespace SlotRogue.Slot.Data
{
    public enum SlotSymbolType
    {
        Cherry,
        Seven,
        Diamond,
        Bell,
        Clover,
        Lemon
    }

    public static class SlotSymbolAttackValues
    {
        public const int DefaultCherryDamage = 2;
        public const int DefaultLemonDamage = 2;
        public const int DefaultCloverDamage = 3;
        public const int DefaultBellDamage = 4;
        public const int DefaultDiamondDamage = 5;
        public const int DefaultSevenDamage = 7;

        private static int _cherryDamage = DefaultCherryDamage;
        private static int _lemonDamage = DefaultLemonDamage;
        private static int _cloverDamage = DefaultCloverDamage;
        private static int _bellDamage = DefaultBellDamage;
        private static int _diamondDamage = DefaultDiamondDamage;
        private static int _sevenDamage = DefaultSevenDamage;

        public static void Configure(
            int cherry,
            int lemon,
            int clover,
            int bell,
            int diamond,
            int seven)
        {
            _cherryDamage = Math.Max(0, cherry);
            _lemonDamage = Math.Max(0, lemon);
            _cloverDamage = Math.Max(0, clover);
            _bellDamage = Math.Max(0, bell);
            _diamondDamage = Math.Max(0, diamond);
            _sevenDamage = Math.Max(0, seven);
        }

        public static void ResetToDefaults()
        {
            Configure(
                DefaultCherryDamage,
                DefaultLemonDamage,
                DefaultCloverDamage,
                DefaultBellDamage,
                DefaultDiamondDamage,
                DefaultSevenDamage);
        }

        public static int DamageFor(SlotSymbolType symbol) =>
            symbol switch
            {
                SlotSymbolType.Cherry => _cherryDamage,
                SlotSymbolType.Lemon => _lemonDamage,
                SlotSymbolType.Clover => _cloverDamage,
                SlotSymbolType.Bell => _bellDamage,
                SlotSymbolType.Diamond => _diamondDamage,
                SlotSymbolType.Seven => _sevenDamage,
                _ => 0,
            };
    }
}
