using System;

using NUnit.Framework;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;
using SlotRogue.Slot.ViewModels;

namespace SlotRogue.Slot.Tests
{
    public sealed class SlotMachineModelTests
    {
        [Test]
        public void Spin_PopulatesUnresolvedStateAndRaisesEvent()
        {
            var viewModel = new SlotMachineModel(
                new SlotMachineService(new Random(42)),
                new SlotPatternResolver(),
                new SlotResultCalculator(),
                new SlotCombatRequestBuilder());
            bool wasRaised = false;
            viewModel.StateChanged += _ => wasRaised = true;

            viewModel.Spin();

            Assert.That(wasRaised, Is.True);
            Assert.That(viewModel.CurrentSpinResult, Is.Not.Null);
            Assert.That(viewModel.CurrentPatternMatches, Is.Not.Null);
            Assert.That(viewModel.CurrentPatternMatches, Is.Empty);
            Assert.That(viewModel.CurrentPatternResult.HasMatch, Is.False);
            Assert.That(viewModel.CurrentCalculationResult.Damage, Is.Zero);
            Assert.That(viewModel.CurrentCombatRequest, Is.SameAs(SlotCombatRequest.Empty));
            Assert.That(viewModel.IsCurrentSpinResolved, Is.False);
            Assert.That(viewModel.CanSpin, Is.True);
        }

        [Test]
        public void ResolveCurrentSpinResult_PopulatesCombatRequestAfterSpin()
        {
            var viewModel = new SlotMachineModel(
                new SlotMachineService(new Random(42)),
                new SlotPatternResolver(),
                new SlotResultCalculator(),
                new SlotCombatRequestBuilder());
            int stateChangedCount = 0;
            viewModel.StateChanged += _ => stateChangedCount++;

            viewModel.Spin();
            viewModel.ResolveCurrentSpinResult();

            Assert.That(viewModel.IsCurrentSpinResolved, Is.True);
            Assert.That(viewModel.CurrentPatternMatches, Is.Not.Null);
            Assert.That(viewModel.CurrentPatternResult, Is.Not.Null);
            Assert.That(viewModel.CurrentCalculationResult, Is.Not.Null);
            Assert.That(viewModel.CurrentCombatRequest, Is.Not.Null);
            Assert.That(stateChangedCount, Is.EqualTo(2));
        }

        [Test]
        public void TrySwapAdjacentSymbols_UpdatesSpinResultAndRaisesEvent()
        {
            SlotSpinResult spin = new SlotSpinResult(new[]
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
            var viewModel = new SlotMachineModel(
                new SlotMachineService(
                    new Random(42),
                    null,
                    () => spin),
                new SlotPatternResolver(),
                new SlotResultCalculator(),
                new SlotCombatRequestBuilder());
            int stateChangedCount = 0;
            viewModel.StateChanged += _ => stateChangedCount++;

            viewModel.Spin();
            bool swapped = viewModel.TrySwapAdjacentSymbols(0, 1);

            Assert.That(swapped, Is.True);
            Assert.That(viewModel.CurrentSpinResult.Symbols[0], Is.EqualTo(SlotSymbolType.Lemon));
            Assert.That(viewModel.CurrentSpinResult.Symbols[1], Is.EqualTo(SlotSymbolType.Cherry));
            Assert.That(viewModel.CurrentCombatRequest, Is.SameAs(SlotCombatRequest.Empty));
            Assert.That(viewModel.IsCurrentSpinResolved, Is.False);
            Assert.That(stateChangedCount, Is.EqualTo(2));
            Assert.That(viewModel.TrySwapAdjacentSymbols(0, 2), Is.False);
        }

        [Test]
        public void TrySwapAdjacentSymbols_ClearsResolvedCombatRequest()
        {
            SlotSpinResult spin = new SlotSpinResult(new[]
            {
                SlotSymbolType.Cherry,
                SlotSymbolType.Cherry,
                SlotSymbolType.Cherry,
                SlotSymbolType.Bell,
                SlotSymbolType.Diamond,
                SlotSymbolType.Seven,
                SlotSymbolType.Lemon,
                SlotSymbolType.Clover,
                SlotSymbolType.Bell,
                SlotSymbolType.Diamond,
                SlotSymbolType.Seven,
                SlotSymbolType.Lemon,
                SlotSymbolType.Clover,
                SlotSymbolType.Bell,
                SlotSymbolType.Diamond,
            });
            var viewModel = new SlotMachineModel(
                new SlotMachineService(
                    new Random(42),
                    null,
                    () => spin),
                new SlotPatternResolver(),
                new SlotResultCalculator(),
                new SlotCombatRequestBuilder());

            viewModel.Spin();
            viewModel.ResolveCurrentSpinResult();
            Assert.That(viewModel.IsCurrentSpinResolved, Is.True);
            Assert.That(viewModel.CurrentCombatRequest, Is.Not.SameAs(SlotCombatRequest.Empty));

            bool swapped = viewModel.TrySwapAdjacentSymbols(0, 1);

            Assert.That(swapped, Is.True);
            Assert.That(viewModel.IsCurrentSpinResolved, Is.False);
            Assert.That(viewModel.CurrentPatternMatches, Is.Empty);
            Assert.That(viewModel.CurrentCombatRequest, Is.SameAs(SlotCombatRequest.Empty));
        }
    }
}
