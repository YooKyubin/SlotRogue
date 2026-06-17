namespace SlotRogue.Relics.Pool
{
    public static class RelicIconKeys
    {
        public const string SheetAddress = "relic/icons/base";

        public const string Slot00 = SheetAddress + "[RelicIcon-Sheet2_0]";
        public const string Slot01 = SheetAddress + "[RelicIcon-Sheet2_1]";
        public const string Slot02 = SheetAddress + "[RelicIcon-Sheet2_2]";
        public const string Slot03 = SheetAddress + "[RelicIcon-Sheet2_3]";
        public const string Slot04 = SheetAddress + "[RelicIcon-Sheet2_4]";
        public const string Slot05 = SheetAddress + "[RelicIcon-Sheet2_5]";
        public const string Slot06 = SheetAddress + "[RelicIcon-Sheet2_6]";
        public const string Slot07 = SheetAddress + "[RelicIcon-Sheet2_7]";
        public const string Slot08 = SheetAddress + "[RelicIcon-Sheet2_8]";
        public const string Slot09 = SheetAddress + "[RelicIcon-Sheet2_9]";
        public const string Slot10 = SheetAddress + "[RelicIcon-Sheet2_10]";
        public const string Slot11 = SheetAddress + "[RelicIcon-Sheet2_11]";
        public const string Slot12 = SheetAddress + "[RelicIcon-Sheet2_12]";
        public const string Slot13 = SheetAddress + "[RelicIcon-Sheet2_13]";
        public const string Slot14 = SheetAddress + "[RelicIcon-Sheet2_14]";
        public const string Slot15 = SheetAddress + "[RelicIcon-Sheet2_15]";

        public const string Default = Slot00;

        public static string DefaultFor(RelicRole role)
        {
            switch (role)
            {
                case RelicRole.Defense:
                    return Slot01;
                case RelicRole.Heal:
                    return Slot02;
                case RelicRole.Status:
                    return Slot03;
                case RelicRole.Utility:
                    return Slot06;
                default:
                    return Slot00;
            }
        }
    }
}
