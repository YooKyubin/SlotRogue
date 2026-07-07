using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.Leaderboard;
using SlotRogue.UI.RunGame.ViewModels;

namespace SlotRogue.UI.RunGame
{
    /// <summary>
    /// RunGame 화면 상태 갱신(Presenter)을 담당한다.
    /// 흐름 제어자(<see cref="RunGameFlowController"/>)가 결정한 결과를 각 ViewModel에 반영한다.
    /// ViewModel 생성/소유는 RunGameSceneRoot가 하고 여기서는 참조만 받는다.
    /// 화면 전환·전투 수명·광고/타이머 같은 흐름 판단은 이 타입에 넣지 않는다(ADR-0020).
    /// </summary>
    public sealed class RunGamePresenter
    {
        private readonly RunRewardViewModel _rewardVM;
        private readonly RunHUDViewModel _hudVM;
        private readonly RunInventoryViewModel _inventoryVM;
        private readonly RunDefeatViewModel _defeatVM;
        private readonly LeaderboardViewModel _leaderboardVM;

        public RunGamePresenter(
            RunRewardViewModel rewardVM,
            RunHUDViewModel hudVM,
            RunInventoryViewModel inventoryVM,
            RunDefeatViewModel defeatVM,
            LeaderboardViewModel leaderboardVM)
        {
            _rewardVM = rewardVM;
            _hudVM = hudVM;
            _inventoryVM = inventoryVM;
            _defeatVM = defeatVM;
            _leaderboardVM = leaderboardVM;
        }

        /// <summary>직전 <see cref="RefreshReward"/> 시점 기준 '시작 유물 선택' 단계 여부(흐름 분기용).</summary>
        public bool IsStarterRewardSelection => _rewardVM.IsStarterSelection;

        // ── HUD ──────────────────────────────────────────────────────────
        public void RefreshHud() => _hudVM.Refresh();

        // ── 보상 ─────────────────────────────────────────────────────────
        public void RefreshReward() => _rewardVM.Refresh();

        public void ClaimReward(int optionIndex) => _rewardVM.ClaimReward(optionIndex);

        public void ApplyRewardedReroll() => _rewardVM.ApplyRewardedReroll();

        public void ApplyRewardedExtraReward() => _rewardVM.ApplyRewardedExtraReward();

        public void ApplyRewardedDouble() => _rewardVM.ApplyRewardedDouble();

        public void SetRewardedAvailability(
            bool rerollReady,
            bool extraRewardReady,
            bool rewardDoubleReady,
            bool reviveReady,
            bool adsRemoved)
        {
            _rewardVM.SetRewardedAvailability(
                rerollReady,
                extraRewardReady,
                rewardDoubleReady,
                adsRemoved);
            _defeatVM.SetRewardedAvailability(reviveReady, adsRemoved);
        }

        // ── 인벤토리 ─────────────────────────────────────────────────────
        public void RefreshInventory() => _inventoryVM.Refresh();

        public void CloseInventory() => _inventoryVM.Close();

        public void OpenRelicInventory() => _inventoryVM.OpenRelicInventory();

        public void CloseRelicInventory() => _inventoryVM.CloseRelicInventory();

        public void OpenDescription() => _inventoryVM.OpenDescription();

        public void CloseDescription() => _inventoryVM.CloseDescription();

        public void SelectInventoryTab(RunInventoryTab tab) => _inventoryVM.SelectTab(tab);

        // ── 패배/부활 ────────────────────────────────────────────────────
        public void ShowReviveOffer(
            int battleNumber,
            int victories,
            int rewardsClaimed,
            int countdownSeconds)
        {
            _defeatVM.ShowReviveOffer(
                battleNumber,
                victories,
                rewardsClaimed,
                countdownSeconds);
        }

        public void ShowDefeatResult(
            int battleNumber,
            int victories,
            int rewardsClaimed,
            bool hasRevived,
            IReadOnlyList<SlotSymbolContributionSnapshot> symbolContributions)
        {
            _defeatVM.ShowResult(
                battleNumber,
                victories,
                rewardsClaimed,
                hasRevived,
                symbolContributions);
        }

        public void SetRevivePending() => _defeatVM.SetRevivePending();

        public void SetCanRevive(bool canRevive) => _defeatVM.SetCanRevive(canRevive);

        public void UpdateReviveCountdown(int countdownSeconds) =>
            _defeatVM.UpdateReviveCountdown(countdownSeconds);

        // ── 리더보드 ─────────────────────────────────────────────────────
        public UniTask OpenLeaderboardAsync() => _leaderboardVM.OpenAsync();
    }
}
