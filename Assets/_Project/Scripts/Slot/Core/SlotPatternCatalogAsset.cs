using System;
using System.Collections.Generic;
using SlotRogue.Slot.Data;
using UnityEngine;

namespace SlotRogue.Slot.Core
{
    [CreateAssetMenu(menuName = "SlotRogue/Slot Pattern Catalog", fileName = "SlotPatternCatalog")]
    public sealed class SlotPatternCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<SlotPatternCatalogEntry> _entries = new List<SlotPatternCatalogEntry>();

        public IReadOnlyList<SlotPatternCatalogEntry> Entries => _entries;

        public bool HasEntries => _entries != null && _entries.Count > 0;

        public List<PatternCandidate> GenerateCandidates(SlotSpinResult spin)
        {
            var candidates = new List<PatternCandidate>();

            if (spin == null || _entries == null)
            {
                return candidates;
            }

            for (int index = 0; index < _entries.Count; index++)
            {
                SlotPatternCatalogEntry entry = _entries[index];

                if (entry == null || !entry.CanEvaluate)
                {
                    continue;
                }

                switch (entry.MatchKind)
                {
                    case SlotPatternMatchKind.HorizontalRun:
                        AddHorizontalCandidates(spin, entry, candidates);
                        break;
                    case SlotPatternMatchKind.FixedCells:
                        TryAddFixedCandidate(spin, entry, candidates);
                        break;
                }
            }

            return candidates;
        }

