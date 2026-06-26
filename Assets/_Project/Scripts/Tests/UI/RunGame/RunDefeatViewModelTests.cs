using NUnit.Framework;
using SlotRogue.UI.RunGame.ViewModels;

namespace SlotRogue.UI.Tests.RunGame
{
    /// <summary>
    /// RunDefeatViewModel의 화면 상태 전이 단위 테스트입니다(순수 C#, 정적 의존 없음).
    /// 값은 모두 인자로 주입되므로 GameFlowSession 셋업이 필요 없습니다.
    /// </summary>
    public sealed class RunDefeatViewModelTests
    {
        [Test]
        public void ShowReviveOffer_EntersReviveOfferPhase_VisibleButNotInteractableWithoutAd()
        {
            var vm = new RunDefeatViewModel();

            vm.ShowReviveOffer(3, 2, 1, 5);

            RunDefeatViewState state = vm.State.CurrentValue;
            Assert.That(state.Phase, Is.EqualTo(RunDefeatPhase.ReviveOffer));
            Assert.That(state.IsReviveOffer, Is.True);
            Assert.That(state.IsReviveVisible, Is.True);
            // 광고가 준비되지 않으면 부활 버튼은 보이되 비활성.
            Assert.That(state.CanRevive, Is.False);
            Assert.That(state.CountdownLabel, Is.EqualTo("5"));
        }

        [Test]
        public void ShowReviveOffer_ThenAdReady_CanRevive()
        {
            var vm = new RunDefeatViewModel();
            vm.ShowReviveOffer(1, 0, 0, 5);

            vm.SetRewardedAvailability(isReady: true, adsRemoved: false);

            Assert.That(vm.State.CurrentValue.CanRevive, Is.True);
        }

        [Test]
        public void SetRevivePending_ShowsAwaitingStateAndDisablesRevive()
        {
            var vm = new RunDefeatViewModel();
            vm.ShowReviveOffer(1, 0, 0, 5);
            vm.SetRewardedAvailability(isReady: true, adsRemoved: false);

            vm.SetRevivePending();

            RunDefeatViewState state = vm.State.CurrentValue;
            Assert.That(state.CanRevive, Is.False);
            Assert.That(state.ReviveLabel, Does.Contain("광고 재생"));
            Assert.That(state.CountdownLabel, Is.Empty);
        }

        [Test]
        public void UpdateReviveCountdown_UpdatesLabel()
        {
            var vm = new RunDefeatViewModel();
            vm.ShowReviveOffer(1, 0, 0, 5);

            vm.UpdateReviveCountdown(2);

            Assert.That(vm.State.CurrentValue.CountdownLabel, Is.EqualTo("2"));
        }

        [Test]
        public void ShowResult_EntersRunResultPhaseWithSummary()
        {
            var vm = new RunDefeatViewModel();

            vm.ShowResult(3, 2, 1, hasRevived: false, contributionSummary: "X");

            RunDefeatViewState state = vm.State.CurrentValue;
            Assert.That(state.Phase, Is.EqualTo(RunDefeatPhase.RunResult));
            Assert.That(state.IsResultVisible, Is.True);
            Assert.That(state.Summary, Does.Contain("BATTLE 3"));
        }

        [Test]
        public void RequestRestart_OnlyFiresInRunResultPhase()
        {
            var vm = new RunDefeatViewModel();
            int fired = 0;
            vm.RestartRequested += () => fired++;

            vm.ShowReviveOffer(1, 0, 0, 5);
            vm.RequestRestart();
            Assert.That(fired, Is.EqualTo(0), "ReviveOffer 단계에서는 RESTART가 무시돼야 한다.");

            vm.ShowResult(1, 0, 0, hasRevived: false, contributionSummary: "X");
            vm.RequestRestart();
            Assert.That(fired, Is.EqualTo(1));
        }

        [Test]
        public void RequestRevive_FiresOnlyWhenReviveIsInteractable()
        {
            var vm = new RunDefeatViewModel();
            int fired = 0;
            vm.ReviveRequested += () => fired++;

            vm.ShowReviveOffer(1, 0, 0, 5);
            vm.RequestRevive();
            Assert.That(fired, Is.EqualTo(0), "광고 미준비 시 부활 요청은 무시돼야 한다.");

            vm.SetRewardedAvailability(isReady: true, adsRemoved: false);
            vm.RequestRevive();
            Assert.That(fired, Is.EqualTo(1));
        }
    }
}
