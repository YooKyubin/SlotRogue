using System.Collections.Generic;
using SlotRogue.Slot.Data;
using UnityEngine;
using Random = System.Random;

namespace SlotRogue.Slot.Core
{
    public static class SlotPatternCatalog
    {
        private const string ResourceCatalogPath = "SlotPatternCatalog";

        public static List<PatternCandidate> GenerateCandidates(SlotSpinResult spin)
        {
            SlotPatternCatalogAsset asset = GetCatalogOrLogError();
            return asset != null ? asset.GenerateCandidates(spin) : new List<PatternCandidate>();
        }

        public static SlotPatternDefinition PickForcedPattern(Random random, int luck)
        {
            SlotPatternCatalogAsset asset = GetCatalogOrLogError();
            return asset != null ? asset.PickForcedPattern(random, luck) : null;
        }

        public static void SetRuntimeCatalogOverride(SlotPatternCatalogAsset catalog)
        {
            _runtimeCatalogOverride = catalog;
        }

        public static void ClearRuntimeCatalogOverride()
        {
            _runtimeCatalogOverride = null;
        }

        private static SlotPatternCatalogAsset GetCatalogOrLogError()
        {
            SlotPatternCatalogAsset asset = LoadCatalogAsset();

            if (asset != null && asset.HasEntries)
            {
                return asset;
            }

            Debug.LogError("[SlotRogue] SlotPatternCatalog asset not found or empty. " +
                "Place SlotPatternCatalog.asset in Assets/Resources/.");
            return null;
        }

        private static SlotPatternCatalogAsset LoadCatalogAsset()
        {
            if (_runtimeCatalogOverride != null)
            {
                return _runtimeCatalogOverride;
            }

            return Resources.Load<SlotPatternCatalogAsset>(ResourceCatalogPath);
        }

        private static SlotPatternCatalogAsset _runtimeCatalogOverride;
    }

    public sealed class PatternCandidate
    {
        public PatternCandidate(
            SlotPatternDefinition definition,
            SlotSymbolType symbol,
            List<SlotCell> cells)
        {
            Definition = definition;
            Symbol = symbol;
            Cells = cells;
        }

        public SlotPatternDefinition Definition { get; }
        public SlotSymbolType Symbol { get; }
        public List<SlotCell> Cells { get; }
        public bool Suppressed { get; set; }

        public int SortKey => Definition.OrderIndex * 10000 + MinCellSortKey;

        private int MinCellSortKey
        {
            get
            {
                int minimum = int.MaxValue;

                foreach (SlotCell cell in Cells)
                {
                    if (cell.SortKey < minimum)
                    {
                        minimum = cell.SortKey;
                    }
                }

                return minimum;
            }
        }
    }
}