        public SlotPatternDefinition PickForcedPattern(System.Random random, int luck)
        {
            if (luck <= 0 || random == null)
            {
                return null;
            }

            if (luck >= SlotSpinResult.CellCount)
            {
                return GetJackpotDefinition();
            }

            var pool = new List<SlotPatternDefinition>();

            for (int index = 0; index < _entries.Count; index++)
            {
                SlotPatternCatalogEntry entry = _entries[index];

                if (entry == null || !entry.CanForce)
                {
                    continue;
                }

                SlotPatternDefinition definition = entry.CreateDefinition(entry.BuildCells());

                if (definition.Cells.Count > 0)
                {
                    pool.Add(definition);
                }
            }

            int bestCellCount = 0;
            var candidates = new List<SlotPatternDefinition>();

            for (int index = 0; index < pool.Count; index++)
            {
                SlotPatternDefinition definition = pool[index];
                int cellCount = definition.Cells.Count;

                if (cellCount > luck)
                {
                    continue;
                }

                if (cellCount > bestCellCount)
                {
                    bestCellCount = cellCount;
                    candidates.Clear();
                    candidates.Add(definition);
                }
                else if (cellCount == bestCellCount)
                {
                    candidates.Add(definition);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[random.Next(candidates.Count)];
        }

        public SlotPatternDefinition GetJackpotDefinition()
        {
            if (_entries == null)
            {
                return null;
            }

            for (int index = 0; index < _entries.Count; index++)
            {
                SlotPatternCatalogEntry entry = _entries[index];

                if (entry != null && entry.Enabled && entry.IsJackpot)
                {
                    return entry.CreateDefinition(entry.BuildCells());
                }
            }

            return null;
        }

        public void ResetToDefaults()
        {
            _entries = CreateDefaultEntries();
        }

        public void SortByOrderIndex()
        {
            if (_entries == null)
            {
                return;
            }

            _entries.Sort((left, right) =>
            {
                if (left == null && right == null)
                {
                    return 0;
                }

                if (left == null)
                {
                    return 1;
                }

                if (right == null)
                {
                    return -1;
                }

                int orderCompare = left.OrderIndex.CompareTo(right.OrderIndex);
                return orderCompare != 0
                    ? orderCompare
                    : string.Compare(left.PatternId, right.PatternId, StringComparison.Ordinal);
            });
        }

        public List<string> ValidateEntries()
        {
            var messages = new List<string>();

            if (_entries == null || _entries.Count == 0)
            {
                messages.Add("Catalog has no pattern entries.");
                return messages;
            }

            var ids = new HashSet<string>();
            bool hasJackpot = false;

            for (int index = 0; index < _entries.Count; index++)
            {
                SlotPatternCatalogEntry entry = _entries[index];

                if (entry == null)
                {
                    messages.Add($"Entry {index} is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.PatternId))
                {
                    messages.Add($"Entry {index} has an empty pattern id.");
                }
                else if (!ids.Add(entry.PatternId))
                {
                    messages.Add($"Pattern id '{entry.PatternId}' is duplicated.");
                }

                if (entry.MatchKind == SlotPatternMatchKind.HorizontalRun && entry.HorizontalLength <= 0)
                {
                    messages.Add($"Pattern '{entry.PatternId}' has an invalid horizontal length.");
                }

                if (entry.MatchKind == SlotPatternMatchKind.FixedCells && entry.CellCount == 0)
                {
                    messages.Add($"Pattern '{entry.PatternId}' has no cells.");
                }

                if (entry.HasInvalidCell)
                {
                    messages.Add($"Pattern '{entry.PatternId}' has a cell outside the slot board.");
                }

                hasJackpot |= entry.Enabled && entry.IsJackpot;
            }

            if (!hasJackpot)
            {
                messages.Add("Catalog has no enabled jackpot pattern.");
            }

            return messages;
        }

        public static SlotPatternCatalogAsset CreateDefaultCatalog()
        {
            var catalog = CreateInstance<SlotPatternCatalogAsset>();
            catalog.ResetToDefaults();
            return catalog;
        }

        private void OnValidate()
        {
            if (_entries == null)
            {
                _entries = new List<SlotPatternCatalogEntry>();
            }

            for (int index = 0; index < _entries.Count; index++)
            {
                _entries[index]?.ClampSerializedValues();
            }
        }

        private static void AddHorizontalCandidates(
            SlotSpinResult spin,
            SlotPatternCatalogEntry entry,
            List<PatternCandidate> candidates)
        {
            int length = entry.HorizontalLength;

            for (int row = 0; row < SlotSpinResult.Rows; row++)
            {
                int column = 0;

                while (column < SlotSpinResult.Columns)
                {
                    SlotSymbolType symbol = spin.GetSymbol(column, row);
                    int runLength = 1;

                    while (column + runLength < SlotSpinResult.Columns &&
                           spin.GetSymbol(column + runLength, row) == symbol)
                    {
                        runLength++;
                    }

                    if (runLength >= length)
                    {
                        for (int startColumn = column; startColumn <= column + runLength - length; startColumn++)
                        {
                            AddHorizontalCandidate(entry, symbol, startColumn, row, length, candidates);
                        }
                    }

                    column += runLength;
                }
            }
        }

        private static void AddHorizontalCandidate(
            SlotPatternCatalogEntry entry,
            SlotSymbolType symbol,
            int startColumn,
            int row,
            int length,
            List<PatternCandidate> candidates)
        {
            var cells = new List<SlotCell>(length);

            for (int column = startColumn; column < startColumn + length; column++)
            {
                cells.Add(new SlotCell(column, row));
            }

            SlotPatternDefinition definition = entry.CreateDefinition(Array.Empty<SlotCell>());
            candidates.Add(new PatternCandidate(definition, symbol, cells));
        }

        private static void TryAddFixedCandidate(
            SlotSpinResult spin,
            SlotPatternCatalogEntry entry,
            List<PatternCandidate> candidates)
        {
            List<SlotCell> cells = entry.BuildCells();

            if (cells.Count == 0)
            {
                return;
            }

            SlotCell firstCell = cells[0];
            SlotSymbolType symbol = spin.GetSymbol(firstCell.Col, firstCell.Row);

            for (int index = 0; index < cells.Count; index++)
            {
                SlotCell cell = cells[index];

                if (!IsCellInBounds(cell) || spin.GetSymbol(cell.Col, cell.Row) != symbol)
                {
                    return;
                }
            }

            SlotPatternDefinition definition = entry.CreateDefinition(cells);
            candidates.Add(new PatternCandidate(definition, symbol, cells));
        }

        private static bool IsCellInBounds(SlotCell cell)
        {
            return cell.Col >= 0 &&
                cell.Col < SlotSpinResult.Columns &&
                cell.Row >= 0 &&
                cell.Row < SlotSpinResult.Rows;
        }

        private static List<SlotPatternCatalogEntry> CreateDefaultEntries()
        {
            var entries = new List<SlotPatternCatalogEntry>
            {
                SlotPatternCatalogEntry.Horizontal(
                    "horizontal-sm",
                    "가로 3칸",
                    0,
                    1.0f,
                    SlotPatternRank.HorizontalSm,
                    3),
                SlotPatternCatalogEntry.Horizontal(
                    "horizontal-lg",
                    "가로 4칸",
                    3,
                    2.0f,
                    SlotPatternRank.HorizontalLg,
                    4),
                SlotPatternCatalogEntry.Horizontal(
                    "horizontal-xl",
                    "가로 5칸",
                    4,
                    3.0f,
                    SlotPatternRank.HorizontalXL,
                    5)
            };

            AddVerticalEntries(entries);
            AddDiagonalEntries(entries);
            entries.Add(SlotPatternCatalogEntry.Fixed(
                "zig",
                "지그재그",
                5,
                4.0f,
                SlotPatternRank.Zig,
                false,
                true,
                true,
                new[]
                {
                    new SlotCell(2, 0),
                    new SlotCell(1, 1),
                    new SlotCell(3, 1),
                    new SlotCell(0, 2),
                    new SlotCell(4, 2)
                }));
            entries.Add(SlotPatternCatalogEntry.Fixed(
                "zag",
                "역지그재그",
                6,
                4.0f,
                SlotPatternRank.Zag,
                false,
                true,
                true,
                new[]
                {
                    new SlotCell(0, 0),
                    new SlotCell(4, 0),
                    new SlotCell(1, 1),
                    new SlotCell(3, 1),
                    new SlotCell(2, 2)
                }));
            entries.Add(SlotPatternCatalogEntry.Fixed(
                "ground",
                "대지",
                7,
                7.0f,
                SlotPatternRank.Ground,
                false,
                true,
                true,
                new[]
                {
                    new SlotCell(2, 0),
                    new SlotCell(1, 1),
                    new SlotCell(3, 1),
                    new SlotCell(0, 2),
                    new SlotCell(1, 2),
                    new SlotCell(2, 2),
                    new SlotCell(3, 2),
                    new SlotCell(4, 2)
                }));
            entries.Add(SlotPatternCatalogEntry.Fixed(
                "heaven",
                "천상",
                8,
                7.0f,
                SlotPatternRank.Heaven,
                false,
                true,
                true,
                new[]
                {
                    new SlotCell(0, 0),
                    new SlotCell(1, 0),
                    new SlotCell(2, 0),
                    new SlotCell(3, 0),
                    new SlotCell(4, 0),
                    new SlotCell(1, 1),
                    new SlotCell(3, 1),
                    new SlotCell(2, 2)
                }));
            entries.Add(SlotPatternCatalogEntry.Fixed(
                "eye",
                "눈",
                9,
                8.0f,
                SlotPatternRank.Eye,
                false,
                true,
                true,
                new[]
                {
                    new SlotCell(1, 0),
                    new SlotCell(2, 0),
                    new SlotCell(3, 0),
                    new SlotCell(0, 1),
                    new SlotCell(1, 1),
                    new SlotCell(3, 1),
                    new SlotCell(4, 1),
                    new SlotCell(1, 2),
                    new SlotCell(2, 2),
                    new SlotCell(3, 2)
                }));
            entries.Add(SlotPatternCatalogEntry.Fixed(
                "jackpot",
                "잭팟",
                10,
                10.0f,
                SlotPatternRank.Jackpot,
                true,
                true,
                false,
                BuildAllCells()));
            entries.Add(SlotPatternCatalogEntry.Fixed(
                "forced-horizontal-xl-row1",
                "가로 5칸",
                4,
                3.0f,
                SlotPatternRank.HorizontalXL,
                false,
                false,
                true,
                new[]
                {
                    new SlotCell(0, 1),
                    new SlotCell(1, 1),
                    new SlotCell(2, 1),
                    new SlotCell(3, 1),
                    new SlotCell(4, 1)
                }));

            return entries;
        }

        private static void AddVerticalEntries(List<SlotPatternCatalogEntry> entries)
        {
            for (int column = 0; column < SlotSpinResult.Columns; column++)
            {
                entries.Add(SlotPatternCatalogEntry.Fixed(
                    $"vertical-col{column}",
                    $"{column + 1}열 세로",
                    1,
                    1.0f,
                    SlotPatternRank.Vertical,
                    false,
                    true,
                    true,
                    new[]
                    {
                        new SlotCell(column, 0),
                        new SlotCell(column, 1),
                        new SlotCell(column, 2)
                    }));
            }
        }

        private static void AddDiagonalEntries(List<SlotPatternCatalogEntry> entries)
        {
            entries.Add(SlotPatternCatalogEntry.Fixed("diag-bs0", "역대각 1", 2, 1.0f, SlotPatternRank.Diagonal, false, true, true, new[] { new SlotCell(0, 0), new SlotCell(1, 1), new SlotCell(2, 2) }));
            entries.Add(SlotPatternCatalogEntry.Fixed("diag-bs1", "역대각 2", 2, 1.0f, SlotPatternRank.Diagonal, false, true, true, new[] { new SlotCell(1, 0), new SlotCell(2, 1), new SlotCell(3, 2) }));
            entries.Add(SlotPatternCatalogEntry.Fixed("diag-bs2", "역대각 3", 2, 1.0f, SlotPatternRank.Diagonal, false, true, true, new[] { new SlotCell(2, 0), new SlotCell(3, 1), new SlotCell(4, 2) }));
            entries.Add(SlotPatternCatalogEntry.Fixed("diag-s0", "정대각 1", 2, 1.0f, SlotPatternRank.Diagonal, false, true, true, new[] { new SlotCell(2, 0), new SlotCell(1, 1), new SlotCell(0, 2) }));
            entries.Add(SlotPatternCatalogEntry.Fixed("diag-s1", "정대각 2", 2, 1.0f, SlotPatternRank.Diagonal, false, true, true, new[] { new SlotCell(3, 0), new SlotCell(2, 1), new SlotCell(1, 2) }));
            entries.Add(SlotPatternCatalogEntry.Fixed("diag-s2", "정대각 3", 2, 1.0f, SlotPatternRank.Diagonal, false, true, true, new[] { new SlotCell(4, 0), new SlotCell(3, 1), new SlotCell(2, 2) }));
        }

        private static SlotCell[] BuildAllCells()
        {
            var cells = new SlotCell[SlotSpinResult.CellCount];
            int index = 0;

            for (int row = 0; row < SlotSpinResult.Rows; row++)
            {
                for (int column = 0; column < SlotSpinResult.Columns; column++)
                {
                    cells[index] = new SlotCell(column, row);
                    index++;
                }
            }

            return cells;
        }
    }

    public enum SlotPatternMatchKind
    {
        FixedCells,
        HorizontalRun
    }

    [Serializable]
    public sealed class SlotPatternCatalogEntry
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private bool _includeInSpinEvaluation = true;
        [SerializeField] private bool _includeInForcedPatternPool;
        [SerializeField] private SlotPatternMatchKind _matchKind = SlotPatternMatchKind.FixedCells;
        [SerializeField] private string _patternId = "pattern-id";
        [SerializeField] private string _displayName = "Pattern";
        [SerializeField] private int _orderIndex;
        [SerializeField] private float _multiplier = 1.0f;
        [SerializeField] private SlotPatternRank _rank;
        [SerializeField] private bool _isJackpot;
        [SerializeField] private int _horizontalLength = 3;
        [SerializeField] private List<SlotPatternCell> _cells = new List<SlotPatternCell>();

        public bool Enabled => _enabled;
        public bool CanEvaluate => _enabled && _includeInSpinEvaluation;
        public bool CanForce => _enabled && _includeInForcedPatternPool;
        public SlotPatternMatchKind MatchKind => _matchKind;
        public string PatternId => _patternId;
        public int OrderIndex => _orderIndex;
        public bool IsJackpot => _isJackpot;
        public int HorizontalLength => Clamp(_horizontalLength, 1, SlotSpinResult.Columns);
        public int CellCount => _cells != null ? _cells.Count : 0;

        public bool HasInvalidCell
        {
            get
            {
                if (_cells == null)
                {
                    return false;
                }

                for (int index = 0; index < _cells.Count; index++)
                {
                    if (_cells[index] == null || !_cells[index].IsInBounds)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public static SlotPatternCatalogEntry Horizontal(
            string patternId,
            string displayName,
            int orderIndex,
            float multiplier,
            SlotPatternRank rank,
            int horizontalLength)
        {
            return new SlotPatternCatalogEntry
            {
                _enabled = true,
                _includeInSpinEvaluation = true,
                _includeInForcedPatternPool = false,
                _matchKind = SlotPatternMatchKind.HorizontalRun,
                _patternId = patternId,
                _displayName = displayName,
                _orderIndex = orderIndex,
                _multiplier = multiplier,
                _rank = rank,
                _isJackpot = false,
                _horizontalLength = horizontalLength,
                _cells = new List<SlotPatternCell>()
            };
        }

        public static SlotPatternCatalogEntry Fixed(
            string patternId,
            string displayName,
            int orderIndex,
            float multiplier,
            SlotPatternRank rank,
            bool isJackpot,
            bool includeInSpinEvaluation,
            bool includeInForcedPatternPool,
            IReadOnlyList<SlotCell> cells)
        {
            var entry = new SlotPatternCatalogEntry
            {
                _enabled = true,
                _includeInSpinEvaluation = includeInSpinEvaluation,
                _includeInForcedPatternPool = includeInForcedPatternPool,
                _matchKind = SlotPatternMatchKind.FixedCells,
                _patternId = patternId,
                _displayName = displayName,
                _orderIndex = orderIndex,
                _multiplier = multiplier,
                _rank = rank,
                _isJackpot = isJackpot,
                _horizontalLength = 3,
                _cells = new List<SlotPatternCell>()
            };

            if (cells != null)
            {
                for (int index = 0; index < cells.Count; index++)
                {
                    entry._cells.Add(SlotPatternCell.FromSlotCell(cells[index]));
                }
            }

            return entry;
        }

        public SlotPatternDefinition CreateDefinition(IReadOnlyList<SlotCell> cells)
        {
            return new SlotPatternDefinition(
                _patternId,
                _displayName,
                _orderIndex,
                _multiplier,
                _rank,
                _isJackpot,
                cells ?? Array.Empty<SlotCell>());
        }

        public List<SlotCell> BuildCells()
        {
            var cells = new List<SlotCell>();

            if (_cells == null)
            {
                return cells;
            }

            for (int index = 0; index < _cells.Count; index++)
            {
                if (_cells[index] != null && _cells[index].IsInBounds)
                {
                    cells.Add(_cells[index].ToSlotCell());
                }
            }

            return cells;
        }

        public void ClampSerializedValues()
        {
            _horizontalLength = Clamp(_horizontalLength, 1, SlotSpinResult.Columns);

            if (_cells == null)
            {
                _cells = new List<SlotPatternCell>();
            }

            for (int index = 0; index < _cells.Count; index++)
            {
                _cells[index]?.ClampSerializedValues();
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }

    [Serializable]
    public sealed class SlotPatternCell
    {
        [SerializeField] private int _col;
        [SerializeField] private int _row;

        public bool IsInBounds =>
            _col >= 0 &&
            _col < SlotSpinResult.Columns &&
            _row >= 0 &&
            _row < SlotSpinResult.Rows;

        public static SlotPatternCell FromSlotCell(SlotCell cell)
        {
            return new SlotPatternCell
            {
                _col = cell.Col,
                _row = cell.Row
            };
        }

        public SlotCell ToSlotCell()
        {
            return new SlotCell(_col, _row);
        }

        public void ClampSerializedValues()
        {
            _col = Clamp(_col, 0, SlotSpinResult.Columns - 1);
            _row = Clamp(_row, 0, SlotSpinResult.Rows - 1);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
