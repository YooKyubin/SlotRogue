using System;

namespace SlotRogue.UI.RunGame.ViewModels
{
    public enum RunDefeatPhase
    {
        ReviveOffer = 0,
        RunResult = 1,
    }

    public sealed class RunDefeatViewModel
    {
        private int _battleNumber;
        private int _victories;
        private int _rewardsClaimed;
        private int _countdownSeconds;
        private string _contributionSummary = string.Empty;
        private bool _hasRevived;
        private bool _canRevive;
        private bool _rewardedAdReady;
        private bool _adsRemoved;
        private bool _isAwaitingAd;

        public RunDefeatViewModel()
        {
            State = RunDefeatViewState.Empty;
        }

        public event Action<RunDefeatViewState> Changed;

        public event Action RestartRequested;

        public event Action RankingRequested;

        public event Action HomeRequested;

        public event Action ReviveRequested;

        public RunDefeatViewState State { get; private set; }

        public void ShowReviveOffer(
            int battleNumber,
            int victories,
            int rewardsClaimed,
            int countdownSeconds)
        {
            SetRunResultValues(battleNumber, victories, rewardsClaimed);
            _countdownSeconds = Math.Max(0, countdownSeconds);
            _contributionSummary = string.Empty;
            _canRevive = true;
            _isAwaitingAd = false;
            Publish(RunDefeatPhase.ReviveOffer);
        }

        public void ShowResult(
            int battleNumber,
            int victories,
            int rewardsClaimed,
            bool hasRevived,
            string contributionSummary)
        {
            SetRunResultValues(battleNumber, victories, rewardsClaimed);
            _countdownSeconds = 0;
            _contributionSummary = contributionSummary ?? string.Empty;
            _hasRevived = hasRevived;
            _canRevive = false;
            _isAwaitingAd = false;
            Publish(RunDefeatPhase.RunResult);
        }

        public void UpdateReviveCountdown(int countdownSeconds)
        {
            if (State.Phase != RunDefeatPhase.ReviveOffer || _isAwaitingAd)
            {
                return;
            }

            int normalizedSeconds = Math.Max(0, countdownSeconds);
            if (_countdownSeconds == normalizedSeconds)
            {
                return;
            }

            _countdownSeconds = normalizedSeconds;
            Publish(RunDefeatPhase.ReviveOffer);
        }

        public void SetRevivePending()
        {
            if (State.Phase != RunDefeatPhase.ReviveOffer || !_canRevive)
            {
                return;
            }

            _isAwaitingAd = true;
            Publish(RunDefeatPhase.ReviveOffer);
        }

        public void SetRewardedAdReady(bool isReady)
        {
            SetRewardedAvailability(isReady, _adsRemoved);
        }

        public void SetRewardedAvailability(
            bool isReady,
            bool adsRemoved)
        {
            if (_rewardedAdReady == isReady &&
                _adsRemoved == adsRemoved)
            {
                return;
            }

            _rewardedAdReady = isReady;
            _adsRemoved = adsRemoved;
            Publish(State.Phase);
        }

        public void SetCanRevive(bool canRevive)
        {
            if (_canRevive == canRevive)
            {
                return;
            }

            _canRevive = canRevive;
            Publish(State.Phase);
        }

        public void RequestRestart()
        {
            if (State.Phase == RunDefeatPhase.RunResult)
            {
                RestartRequested?.Invoke();
            }
        }

        public void RequestRanking()
        {
            if (State.Phase == RunDefeatPhase.RunResult)
            {
                RankingRequested?.Invoke();
            }
        }

        public void RequestHome()
        {
            if (State.Phase == RunDefeatPhase.RunResult)
            {
                HomeRequested?.Invoke();
            }
        }

        public void RequestRevive()
        {
            if (State.CanRevive)
            {
                ReviveRequested?.Invoke();
            }
        }

