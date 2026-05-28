using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Core
{
    public sealed class SlotPatternResolver
    {
        private const int MinimumMatchLength = 3;

        public SlotPatternResult Resolve(SlotSpinResult spinResult)
        {
            if (spinResult == null)
            {
                return SlotPatternResult.NoMatch;
            }

            SlotPatternResult bestResult = SlotPatternResult.NoMatch;

            for (int row = 0; row < SlotSpinResult.Rows; row++)
            {
                int startColumn = 0;

                while (startColumn < SlotSpinResult.Columns)
                {
                    SlotSymbolType symbol = spinResult.GetSymbol(startColumn, row);
                    int matchLength = CountRunLength(spinResult, symbol, startColumn, row);

                    if (matchLength >= MinimumMatchLength)
                    {
                        SlotPatternResult candidate = CreateResult(symbol, row, startColumn, matchLength);

                        if (candidate.Score > bestResult.Score)
                        {
                            bestResult = candidate;
                        }
                    }

                    startColumn += matchLength;
                }
            }

            return bestResult;
        }

        private static int CountRunLength(SlotSpinResult spinResult, SlotSymbolType symbol, int startColumn, int row)
        {
            int matchLength = 0;

            for (int column = startColumn; column < SlotSpinResult.Columns; column++)
            {
                if (spinResult.GetSymbol(column, row) != symbol)
                {
                    break;
                }

                matchLength++;
            }

            return matchLength;
        }

        private static SlotPatternResult CreateResult(
            SlotSymbolType symbol,
            int row,
            int startColumn,
            int matchLength)
        {
            int score = GetSymbolScore(symbol) * matchLength;
            string patternName = $"{symbol} Line x{matchLength}";

            return new SlotPatternResult(true, patternName, symbol, row, startColumn, matchLength, score);
        }

        private static int GetSymbolScore(SlotSymbolType symbol)
        {
            switch (symbol)
            {
                case SlotSymbolType.Sword:
                    return 6;
                case SlotSymbolType.Shield:
                    return 5;
                case SlotSymbolType.Heart:
                    return 4;
                case SlotSymbolType.Coin:
                    return 3;
                case SlotSymbolType.Gem:
                    return 8;
                case SlotSymbolType.Skull:
                    return 7;
                default:
                    return 1;
            }
        }
    }
}
