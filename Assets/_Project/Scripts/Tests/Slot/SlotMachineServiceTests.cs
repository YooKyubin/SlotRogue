using System;

using NUnit.Framework;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;

namespace SlotRogue.Slot.Tests
{
    public sealed class SlotMachineServiceTests
    {
        [Test]
        public void Spin_CreatesFiveByThreeBoard()
        {
            var service = new SlotMachineService(new Random(1234));

            SlotSpinResult result = service.Spin();

            Assert.That(result.Symbols.Count, Is.EqualTo(SlotSpinResult.CellCount));
        }

        [Test]
        public void Spin_WithSameSeed_CreatesSameSymbols()
        {
            var firstService = new SlotMachineService(new Random(7));
            var secondService = new SlotMachineService(new Random(7));

            SlotSpinResult firstResult = firstService.Spin();
            SlotSpinResult secondResult = secondService.Spin();

            for (int index = 0; index < SlotSpinResult.CellCount; index++)
            {
                Assert.That(firstResult.Symbols[index], Is.EqualTo(secondResult.Symbols[index]));
            }
        }

        [Test]
        public void Spin_WithOverride_UsesProvidedResult()
        {
            var overrideResult = new SlotSpinResult(new[]
            {
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
                SlotSymbolType.Lemon,
            });
            var service = new SlotMachineService(
                new Random(7),
                null,
                () => overrideResult);

            SlotSpinResult result = service.Spin();

            Assert.That(result, Is.SameAs(overrideResult));
        }

        [Test]
        public void SwapAdjacent_ExchangesNeighborSymbols()
        {
            SlotSpinResult result = new SlotSpinResult(new[]
            {
                SlotSymbolType.Cherry,
                SlotSymbolType.Lemon,
                SlotSymbolType.Clover,
                SlotSymbolType.Bell,
                SlotSymbolType.Diamond,
                SlotSymbolType.Seven,
                SlotSymbolType.Cherry,
                SlotSymbolType.Lemon,
                SlotSymbolType.Clover,
                SlotSymbolType.Bell,
                SlotSymbolType.Diamond,
                SlotSymbolType.Seven,
                SlotSymbolType.Cherry,
                SlotSymbolType.Lemon,
                SlotSymbolType.Clover,
            });

            SlotSpinResult swapped = result.SwapAdjacent(0, 1);

            Assert.That(swapped.Symbols[0], Is.EqualTo(SlotSymbolType.Lemon));
            Assert.That(swapped.Symbols[1], Is.EqualTo(SlotSymbolType.Cherry));
            Assert.That(result.Symbols[0], Is.EqualTo(SlotSymbolType.Cherry));
            Assert.That(result.Symbols[1], Is.EqualTo(SlotSymbolType.Lemon));
            Assert.Throws<ArgumentException>(() => result.SwapAdjacent(0, 2));
        }
    }
}
