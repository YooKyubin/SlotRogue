using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SlotRogue.UI.App;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.Leaderboard;
using SlotRogue.UI.RunGame;
using SlotRogue.UI.RunGame.ViewModels;
using UnityEngine;

namespace SlotRogue.UI.Tests.RunGame
{
    /// <summary>
    /// 흐름 제어자 RunGameFlowController의 순수 C# 단위 테스트입니다(ADR-0020).
    /// MonoBehaviour 협력자는 인터페이스 대역(fake)으로 주입하고, 화면 상태는 실제 ViewModel을 씁니다.
    /// 광고/씬 이동(static AdsManager/GameSceneLoader)을 건드리지 않는 핵심 흐름만 검증합니다.
    /// </summary>
    public sealed class RunGameFlowControllerTests
    {
        private FakeNavigator _navigator;
        private FakeBattleSceneController _battleScene;
        private FakeTutorialOverlay _overlay;
        private FakeDefeatPortrait _defeatPortrait;
        private FakeBattleView _battleView;
        private RunRewardViewModel _rewardVM;
        private RunHUDViewModel _hudVM;
        private RunInventoryViewModel _inventoryVM;
        private RunDefeatViewModel _defeatVM;
        private LeaderboardViewModel _leaderboardVM;
        private RunBattleTutorialSequenceDefinition _tutorialSequenceDefinition;

        [SetUp]
        public void SetUp()
        {
            GameFlowSession.StartNewRun();

            _navigator = new FakeNavigator();
            _battleScene = new FakeBattleSceneController();
            _overlay = new FakeTutorialOverlay();
            _defeatPortrait = new FakeDefeatPortrait();
            _battleView = new FakeBattleView();
            _rewardVM = new RunRewardViewModel();
            _hudVM = new RunHUDViewModel();
            _inventoryVM = new RunInventoryViewModel();
            _defeatVM = new RunDefeatViewModel();
            _leaderboardVM = new LeaderboardViewModel();
            _tutorialSequenceDefinition = CreateTutorialSequenceDefinition();
        }

        [TearDown]
        public void TearDown()
        {
            if (_tutorialSequenceDefinition != null)
            {
                UnityEngine.Object.DestroyImmediate(_tutorialSequenceDefinition);
                _tutorialSequenceDefinition = null;
            }
        }

        private RunGameFlowController CreateController(bool hasInSceneLeaderboard = true)
        {
            var presenter = new RunGamePresenter(
                _rewardVM,
                _hudVM,
                _inventoryVM,
                _defeatVM,
                _leaderboardVM);
            return new RunGameFlowController(
                _navigator,
                presenter,
                _battleView,
                _battleScene,
                _overlay,
                _tutorialSequenceDefinition,
                _defeatPortrait,
                hasInSceneLeaderboard,
                CancellationToken.None);
        }

        private static RunBattleTutorialSequenceDefinition CreateTutorialSequenceDefinition()
        {
            var definition = ScriptableObject.CreateInstance<RunBattleTutorialSequenceDefinition>();
            definition.ConfigureForRuntime(
                new[]
                {
                    new RunBattleTutorialStep(
                        RunBattleTutorialTargetKey.Spin,
                        "Start: defeat within 5 turns. Press SPIN."),
                    new RunBattleTutorialStep(
                        RunBattleTutorialTargetKey.SwapDecision,
                        "Decision: attack power uses slot result. SWAP skips star fragments."),
                    new RunBattleTutorialStep(
                        RunBattleTutorialTargetKey.Shop,
                        "Result: slot converts into damage. Buy relics in the relic shop."),
                    new RunBattleTutorialStep(
                        RunBattleTutorialTargetKey.Spin,
                        "Enemy turn: shop refreshes when the monster dies."),
                },
                "Tutorial complete.");
            return definition;
        }

        [Test]
        public void HandleBattleEntered_FreshEntry_BeginsBattle()
        {
            RunGameFlowController flow = CreateController();

            flow.HandleBattleEntered();

            Assert.That(_battleScene.BeginBattleCount, Is.EqualTo(1));
        }

        [Test]
        public void GetInitialRunState_NewRun_StartsBattleWithoutStarterSelection()
        {
            Assert.That(GameFlowSession.HasStarterRelic, Is.False);
            Assert.That(
                RunGameFlowController.GetInitialRunState(),
                Is.EqualTo(RunGameState.Battle));

            _rewardVM.Refresh();
            Assert.That(_rewardVM.IsStarterSelection, Is.False);
        }

        [Test]
        public void StartTutorialRun_DoesNotGrantStarterRelic()
        {
            GameFlowSession.StartTutorialRun();

            Assert.That(GameFlowSession.OwnedRelics.Count, Is.EqualTo(0));
            Assert.That(
                RunGameFlowController.GetInitialRunState(),
                Is.EqualTo(RunGameState.Battle));
        }

