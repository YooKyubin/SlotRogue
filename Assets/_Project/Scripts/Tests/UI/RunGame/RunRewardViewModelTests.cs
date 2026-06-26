using NUnit.Framework;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.RunGame.ViewModels;

namespace SlotRogue.UI.Tests.RunGame
{
    /// <summary>
    /// RunRewardViewModel의 보상 옵션/광고 보강 화면 상태 단위 테스트입니다.
    /// 보상 추첨·적용은 GameFlowSession/RunRewardCatalog를 읽으므로 새 런을 셋업합니다.
    /// (추첨 RNG는 비결정적이라 개수·라벨·gating 같은 결정적 속성만 검증합니다.)
    /// </summary>
    public sealed class RunRewardViewModelTests
    {
        [SetUp]
        public void SetUp()
        {
            GameFlowSession.StartNewRun();
        }

        [Test]
        public void Refresh_ProvidesUpToThreeOptions()
        {
            var vm = new RunRewardViewModel();

            vm.Refresh();

            int count = vm.State.CurrentValue.Options.Count;
            Assert.That(count, Is.GreaterThan(0));
            Assert.That(count, Is.LessThanOrEqualTo(3));
        }

        [Test]
        public void ClaimReward_FiresRewardClaimedOnce()
        {
            var vm = new RunRewardViewModel();
            vm.Refresh();
            int fired = 0;
            vm.RewardClaimed += () => fired++;

            vm.ClaimReward(0);
            vm.ClaimReward(0); // 두 번째 수령은 무시돼야 한다.

            Assert.That(fired, Is.EqualTo(1));
            Assert.That(vm.State.CurrentValue.RerollLabel, Does.Contain("완료"));
        }

        [Test]
        public void ClaimReward_InvalidIndex_DoesNotFire()
        {
            var vm = new RunRewardViewModel();
            vm.Refresh();
            int fired = 0;
            vm.RewardClaimed += () => fired++;

            vm.ClaimReward(99);

            Assert.That(fired, Is.EqualTo(0));
        }

        [Test]
        public void ApplyRewardedDouble_MarksDoubleApplied()
        {
            var vm = new RunRewardViewModel();
            vm.Refresh();

            vm.ApplyRewardedDouble();

            Assert.That(
                vm.State.CurrentValue.DoubleRewardLabel,
                Is.EqualTo("보상 2배 적용됨"));
        }

        [Test]
        public void AdsRemoved_EnablesRerollWithoutAd()
        {
            var vm = new RunRewardViewModel();
            vm.Refresh();

            vm.SetRewardedAvailability(
                rerollAdReady: false,
                extraRewardAdReady: false,
                rewardDoubleAdReady: false,
                adsRemoved: true);

            RunRewardViewState state = vm.State.CurrentValue;
            Assert.That(state.CanReroll, Is.True);
            Assert.That(state.RerollLabel, Does.Contain("광고 없이"));
        }

        [Test]
        public void Reroll_RerollsAndKeepsOptionCount()
        {
            var vm = new RunRewardViewModel();
            vm.Refresh();
            vm.SetRewardedAvailability(true, true, true, adsRemoved: false);
            int before = vm.State.CurrentValue.Options.Count;

            vm.ApplyRewardedReroll();

            Assert.That(vm.State.CurrentValue.Options.Count, Is.EqualTo(before));
            // 리롤을 한 번 쓰면 다시 사용 불가.
            Assert.That(vm.State.CurrentValue.CanReroll, Is.False);
        }
    }
}
