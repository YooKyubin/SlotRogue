using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
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
        }

        private RunGameFlowController CreateController(bool hasInSceneLeaderboard = true)
        {
            return new RunGameFlowController(
                _navigator,
                _rewardVM,
                _hudVM,
                _inventoryVM,
                _defeatVM,
                _leaderboardVM,
                _battleView,
                _battleScene,
                _overlay,
                _defeatPortrait,
                hasInSceneLeaderboard,
                CancellationToken.None);
        }

        [Test]
        public void HandleBattleEntered_FreshEntry_BeginsBattle()
        {
            RunGameFlowController flow = CreateController();

            flow.HandleBattleEntered();

            Assert.That(_battleScene.BeginBattleCount, Is.EqualTo(1));
        }

        [Test]
        public void HandleRewardClaimed_StarterSelection_EntersBattleWithoutAdvancing()
        {
            // 시작 유물 선택은 보상 화면(RunRewardView)에 병합되었다.
            // 새 런(시작 유물 미선택)에서 Refresh하면 ViewModel이 시작 유물 모드가 된다.
            RunGameFlowController flow = CreateController();
            _navigator.GoTo(RunGameState.Reward);
            _rewardVM.Refresh();
            Assume.That(_rewardVM.IsStarterSelection, Is.True);
            int before = GameFlowSession.CurrentBattleNumber;

            flow.HandleRewardClaimed();

            Assert.That(GameFlowSession.CurrentBattleNumber, Is.EqualTo(before));
            Assert.That(_overlay.HideCount, Is.EqualTo(1));
            Assert.That(_battleScene.PrepareBattleEntryCount, Is.EqualTo(1));
            Assert.That(_navigator.CurrentState, Is.EqualTo(RunGameState.Battle));
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

            public bool ReviveResult { get; set; }

            public UniTask PrepareBattleEntryAsync(CancellationToken cancellationToken)
            {
                PrepareBattleEntryCount++;
                return UniTask.CompletedTask;
            }

            public void BeginBattle() => BeginBattleCount++;

            public void SetTutorialSpinBlocked(bool blocked) { }

            public void SetTutorialTargetSelectionBlocked(bool blocked) { }

            public Sprite GetDefeatingMonsterPortrait() => null;

            public void FinalizePendingDefeat() { }

            public bool TryRevive() => ReviveResult;
        }

        private sealed class FakeTutorialOverlay : ITutorialOverlay
        {
            public int HideCount { get; private set; }

            public string LastMessage { get; private set; }

            public void Hide() => HideCount++;

            public void ShowMessage(string message) => LastMessage = message;
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
