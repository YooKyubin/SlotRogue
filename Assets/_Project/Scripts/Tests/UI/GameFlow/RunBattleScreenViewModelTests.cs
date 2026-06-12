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
                },
                upcomingActions: new[]
                {
                    new EnemyUpcomingActionViewData(EnemyUpcomingActionKind.Damage, amount: 4),
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
            Assert.That(shown.UpcomingActions, Has.Length.EqualTo(1));
            Assert.That(shown.UpcomingActions[0].Kind, Is.EqualTo(EnemyUpcomingActionKind.Damage));
            Assert.That(shown.UpcomingActions[0].Amount, Is.EqualTo(4));
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
        public void EnemySlotState_ReturnsUpcomingActionCopies()
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
                upcomingActions: new[]
                {
                    new EnemyUpcomingActionViewData(EnemyUpcomingActionKind.Shield, amount: 3),
                });

            EnemyUpcomingActionViewData[] actions = viewModel.State.EnemySlots[0].UpcomingActions;
            actions[0] = new EnemyUpcomingActionViewData(EnemyUpcomingActionKind.Damage, amount: 9);

            Assert.That(viewModel.State.EnemySlots[0].UpcomingActions[0].Kind, Is.EqualTo(EnemyUpcomingActionKind.Shield));
            Assert.That(viewModel.State.EnemySlots[0].UpcomingActions[0].Amount, Is.EqualTo(3));
        }

        [Test]
        public void EnemyVisibleIntentState_Refresh_PreservesEffectOrderAndDuplicates()
        {
            var battle = new BattleSystem();
            CombatParticipant player = CreatePlayer();
            CombatParticipant enemy = CreateEnemy(id: 100, maxHp: 20);
            battle.StartBattle(
                player,
                enemy,
                new MonsterTurnSchedule(new[]
                {
                    new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy),
                    new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy),
                    new CombatEffect(CombatEffectKind.Shield, 4, CombatEffectTarget.Self),
                }));

            var visibleIntentState = new EnemyVisibleIntentState();
            visibleIntentState.RefreshFromBattle(battle, battle.Enemies);
            System.Collections.Generic.IReadOnlyList<EnemyUpcomingActionViewData> actions =
                visibleIntentState.GetActions(enemy.Id);

            Assert.That(actions, Has.Length.EqualTo(3));
            Assert.That(actions[0].Kind, Is.EqualTo(EnemyUpcomingActionKind.Damage));
            Assert.That(actions[0].Amount, Is.EqualTo(2));
            Assert.That(actions[1].Kind, Is.EqualTo(EnemyUpcomingActionKind.Damage));
            Assert.That(actions[1].Amount, Is.EqualTo(3));
            Assert.That(actions[2].Kind, Is.EqualTo(EnemyUpcomingActionKind.Shield));
            Assert.That(actions[2].Amount, Is.EqualTo(4));
        }

        [Test]
        public void EnemyVisibleIntentState_Refresh_MapsSupportedEffectKinds()
        {
            var battle = new BattleSystem();
            CombatParticipant player = CreatePlayer();
            CombatParticipant enemy = CreateEnemy(id: 100, maxHp: 20);
            battle.StartBattle(
                player,
                enemy,
                new MonsterTurnSchedule(new[]
                {
                    new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy),
                    new CombatEffect(CombatEffectKind.Shield, 3, CombatEffectTarget.Self),
                    new CombatEffect(CombatEffectKind.Heal, 4, CombatEffectTarget.Self),
                    CombatEffect.ApplyStatus(
                        new StatusEffectSpec(StatusEffectKind.Burn, duration: 2, magnitude: 1, StatusStackMode.Refresh),
                        CombatEffectTarget.Enemy),
                }));

            var visibleIntentState = new EnemyVisibleIntentState();
            visibleIntentState.RefreshFromBattle(battle, battle.Enemies);
            EnemyUpcomingActionKind[] kinds =
                System.Array.ConvertAll(
                    ToArray(visibleIntentState.GetActions(enemy.Id)),
                    action => action.Kind);

            Assert.That(
                kinds,
                Is.EqualTo(new[]
                {
                    EnemyUpcomingActionKind.Damage,
                    EnemyUpcomingActionKind.Shield,
                    EnemyUpcomingActionKind.Heal,
                    EnemyUpcomingActionKind.ApplyStatus,
                }));
        }

        [Test]
        public void EnemyVisibleIntentState_Refresh_ReturnsEmptyWhenMissingOrNoActions()
        {
            var battle = new BattleSystem();
            CombatParticipant player = CreatePlayer();
            CombatParticipant enemy = CreateEnemy(id: 100, maxHp: 20);
            battle.StartBattle(player, enemy, System.Array.Empty<CombatEffect>());

            var visibleIntentState = new EnemyVisibleIntentState();
            visibleIntentState.RefreshFromBattle(battle, battle.Enemies);

            Assert.That(
                visibleIntentState.GetActions(enemy.Id),
                Is.Empty);
            Assert.That(
                visibleIntentState.GetActions(new CombatParticipantId(999)),
                Is.Empty);
        }

        [Test]
        public void EnemyVisibleIntentState_ConsumeFirstAction_RemovesOnlyFirstVisibleAction()
        {
            var battle = new BattleSystem();
            CombatParticipant player = CreatePlayer();
            CombatParticipant enemy = CreateEnemy(id: 100, maxHp: 20);
            battle.StartBattle(
                player,
                enemy,
                new MonsterTurnSchedule(new[]
                {
                    new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy),
                    new CombatEffect(CombatEffectKind.Damage, 3, CombatEffectTarget.Enemy),
                    new CombatEffect(CombatEffectKind.Shield, 4, CombatEffectTarget.Self),
                }));

            var visibleIntentState = new EnemyVisibleIntentState();
            visibleIntentState.RefreshFromBattle(battle, battle.Enemies);

            visibleIntentState.ConsumeFirstAction(enemy.Id);

            System.Collections.Generic.IReadOnlyList<EnemyUpcomingActionViewData> actions =
                visibleIntentState.GetActions(enemy.Id);
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0].Kind, Is.EqualTo(EnemyUpcomingActionKind.Damage));
            Assert.That(actions[0].Amount, Is.EqualTo(3));
            Assert.That(actions[1].Kind, Is.EqualTo(EnemyUpcomingActionKind.Shield));
            Assert.That(actions[1].Amount, Is.EqualTo(4));
        }

        [Test]
        public void EnemyVisibleIntentState_DoesNotReadNextScheduleUntilRefresh()
        {
            var battle = new BattleSystem();
            CombatParticipant player = CreatePlayer();
            CombatParticipant enemy = CreateEnemy(id: 100, maxHp: 20);
            battle.StartBattle(
                player,
                enemy,
                new MonsterTurnSchedule(
                    new[] { new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy) },
                    new[] { new CombatEffect(CombatEffectKind.Shield, 5, CombatEffectTarget.Self) }));

            var visibleIntentState = new EnemyVisibleIntentState();
            visibleIntentState.RefreshFromBattle(battle, battle.Enemies);

            battle.ApplyPlayerTurn(System.Array.Empty<CombatEffect>());

            System.Collections.Generic.IReadOnlyList<EnemyUpcomingActionViewData> actions =
                visibleIntentState.GetActions(enemy.Id);
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].Kind, Is.EqualTo(EnemyUpcomingActionKind.Damage));
            Assert.That(actions[0].Amount, Is.EqualTo(2));

            visibleIntentState.RefreshFromBattle(battle, battle.Enemies);

            actions = visibleIntentState.GetActions(enemy.Id);
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].Kind, Is.EqualTo(EnemyUpcomingActionKind.Shield));
            Assert.That(actions[0].Amount, Is.EqualTo(5));
        }

        private static EnemyUpcomingActionViewData[] ToArray(
            System.Collections.Generic.IReadOnlyList<EnemyUpcomingActionViewData> actions)
        {
            var array = new EnemyUpcomingActionViewData[actions.Count];
            for (int index = 0; index < actions.Count; index++)
            {
                array[index] = actions[index];
            }

            return array;
        }

        private static CombatParticipant CreatePlayer() =>
            new(30, 30, 0, new CombatParticipantId(1), CombatTeam.Player);

        private static CombatParticipant CreateEnemy(int id, int maxHp) =>
            new(maxHp, maxHp, 0, new CombatParticipantId(id), CombatTeam.Enemy);

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
