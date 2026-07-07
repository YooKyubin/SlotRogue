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

        public SlotSpinResult SwapAdjacent(int firstIndex, int secondIndex)
        {
            if (!AreAdjacent(firstIndex, secondIndex))
            {
                throw new ArgumentException("Slot symbols can only be swapped with an adjacent cell.");
            }

            var symbols = new SlotSymbolType[CellCount];
            Array.Copy(_symbols, symbols, CellCount);
            (symbols[firstIndex], symbols[secondIndex]) =
                (symbols[secondIndex], symbols[firstIndex]);
            return new SlotSpinResult(symbols);
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

        public static int ToColumn(int index)
        {
            return index % Columns;
        }

        public static int ToRow(int index)
        {
            return index / Columns;
        }

        public static bool IsValidIndex(int index)
        {
            return index >= 0 && index < CellCount;
        }

        public static bool AreAdjacent(int firstIndex, int secondIndex)
        {
            if (!IsValidIndex(firstIndex) ||
                !IsValidIndex(secondIndex) ||
                firstIndex == secondIndex)
            {
                return false;
            }

            int columnDelta = Math.Abs(ToColumn(firstIndex) - ToColumn(secondIndex));
            int rowDelta = Math.Abs(ToRow(firstIndex) - ToRow(secondIndex));
            return columnDelta + rowDelta == 1;
        }

        private readonly SlotSymbolType[] _symbols;
    }
}