        [Test]
        public void HandleBattleEntered_TutorialRun_PlaysNarrationThenBeginsNormalBattle()
        {
            FirstRunTutorialState.ResetForDebug();
            GameFlowSession.StartTutorialRun();
            _tutorialSequenceDefinition.ConfigureForRuntime(
                new[]
                {
                    new RunBattleTutorialStep(
                        RunBattleTutorialTargetKey.Enemy,
                        "line 1",
                        false,
                        RunTutorialMessagePlacement.Top),
                    new RunBattleTutorialStep(
                        RunBattleTutorialTargetKey.Spin,
                        "line 2",
                        true,
                        RunTutorialMessagePlacement.Bottom),
                },
                string.Empty);
            RunGameFlowController flow = CreateController();

            try
            {
                // 전투 진입: 첫 안내만 표시되고 실제 전투는 아직 시작하지 않는다.
                flow.HandleBattleEntered();
                Assert.That(_overlay.LastMessage, Is.EqualTo("line 1"));
                Assert.That(_overlay.LastStep.TargetKey, Is.EqualTo(RunBattleTutorialTargetKey.Enemy));
                Assert.That(_overlay.LastStep.MessagePlacement, Is.EqualTo(RunTutorialMessagePlacement.Top));
                Assert.That(_battleScene.BeginBattleCount, Is.EqualTo(1));
                Assert.That(_battleScene.TutorialSpinBlocked, Is.True);
                Assert.That(_battleScene.TutorialTargetSelectionBlocked, Is.True);
                Assert.That(GameFlowSession.IsTutorialRun, Is.True);

                // 화면을 탭하면 다음 안내로 진행한다.
                _overlay.InvokeAdvance();
                Assert.That(_overlay.LastMessage, Is.EqualTo("line 2"));
                Assert.That(_overlay.LastStep.TargetKey, Is.EqualTo(RunBattleTutorialTargetKey.Spin));
                Assert.That(_overlay.LastStep.ShowHand, Is.True);
                Assert.That(_battleScene.BeginBattleCount, Is.EqualTo(1));

                // 마지막 안내에서 탭하면 튜토리얼을 완료하고 일반 전투를 시작한다.
                _overlay.InvokeAdvance();
                Assert.That(GameFlowSession.IsTutorialRun, Is.False);
                Assert.That(FirstRunTutorialState.IsCompleted, Is.True);
                Assert.That(_battleScene.BeginBattleCount, Is.EqualTo(1));
                Assert.That(_battleScene.TutorialSpinBlocked, Is.False);
                Assert.That(_battleScene.TutorialTargetSelectionBlocked, Is.False);
                Assert.That(_overlay.HideCount, Is.EqualTo(1));
            }
            finally
            {
                FirstRunTutorialState.ResetForDebug();
            }
        }

        [Test]
        public void HandleBattleEntered_TutorialRunWithoutNarration_BeginsBattleImmediately()
        {
            FirstRunTutorialState.ResetForDebug();
            GameFlowSession.StartTutorialRun();
            _tutorialSequenceDefinition.ConfigureForRuntime(
                Array.Empty<RunBattleTutorialStep>(),
                string.Empty);
            RunGameFlowController flow = CreateController();

            try
            {
                flow.HandleBattleEntered();

                Assert.That(GameFlowSession.IsTutorialRun, Is.False);
                Assert.That(FirstRunTutorialState.IsCompleted, Is.True);
                Assert.That(_battleScene.BeginBattleCount, Is.EqualTo(1));
            }
            finally
            {
                FirstRunTutorialState.ResetForDebug();
            }
        }

        [Test]
        public void OnBattleVictory_TutorialRun_MarksCompletedAndContinuesAsNormalRun()
        {
            FirstRunTutorialState.ResetForDebug();
            GameFlowSession.StartTutorialRun();
            RunGameFlowController flow = CreateController();
            _navigator.GoTo(RunGameState.Battle);

            try
            {
                flow.OnBattleVictory();

                Assert.That(FirstRunTutorialState.IsCompleted, Is.True);
                Assert.That(GameFlowSession.IsTutorialRun, Is.False);
                Assert.That(_navigator.CurrentState, Is.EqualTo(RunGameState.Reward));
                Assert.That(_overlay.HideCount, Is.EqualTo(1));
            }
            finally
            {
                FirstRunTutorialState.ResetForDebug();
            }
        }

        [Test]
        public void OnBattleVictory_NonTutorial_GoesToReward()
        {
            RunGameFlowController flow = CreateController();
            _navigator.GoTo(RunGameState.Battle);

            flow.OnBattleVictory();

            Assert.That(_navigator.CurrentState, Is.EqualTo(RunGameState.Reward));
        }

        [Test]
        public void HandleRewardClaimed_AdvancesBattleAndReentersBattle()
        {
            RunGameFlowController flow = CreateController();
            _navigator.GoTo(RunGameState.Reward);
            int before = GameFlowSession.CurrentBattleNumber;

            flow.HandleRewardClaimed();

            Assert.That(
                GameFlowSession.CurrentBattleNumber,
                Is.EqualTo(before + 1));
            Assert.That(_navigator.CurrentState, Is.EqualTo(RunGameState.Battle));
        }

