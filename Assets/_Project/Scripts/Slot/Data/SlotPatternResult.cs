namespace SlotRogue.Slot.Data
{
    public sealed class SlotPatternResult
    {
        public static readonly SlotPatternResult NoMatch = new(
            false,
            "매치 없음",
            SlotSymbolType.Cherry,
            -1,
            -1,
            0,
            0);

        public SlotPatternResult(
            bool hasMatch,
            string patternName,
            SlotSymbolType symbol,
            int row,
            int startColumn,
            int matchLength,
            int score)
        {
            HasMatch = hasMatch;
            PatternName = patternName;
            Symbol = symbol;
            Row = row;
            StartColumn = startColumn;
            MatchLength = matchLength;
            Score = score;
        }

        public bool HasMatch { get; }

        public string PatternName { get; }

        public SlotSymbolType Symbol { get; }

        public int Row { get; }

        public int StartColumn { get; }

        public int MatchLength { get; }

        public int Score { get; }
    }
}
