using System;
using System.Collections.Generic;
using R3;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;

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
        private RunDefeatSymbolStatViewState[] _symbolStats =
            Array.Empty<RunDefeatSymbolStatViewState>();
        private bool _hasRevived;
        private bool _canRevive;
        private bool _rewardedAdReady;
        private bool _adsRemoved;
        private bool _isAwaitingAd;

        // 화면 상태는 R3 ReactiveProperty로 노출한다. 구독 즉시 현재 값을 1회 흘려보내므로
        // 별도 Changed 이벤트 + 초기 Render 호출이 필요 없다(구독 한 번 = 영구 동기화).
        private readonly ReactiveProperty<RunDefeatViewState> _state =
            new(RunDefeatViewState.Empty);

        public event Action RestartRequested;

        public event Action RankingRequested;

        public event Action HomeRequested;

        public event Action ReviveRequested;

        public ReadOnlyReactiveProperty<RunDefeatViewState> State => _state;

        public void ShowReviveOffer(
            int battleNumber,
            int victories,
            int rewardsClaimed,
            int countdownSeconds)
        {
            SetRunResultValues(battleNumber, victories, rewardsClaimed);
            _countdownSeconds = Math.Max(0, countdownSeconds);
            _contributionSummary = string.Empty;
            _symbolStats = Array.Empty<RunDefeatSymbolStatViewState>();
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
            ShowResult(
                battleNumber,
                victories,
                rewardsClaimed,
                hasRevived,
                contributionSummary,
                Array.Empty<RunDefeatSymbolStatViewState>());
        }

        public void ShowResult(
            int battleNumber,
            int victories,
            int rewardsClaimed,
            bool hasRevived,
            IReadOnlyList<SlotSymbolContributionSnapshot> symbolContributions)
        {
            RunDefeatSymbolStatViewState[] symbolStats =
                BuildSymbolStats(symbolContributions);
            ShowResult(
                battleNumber,
                victories,
                rewardsClaimed,
                hasRevived,
                BuildSymbolContributionSummary(symbolStats),
                symbolStats);
        }

        private void ShowResult(
            int battleNumber,
            int victories,
            int rewardsClaimed,
            bool hasRevived,
            string contributionSummary,
            RunDefeatSymbolStatViewState[] symbolStats)
        {
            SetRunResultValues(battleNumber, victories, rewardsClaimed);
            _countdownSeconds = 0;
            _contributionSummary = contributionSummary ?? string.Empty;
            _symbolStats = symbolStats ?? Array.Empty<RunDefeatSymbolStatViewState>();
            _hasRevived = hasRevived;
            _canRevive = false;
            _isAwaitingAd = false;
            Publish(RunDefeatPhase.RunResult);
        }

        public void UpdateReviveCountdown(int countdownSeconds)
        {
            if (_state.Value.Phase != RunDefeatPhase.ReviveOffer || _isAwaitingAd)
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
            if (_state.Value.Phase != RunDefeatPhase.ReviveOffer || !_canRevive)
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
            Publish(_state.Value.Phase);
        }

        public void SetCanRevive(bool canRevive)
        {
            if (_canRevive == canRevive)
            {
                return;
            }

            _canRevive = canRevive;
            Publish(_state.Value.Phase);
        }

        public void RequestRestart()
        {
            if (_state.Value.Phase == RunDefeatPhase.RunResult)
            {
                RestartRequested?.Invoke();
            }
        }

        public void RequestRanking()
        {
            if (_state.Value.Phase == RunDefeatPhase.RunResult)
            {
                RankingRequested?.Invoke();
            }
        }

        public void RequestHome()
        {
            if (_state.Value.Phase == RunDefeatPhase.RunResult)
            {
                HomeRequested?.Invoke();
            }
        }

        public void RequestRevive()
        {
            if (_state.Value.CanRevive)
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

        private static RunDefeatSymbolStatViewState[] BuildSymbolStats(
            IReadOnlyList<SlotSymbolContributionSnapshot> contributions)
        {
            IReadOnlyList<SlotSymbolType> symbols = SlotSymbolPool.Symbols;
            var rows = new RunDefeatSymbolStatViewState[symbols.Count];
            for (int index = 0; index < symbols.Count; index++)
            {
                SlotSymbolType symbol = symbols[index];
                SlotSymbolContributionSnapshot snapshot =
                    FindContribution(contributions, symbol);
                rows[index] = new RunDefeatSymbolStatViewState(
                    symbol,
                    RelicDisplay.SymbolKorean(symbol),
                    snapshot.PatternCount,
                    snapshot.TotalAttackPower,
                    snapshot.DefensePower);
            }

            return rows;
        }

        private static SlotSymbolContributionSnapshot FindContribution(
            IReadOnlyList<SlotSymbolContributionSnapshot> contributions,
            SlotSymbolType symbol)
        {
            if (contributions != null)
            {
                for (int index = 0; index < contributions.Count; index++)
                {
                    SlotSymbolContributionSnapshot contribution = contributions[index];
                    if (contribution.Symbol == symbol)
                    {
                        return contribution;
                    }
                }
            }

            return new SlotSymbolContributionSnapshot(symbol, 0, 0, 0, 0);
        }

        private static string BuildSymbolContributionSummary(
            IReadOnlyList<RunDefeatSymbolStatViewState> symbolStats)
        {
            if (symbolStats == null || symbolStats.Count == 0)
            {
                return "NO SYMBOLS";
            }

            var builder = new System.Text.StringBuilder();
            for (int index = 0; index < symbolStats.Count; index++)
            {
                RunDefeatSymbolStatViewState stat = symbolStats[index];
                if (index > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(stat.DisplayName);
                builder.Append("  PATTERN ");
                builder.Append(stat.PatternCount);
                builder.Append("  ATK ");
                builder.Append(stat.AttackPower);
                builder.Append("  DEF ");
                builder.Append(stat.DefensePower);
            }

            return builder.ToString();
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

            _state.Value = new RunDefeatViewState(
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
                    !_isAwaitingAd,
                _symbolStats);
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
            bool canRevive,
            IReadOnlyList<RunDefeatSymbolStatViewState> symbolStats = null)
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
            SymbolStats = CopySymbolStats(symbolStats);
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

        public IReadOnlyList<RunDefeatSymbolStatViewState> SymbolStats { get; }

        public bool IsReviveVisible { get; }

        public bool CanRevive { get; }

        public bool IsReviveOffer => Phase == RunDefeatPhase.ReviveOffer;

        public bool IsResultVisible => Phase == RunDefeatPhase.RunResult;

        private static RunDefeatSymbolStatViewState[] CopySymbolStats(
            IReadOnlyList<RunDefeatSymbolStatViewState> symbolStats)
        {
            if (symbolStats == null || symbolStats.Count == 0)
            {
                return Array.Empty<RunDefeatSymbolStatViewState>();
            }

            var copied = new RunDefeatSymbolStatViewState[symbolStats.Count];
            for (int index = 0; index < symbolStats.Count; index++)
            {
                copied[index] = symbolStats[index];
            }

            return copied;
        }
    }

    public readonly struct RunDefeatSymbolStatViewState
    {
        public RunDefeatSymbolStatViewState(
            SlotSymbolType symbol,
            string displayName,
            int patternCount,
            int attackPower,
            int defensePower)
        {
            Symbol = symbol;
            DisplayName = displayName ?? string.Empty;
            PatternCount = Math.Max(0, patternCount);
            AttackPower = Math.Max(0, attackPower);
            DefensePower = Math.Max(0, defensePower);
        }

        public SlotSymbolType Symbol { get; }

        public string DisplayName { get; }

        public int PatternCount { get; }

        public int AttackPower { get; }

        public int DefensePower { get; }
    }
}