        private void SetRunResultValues(
            int battleNumber,
            int victories,
            int rewardsClaimed)
        {
            _battleNumber = Math.Max(0, battleNumber);
            _victories = Math.Max(0, victories);
            _rewardsClaimed = Math.Max(0, rewardsClaimed);
        }

        private void Publish(RunDefeatPhase phase)
        {
            bool isReviveOffer = phase == RunDefeatPhase.ReviveOffer;
            string title = isReviveOffer ? "CONTINUE?" : "RUN RESULT";
            string summary = isReviveOffer
                ? string.Empty
                : $"BATTLE {_battleNumber}\nVICTORIES {_victories}\n" +
                  $"REWARDS {_rewardsClaimed}\nREVIVED {(_hasRevived ? "YES" : "NO")}";
            string countdownLabel = _isAwaitingAd
                ? "광고 완료 후 부활"
                : $"REVIVE WINDOW  {_countdownSeconds}";
            string reviveLabel = _isAwaitingAd
                ? "광고 재생 중..."
                : _adsRemoved
                    ? "광고 없이 부활"
                    : _rewardedAdReady
                        ? "광고 보고 부활"
                        : "광고 준비 중...";
            bool canUseRewarded = _adsRemoved || _rewardedAdReady;

            State = new RunDefeatViewState(
                phase,
                _battleNumber,
                _victories,
                _rewardsClaimed,
                title,
                summary,
                _contributionSummary,
                countdownLabel,
                "RESTART",
                "RANKING",
                "HOME",
                reviveLabel,
                isReviveOffer && _canRevive,
                isReviveOffer &&
                    _canRevive &&
                    canUseRewarded &&
                    !_isAwaitingAd);
            Changed?.Invoke(State);
        }
    }

    public readonly struct RunDefeatViewState
    {
        public static readonly RunDefeatViewState Empty = new(
            RunDefeatPhase.RunResult,
            0,
            0,
            0,
            "RUN RESULT",
            string.Empty,
            string.Empty,
            string.Empty,
            "RESTART",
            "RANKING",
            "HOME",
            "광고 보고 부활",
            false,
            false);

        public RunDefeatViewState(
            RunDefeatPhase phase,
            int battleNumber,
            int victories,
            int rewardsClaimed,
            string title,
            string summary,
            string contributionSummary,
            string countdownLabel,
            string restartLabel,
            string rankingLabel,
            string homeLabel,
            string reviveLabel,
            bool isReviveVisible,
            bool canRevive)
        {
            Phase = phase;
            BattleNumber = Math.Max(0, battleNumber);
            Victories = Math.Max(0, victories);
            RewardsClaimed = Math.Max(0, rewardsClaimed);
            Title = title ?? string.Empty;
            Summary = summary ?? string.Empty;
            ContributionSummary = contributionSummary ?? string.Empty;
            CountdownLabel = countdownLabel ?? string.Empty;
            RestartLabel = restartLabel ?? string.Empty;
            RankingLabel = rankingLabel ?? string.Empty;
            HomeLabel = homeLabel ?? string.Empty;
            ReviveLabel = reviveLabel ?? string.Empty;
            IsReviveVisible = isReviveVisible;
            CanRevive = isReviveVisible && canRevive;
        }

        public RunDefeatPhase Phase { get; }

        public int BattleNumber { get; }

        public int Victories { get; }

        public int RewardsClaimed { get; }

        public string Title { get; }

        public string Summary { get; }

        public string ContributionSummary { get; }

        public string CountdownLabel { get; }

        public string RestartLabel { get; }

        public string RankingLabel { get; }

        public string HomeLabel { get; }

        public string ReviveLabel { get; }

        public bool IsReviveVisible { get; }

        public bool CanRevive { get; }

        public bool IsReviveOffer => Phase == RunDefeatPhase.ReviveOffer;

        public bool IsResultVisible => Phase == RunDefeatPhase.RunResult;
    }
}
