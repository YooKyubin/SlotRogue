using NUnit.Framework;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class RunBattleScreenViewModelTests
    {
        [Test]
        public void Batch_PublishesSingleSnapshot()
        {
            var viewModel = new RunBattleScreenViewModel(slotCellCount: 3, enemySlotCount: 1);
            int publishCount = 0;
            RunBattleScreenState lastState = null;
            viewModel.Changed += state =>
            {
                publishCount++;
                lastState = state;
            };

            viewModel.Batch(() =>
            {
                viewModel.SetSlotCells(new[] { "A", "B", "C" });
                viewModel.SetBattleText("status", "slot", "attack", "intent");
                viewModel.SetActionMode(RunBattleActionMode.Continue, spinInteractable: false);
            });

            Assert.That(publishCount, Is.EqualTo(1));
            Assert.That(lastState, Is.Not.Null);
            Assert.That(lastState.SlotCells, Is.EqualTo(new[] { "A", "B", "C" }));
            Assert.That(lastState.ActionMode, Is.EqualTo(RunBattleActionMode.Continue));
        }

        [Test]
        public void SetEnemySlot_StoresScreenOnlyState()
        {
            var viewModel = new RunBattleScreenViewModel(slotCellCount: 0, enemySlotCount: 2);

            viewModel.SetEnemySlot(
                slotIndex: 1,
                hudText: "Enemy\n8/10",
                hp: 8,
                maxHp: 10,
                selected: true,
                interactable: false);

            RunBattleEnemySlotState hidden = viewModel.State.EnemySlots[0];
            RunBattleEnemySlotState shown = viewModel.State.EnemySlots[1];

            Assert.That(hidden.Active, Is.False);
            Assert.That(shown.Active, Is.True);
            Assert.That(shown.HudText, Is.EqualTo("Enemy\n8/10"));
            Assert.That(shown.Hp, Is.EqualTo(8));
            Assert.That(shown.MaxHp, Is.EqualTo(10));
            Assert.That(shown.Selected, Is.True);
            Assert.That(shown.Interactable, Is.False);
        }

        [Test]
        public void State_ReturnsArrayCopies()
        {
            var viewModel = new RunBattleScreenViewModel(slotCellCount: 1, enemySlotCount: 0);
            viewModel.SetSlotCells(new[] { "A" });

            RunBattleScreenState state = viewModel.State;
            state.SlotCells[0] = "Mutated";

            Assert.That(viewModel.State.SlotCells[0], Is.EqualTo("A"));
        }
    }
}
