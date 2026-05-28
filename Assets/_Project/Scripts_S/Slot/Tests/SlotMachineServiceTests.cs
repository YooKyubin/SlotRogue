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
    }
}
