using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.UI.App;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.RunGame.ViewModels;

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
                viewModel.SetActionMode(RunBattleActionMode.Spin, spinInteractable: false);
            });

            Assert.That(publishCount, Is.EqualTo(1));
            Assert.That(lastState, Is.Not.Null);
            Assert.That(lastState.SlotCells, Is.EqualTo(new[] { "A", "B", "C" }));
            Assert.That(lastState.ActionMode, Is.EqualTo(RunBattleActionMode.Spin));
            Assert.That(lastState.SpinInteractable, Is.False);
        }

        [Test]
        public void SetEnemySlot_StoresScreenOnlyState()
        {
            var viewModel = new RunBattleScreenViewModel(slotCellCount: 0, enemySlotCount: 2);

            viewModel.SetEnemySlot(
                slotIndex: 1,
                participantId: new CombatParticipantId(101),
                hudText: "Enemy\n8/10",
                hp: 8,
                maxHp: 10,
                shield: 3,
                selected: true,
                interactable: false,
                statuses: new[]
                {
                    new StatusEffectViewData(StatusEffectKind.Burn, remainingTurns: 2, magnitude: 3, stackCount: 1),
                });

            RunBattleEnemySlotState hidden = viewModel.State.EnemySlots[0];
            RunBattleEnemySlotState shown = viewModel.State.EnemySlots[1];

            Assert.That(hidden.Active, Is.False);
            Assert.That(shown.Active, Is.True);
            Assert.That(shown.ParticipantId.Value, Is.EqualTo(101));
            Assert.That(shown.HudText, Is.EqualTo("Enemy\n8/10"));
            Assert.That(shown.Hp, Is.EqualTo(8));
            Assert.That(shown.MaxHp, Is.EqualTo(10));
            Assert.That(shown.Shield, Is.EqualTo(3));
            Assert.That(shown.Selected, Is.True);
            Assert.That(shown.Interactable, Is.False);
            Assert.That(shown.Statuses, Has.Length.EqualTo(1));
            Assert.That(shown.Statuses[0].Kind, Is.EqualTo(StatusEffectKind.Burn));
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

        [Test]
        public void EnemySlotState_ReturnsStatusCopies()
        {
            var viewModel = new RunBattleScreenViewModel(slotCellCount: 0, enemySlotCount: 1);
            viewModel.SetEnemySlot(
                slotIndex: 0,
                participantId: new CombatParticipantId(102),
                hudText: "Enemy",
                hp: 10,
                maxHp: 10,
                shield: 0,
                selected: false,
                interactable: true,
                statuses: new[]
                {
                    new StatusEffectViewData(StatusEffectKind.Poison, remainingTurns: 0, magnitude: 1, stackCount: 2),
                });

            StatusEffectViewData[] statuses = viewModel.State.EnemySlots[0].Statuses;
            statuses[0] = new StatusEffectViewData(StatusEffectKind.Freeze, remainingTurns: 1, magnitude: 0, stackCount: 1);

            Assert.That(viewModel.State.EnemySlots[0].Statuses[0].Kind, Is.EqualTo(StatusEffectKind.Poison));
        }

        [Test]
        public void StartRelicRefresh_PublishesDisplayOnlyOptions()
        {
            GameFlowSession.StartNewRun();
            var viewModel = new StartRelicSelectViewModel(new System.Random(1234));
            StartRelicSelectViewState publishedState = null;
            viewModel.Changed += state => publishedState = state;

            viewModel.Refresh();

            Assert.That(publishedState, Is.SameAs(viewModel.State));
            Assert.That(publishedState.Options, Has.Count.EqualTo(3));
            Assert.That(publishedState.Options[0].Id, Is.Not.Empty);
            Assert.That(publishedState.Options[0].Title, Is.Not.Empty);
            Assert.That(publishedState.Options[0].Description, Is.Not.Empty);
        }

        [Test]
        public void RewardRefresh_PublishesSingleScreenSnapshot()
        {
            GameFlowSession.StartNewRun();
            var viewModel = new RunRewardViewModel();
            int publishCount = 0;
            viewModel.Changed += _ => publishCount++;

            viewModel.Refresh();

            Assert.That(publishCount, Is.EqualTo(1));
            Assert.That(viewModel.State.Options, Is.Not.Empty);
        }

        [Test]
        public void HudRefresh_PublishesSessionSnapshot()
        {
            GameFlowSession.StartNewRun();
            var viewModel = new RunHUDViewModel();
            RunHUDViewState publishedState = default;
            viewModel.Changed += state => publishedState = state;

            viewModel.Refresh();

            Assert.That(publishedState.CurrentHp, Is.EqualTo(GameFlowSession.PlayerCurrentHp));
            Assert.That(publishedState.MaxHp, Is.EqualTo(GameFlowSession.PlayerMaxHp));
            Assert.That(publishedState.BattleIndex, Is.EqualTo(GameFlowSession.CurrentBattleNumber));
        }

        [Test]
        public void GameStartCommands_PublishIntentWithoutUnityDependency()
        {
            var viewModel = new GameStartViewModel();
            bool startRequested = false;
            bool quitRequested = false;
            viewModel.StartGameRequested += () => startRequested = true;
            viewModel.QuitGameRequested += () => quitRequested = true;

            viewModel.RequestStartGame();
            viewModel.RequestQuitGame();

            Assert.That(startRequested, Is.True);
            Assert.That(quitRequested, Is.True);
        }

        [Test]
        public void DefeatViewModel_PublishesResultAndNewRunIntent()
        {
            var viewModel = new RunDefeatViewModel();
            RunDefeatViewState publishedState = default;
            bool newRunRequested = false;
            viewModel.Changed += state => publishedState = state;
            viewModel.NewRunRequested += () => newRunRequested = true;

            viewModel.Refresh(battleNumber: 7, victories: 6, rewardsClaimed: 5);
            viewModel.RequestNewRun();

            Assert.That(publishedState.BattleNumber, Is.EqualTo(7));
            Assert.That(publishedState.Victories, Is.EqualTo(6));
            Assert.That(publishedState.RewardsClaimed, Is.EqualTo(5));
            Assert.That(newRunRequested, Is.True);
        }
    }
}
