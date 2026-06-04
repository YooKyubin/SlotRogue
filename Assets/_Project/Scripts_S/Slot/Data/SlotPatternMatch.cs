using System.Collections.Generic;

namespace SlotRogue.Slot.Data
{
    public sealed class SlotPatternMatch
    {
        public SlotPatternMatch(
            SlotPatternDefinition definition,
            SlotSymbolType symbol,
            IReadOnlyList<SlotCell> matchedCells,
            int baseValue,
            int repeatIndex)
        {
            Definition = definition;
            Symbol = symbol;
            MatchedCells = matchedCells;
            BaseValue = baseValue;
            Multiplier = definition.Multiplier;
            CalculatedValue = (int)(baseValue * definition.Multiplier);
            RepeatIndex = repeatIndex;
            PresentationTitle = BuildTitle(definition, repeatIndex);
        }

        public SlotPatternDefinition Definition { get; }
        public SlotSymbolType Symbol { get; }
        public IReadOnlyList<SlotCell> MatchedCells { get; }
        public int BaseValue { get; }
        public float Multiplier { get; }
        public int CalculatedValue { get; }
        public int RepeatIndex { get; }
        public string PresentationTitle { get; }

        private static string BuildTitle(SlotPatternDefinition definition, int repeatIndex)
        {
            if (!definition.IsJackpot || repeatIndex == 0)
            {
                return definition.DisplayName;
            }

            switch (repeatIndex)
            {
                case 1:
                    return "슈퍼 잭팟";
                case 2:
                    return "메가 잭팟";
                case 3:
                    return "울트라 잭팟";
                case 4:
                    return "얼티밋 잭팟";
                default:
                    return $"잭팟 X{repeatIndex - 3}";
            }
        }
    }
}
