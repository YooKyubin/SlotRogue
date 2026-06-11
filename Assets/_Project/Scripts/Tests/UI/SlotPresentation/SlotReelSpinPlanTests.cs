using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Slot.Data;
using SlotRogue.UI.SlotPresentation.Reel;

namespace SlotRogue.UI.Tests.SlotPresentation
{
    public sealed class SlotReelSpinPlanTests
    {
        [Test]
        public void BuildIncomingSymbols_EndsWithFinalRowsBottomFirst()
        {
            var finalRows = new[] { SlotSymbolType.Cherry, SlotSymbolType.Seven, SlotSymbolType.Bell };

            IReadOnlyList<SlotSymbolType> incoming = SlotReelSpinPlan.BuildIncomingSymbols(
                finalRows,
                blurCount: 4,
                filler: () => SlotSymbolType.Lemon);

            Assert.AreEqual(7, incoming.Count);
            // Last three entered land on the window, top row entering last.
            Assert.AreEqual(SlotSymbolType.Bell, incoming[4]);
            Assert.AreEqual(SlotSymbolType.Seven, incoming[5]);
            Assert.AreEqual(SlotSymbolType.Cherry, incoming[6]);
        }

        [Test]
        public void ProjectWindow_AfterIncoming_ShowsFinalRowsTopToBottom()
        {
            var finalRows = new[] { SlotSymbolType.Diamond, SlotSymbolType.Clover, SlotSymbolType.Lemon };
            var initialWindow = new[] { SlotSymbolType.Cherry, SlotSymbolType.Cherry, SlotSymbolType.Cherry };

            IReadOnlyList<SlotSymbolType> incoming = SlotReelSpinPlan.BuildIncomingSymbols(
                finalRows,
                blurCount: 6,
                filler: () => SlotSymbolType.Seven);

            IReadOnlyList<SlotSymbolType> window = SlotReelSpinPlan.ProjectWindow(initialWindow, incoming, SlotSpinResult.Rows);

            CollectionAssert.AreEqual(finalRows, window);
        }

        [Test]
        public void BuildIncomingSymbols_ZeroBlur_ProducesOnlyFinalRows()
        {
            var finalRows = new[] { SlotSymbolType.Bell, SlotSymbolType.Bell, SlotSymbolType.Seven };

            IReadOnlyList<SlotSymbolType> incoming = SlotReelSpinPlan.BuildIncomingSymbols(
                finalRows,
                blurCount: 0,
                filler: () => SlotSymbolType.Cherry);

            Assert.AreEqual(3, incoming.Count);

            IReadOnlyList<SlotSymbolType> window = SlotReelSpinPlan.ProjectWindow(
                new[] { SlotSymbolType.Lemon, SlotSymbolType.Lemon, SlotSymbolType.Lemon },
                incoming,
                SlotSpinResult.Rows);

            CollectionAssert.AreEqual(finalRows, window);
        }
    }
}
