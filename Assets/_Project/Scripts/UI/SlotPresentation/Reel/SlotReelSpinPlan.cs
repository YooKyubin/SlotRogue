using System;
using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.SlotPresentation.Reel
{
    /// <summary>
    /// Pure, deterministic helper that decides the order in which symbols must scroll
    /// into a reel from the top during the stop phase so that, once the reel comes to
    /// rest, the visible window shows the requested final rows (top -> bottom).
    /// Kept free of UnityEngine so it can be covered by EditMode unit tests.
    /// </summary>
    public static class SlotReelSpinPlan
    {
        /// <summary>
        /// Builds the sequence of symbols that should enter the reel from the top, one per
        /// scroll step, during deceleration. Earlier entries are blur fillers; the trailing
        /// entries are the final rows arranged so the very last symbol to enter lands on the
        /// top row.
        /// </summary>
        /// <param name="finalRows">Target window symbols, ordered top -> bottom.</param>
        /// <param name="blurCount">How many filler symbols to scroll before the result enters.</param>
        /// <param name="filler">Supplies a blur symbol; called once per filler step.</param>
        public static IReadOnlyList<SlotSymbolType> BuildIncomingSymbols(
            IReadOnlyList<SlotSymbolType> finalRows,
            int blurCount,
            Func<SlotSymbolType> filler)
        {
            if (finalRows == null)
            {
                throw new ArgumentNullException(nameof(finalRows));
            }

            if (filler == null)
            {
                throw new ArgumentNullException(nameof(filler));
            }

            int safeBlur = blurCount < 0 ? 0 : blurCount;
            var incoming = new List<SlotSymbolType>(safeBlur + finalRows.Count);

            for (int step = 0; step < safeBlur; step++)
            {
                incoming.Add(filler());
            }

            incoming.AddRange(OrderResultEntries(finalRows));
            return incoming;
        }

        /// <summary>
        /// Returns the final rows in the order they must scroll into the top of the reel so that,
        /// once the reel rests, they occupy the window top -> bottom. The reel scrolls downward, so
        /// the last symbol to enter ends on the top row: feed bottom-first (row[N-1] .. row[0]).
        /// </summary>
        public static IReadOnlyList<SlotSymbolType> OrderResultEntries(IReadOnlyList<SlotSymbolType> finalRows)
        {
            if (finalRows == null)
            {
                throw new ArgumentNullException(nameof(finalRows));
            }

            var ordered = new List<SlotSymbolType>(finalRows.Count);
            for (int row = finalRows.Count - 1; row >= 0; row--)
            {
                ordered.Add(finalRows[row]);
            }

            return ordered;
        }

        /// <summary>
        /// Simulates a downward-scrolling window: each incoming symbol enters at the top and
        /// pushes the rest down, dropping the bottom-most. Returns the window (top -> bottom)
        /// after every incoming symbol has been consumed. Used to validate
        /// <see cref="BuildIncomingSymbols"/>.
        /// </summary>
        public static IReadOnlyList<SlotSymbolType> ProjectWindow(
            IReadOnlyList<SlotSymbolType> initialWindow,
            IReadOnlyList<SlotSymbolType> incoming,
            int visibleRows)
        {
            if (initialWindow == null)
            {
                throw new ArgumentNullException(nameof(initialWindow));
            }

            if (incoming == null)
            {
                throw new ArgumentNullException(nameof(incoming));
            }

            var window = new List<SlotSymbolType>(visibleRows);
            for (int row = 0; row < visibleRows; row++)
            {
                window.Add(row < initialWindow.Count ? initialWindow[row] : default);
            }

            for (int step = 0; step < incoming.Count; step++)
            {
                window.Insert(0, incoming[step]);
                window.RemoveAt(window.Count - 1);
            }

            return window;
        }
    }
}
