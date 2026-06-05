using SlotRogue.Slot.Data;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class SlotPatternPresentationResult
    {
        public SlotPatternPresentationResult(
            string patternName,
            SlotSymbolType symbol,
            int row,
            int startColumn,
            int matchLength,
            int[] highlightedCellIndices,
            string description,
            string bonusText,
            bool isFinale = false,
            int sfxLevel = 0,
            int bonusValue = 0)
        {
            PatternName = patternName ?? string.Empty;
            Symbol = symbol;
            Row = row;
            StartColumn = startColumn;
            MatchLength = matchLength;
            HighlightedCellIndices = highlightedCellIndices ?? new int[0];
            Description = description ?? string.Empty;
            BonusText = bonusText ?? string.Empty;
            IsFinale = isFinale;
            SfxLevel = sfxLevel;
            BonusValue = bonusValue;
        }

        public string PatternName { get; }

        public SlotSymbolType Symbol { get; }

        public int Row { get; }

        public int StartColumn { get; }

        public int MatchLength { get; }

        public int[] HighlightedCellIndices { get; }

        public string Description { get; }

        public string BonusText { get; }

        public bool IsFinale { get; }

        public int SfxLevel { get; }

        public int BonusValue { get; }
    }
}