        [Test]
        public void HandleRewardClaimed_AlreadyInBattle_ReentersViaView()
        {
            RunGameFlowController flow = CreateController();
            _navigator.GoTo(RunGameState.Battle);

            flow.HandleRewardClaimed();

            // 같은 Battle 상태면 Navigator.GoTo는 no-op이므로 View를 직접 재진입시킨다.
            Assert.That(_battleView.OnEnterCount, Is.EqualTo(1));
        }

        [Test]
        public void HandleInventoryOpenRequested_NonTutorial_OpensInventory()
        {
            RunGameFlowController flow = CreateController();

            flow.HandleInventoryOpenRequested();

            Assert.That(_inventoryVM.State.CurrentValue.IsOpen, Is.True);
            Assert.That(_inventoryVM.State.CurrentValue.IsRelicInventoryOpen, Is.True);
            Assert.That(_inventoryVM.State.CurrentValue.IsDescriptionOpen, Is.False);
        }

        [Test]
        public void HandleInventoryCloseRequested_ClosesInventory()
        {
            RunGameFlowController flow = CreateController();
            flow.HandleInventoryOpenRequested();

            flow.HandleInventoryCloseRequested();

            Assert.That(_inventoryVM.State.CurrentValue.IsOpen, Is.False);
        }

        [Test]
        public void HandleGiveUpRequested_ShowsRunResultAndEndsRun()
        {
            RunGameFlowController flow = CreateController();
            _navigator.GoTo(RunGameState.Battle);
            int battleNumber = GameFlowSession.CurrentBattleNumber;

            flow.HandleGiveUpRequested();

            RunDefeatViewState state = _defeatVM.State.CurrentValue;
            Assert.That(_navigator.CurrentState, Is.EqualTo(RunGameState.Defeat));
            Assert.That(state.Phase, Is.EqualTo(RunDefeatPhase.RunResult));
            Assert.That(state.BattleNumber, Is.EqualTo(battleNumber));
            Assert.That(state.IsResultVisible, Is.True);
            Assert.That(GameFlowSession.HasRun, Is.False);
        }

        // ── 인터페이스 대역(fake) ────────────────────────────────────────

        private sealed class FakeNavigator : IRunGameNavigator
        {
            public RunGameState CurrentState { get; private set; } = RunGameState.None;

            public void GoTo(RunGameState nextState)
            {
                CurrentState = nextState;
            }
        }

        private sealed class FakeBattleSceneController : IBattleSceneController
        {
            public int BeginBattleCount { get; private set; }

            public int PrepareBattleEntryCount { get; private set; }

            public bool TutorialSpinBlocked { get; private set; }

            public bool TutorialTargetSelectionBlocked { get; private set; }

            public bool ReviveResult { get; set; }

            public RunBattleTutorialTargets TutorialTargets => RunBattleTutorialTargets.Empty;

            public UniTask PrepareBattleEntryAsync(CancellationToken cancellationToken)
            {
                PrepareBattleEntryCount++;
                return UniTask.CompletedTask;
            }

            public void BeginBattle() => BeginBattleCount++;

            public void SetTutorialSpinBlocked(bool blocked)
            {
                TutorialSpinBlocked = blocked;
            }

            public void SetTutorialTargetSelectionBlocked(bool blocked)
            {
                TutorialTargetSelectionBlocked = blocked;
            }

            public Sprite GetDefeatingMonsterPortrait() => null;

            public void FinalizePendingDefeat() { }

            public bool TryRevive() => ReviveResult;
        }

        private sealed class FakeTutorialOverlay : ITutorialOverlay
        {
            private Action _pendingAdvance;

            public int HideCount { get; private set; }

            public int SpotlightCount { get; private set; }

            public int NarrationCount { get; private set; }

            public int StepCount { get; private set; }

            public string LastMessage { get; private set; }

            public RunBattleTutorialStep LastStep { get; private set; }

            public void Hide() => HideCount++;

            public void ShowMessage(string message) => LastMessage = message;

            public void ShowSpotlight(RectTransform target, string message, bool showHand)
            {
                SpotlightCount++;
                LastMessage = message;
            }

            public void ShowStep(
                RectTransform target,
                RunBattleTutorialStep step,
                Action onAdvance)
            {
                StepCount++;
                LastStep = step;
                LastMessage = step?.Message;
                _pendingAdvance = onAdvance;
            }

            public void ShowNarration(string message, Action onAdvance)
            {
                NarrationCount++;
                LastMessage = message;
                _pendingAdvance = onAdvance;
            }

            /// <summary>사용자가 화면을 탭한 것처럼 다음 문구로 진행시킨다.</summary>
            public void InvokeAdvance()
            {
                Action advance = _pendingAdvance;
                _pendingAdvance = null;
                advance?.Invoke();
            }
        }

        private sealed class FakeDefeatPortrait : IDefeatPortraitView
        {
            public void SetMonsterPortrait(Sprite portrait) { }
        }

        private sealed class FakeBattleView : IRunGameView
        {
            public int OnEnterCount { get; private set; }

            public void OnEnter() => OnEnterCount++;

            public void OnExit() { }
        }
    }
}
