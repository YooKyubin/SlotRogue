using System;

using NUnit.Framework;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;
using SlotRogue.Slot.ViewModels;

namespace SlotRogue.Slot.Tests
{
    public sealed class SlotMachineViewModelTests
    {
        [Test]
        public void Spin_PopulatesStateAndRaisesEvent()
        {
            var viewModel = new SlotMachineViewModel(
                new SlotMachineService(new Random(42)),
                new SlotPatternResolver(),
                new SlotResultCalculator(),
                new SlotCombatRequestBuilder());
            bool wasRaised = false;
            viewModel.StateChanged += () => wasRaised = true;

            viewModel.Spin();

            Assert.That(wasRaised, Is.True);
            Assert.That(viewModel.CurrentSpinResult, Is.Not.Null);
            Assert.That(viewModel.CurrentPatternResult, Is.Not.Null);
            Assert.That(viewModel.CurrentCalculationResult, Is.Not.Null);
            Assert.That(viewModel.CurrentCombatRequest, Is.Not.Null);
            Assert.That(viewModel.CanSpin, Is.True);
        }
    }
}
