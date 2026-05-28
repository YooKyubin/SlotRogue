using System;
using System.Collections.Generic;
using System.Text;

namespace SlotRogue.Slot.Data
{
    public sealed class SlotSpinResult
    {
        public const int Columns = 5;
        public const int Rows = 3;
        public const int CellCount = Columns * Rows;

        public SlotSpinResult(IReadOnlyList<SlotSymbolType> symbols)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (symbols.Count != CellCount)
            {
                throw new ArgumentException($"Slot spin result requires {CellCount} symbols.", nameof(symbols));
            }

            _symbols = new SlotSymbolType[CellCount];

            for (int index = 0; index < CellCount; index++)
            {
                _symbols[index] = symbols[index];
            }
        }

        public IReadOnlyList<SlotSymbolType> Symbols => _symbols;

        public SlotSymbolType GetSymbol(int column, int row)
        {
            return _symbols[ToIndex(column, row)];
        }

        public string ToFlatString()
        {
            var builder = new StringBuilder();

            for (int index = 0; index < _symbols.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(_symbols[index]);
            }

            return builder.ToString();
        }

        public string ToBoardString()
        {
            var builder = new StringBuilder();

            for (int row = 0; row < Rows; row++)
            {
                if (row > 0)
                {
                    builder.AppendLine();
                }

                for (int column = 0; column < Columns; column++)
                {
                    if (column > 0)
                    {
                        builder.Append(" | ");
                    }

                    builder.Append(GetSymbol(column, row));
                }
            }

            return builder.ToString();
        }

        public static int ToIndex(int column, int row)
        {
            return (row * Columns) + column;
        }

        private readonly SlotSymbolType[] _symbols;
    }
}
