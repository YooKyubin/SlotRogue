using System;
using System.Collections.Generic;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.RunGame.ViewModels
{
    /// <summary>
    /// 보상 선택 화면의 상태와 커맨드를 담습니다.
    /// 등급(일반/큰 보상)에 맞는 풀에서 무작위로 N개를 뽑아 제시합니다.
    /// 순수 C# 클래스입니다.
    /// </summary>
    public sealed class RunRewardViewModel
    {
        private const int BaseOptionCount = 3;

        private static readonly Random Rng = new();

        private readonly List<RunRewardDefinition> _options = new();
        private int _optionCount = BaseOptionCount;
        private bool _rerollAdReady;
        private bool _extraRewardAdReady;
        private bool _rewardDoubleAdReady;
        private bool _adsRemoved;
        private bool _rerollUsed;
        private bool _extraRewardUsed;
        private bool _rewardDoubleUsed;
        private bool _rewardDoubleEnabled;
        private bool _rewardClaimed;

        private IReadOnlyList<RunRewardDefinition> SourcePool =>
            RunRewardCatalog.ForTier(GameFlowSession.CurrentTier);

        public RunRewardViewModel()
        {
            State = RunRewardViewState.Empty;
        }

        public event Action RewardClaimed;

        public event Action<RunRewardViewState> Changed;

        public RunRewardViewState State { get; private set; }

        public void ClaimReward(int optionIndex)
        {
            if (_rewardClaimed ||
                optionIndex < 0 ||
                optionIndex >= _options.Count)
            {
                return;
            }

            _rewardClaimed = true;
            RunRewardDefinition reward = _options[optionIndex];
            ApplyReward(reward);

            if (_rewardDoubleEnabled)
            {
                ApplyReward(reward);
            }

            PublishChanged();
            RewardClaimed?.Invoke();
        }

        public void Refresh()
        {
            _optionCount = BaseOptionCount;
            _rerollUsed = false;
            _extraRewardUsed = false;
            _rewardDoubleUsed = false;
            _rewardDoubleEnabled = false;
            _rewardClaimed = false;
            RollOptions(_optionCount);
            PublishChanged();
        }

        public void ApplyRewardedReroll()
        {
            if (_rerollUsed || _rewardClaimed)
            {
                return;
            }

            _rerollUsed = true;
            RollOptions(_optionCount);
            PublishChanged();
        }

        public void SetRewardedAdReady(bool isReady)
        {
            SetRewardedAvailability(
                isReady,
                isReady,
                isReady,
                _adsRemoved);
        }

        public void SetRewardedAvailability(
            bool rerollAdReady,
            bool extraRewardAdReady,
            bool rewardDoubleAdReady,
            bool adsRemoved)
        {
            if (_rerollAdReady == rerollAdReady &&
                _extraRewardAdReady == extraRewardAdReady &&
                _rewardDoubleAdReady == rewardDoubleAdReady &&
                _adsRemoved == adsRemoved)
            {
                return;
            }

            _rerollAdReady = rerollAdReady;
            _extraRewardAdReady = extraRewardAdReady;
            _rewardDoubleAdReady = rewardDoubleAdReady;
            _adsRemoved = adsRemoved;
            PublishChanged();
        }

        public void ApplyRewardedExtraReward()
        {
            if (_extraRewardUsed ||
                _rewardClaimed ||
                !GameFlowSession.CurrentBattleGrantsArtifact)
            {
                return;
            }

            RunRewardDefinition extra = PickOneExcluding(_options);
            if (extra == null)
            {
                return;
            }

            _extraRewardUsed = true;
            _options.Add(extra);
            _optionCount = _options.Count;
            PublishChanged();
        }

        public void ApplyRewardedDouble()
        {
            if (_rewardDoubleUsed || _rewardClaimed)
            {
                return;
            }

            _rewardDoubleUsed = true;
            _rewardDoubleEnabled = true;
            PublishChanged();
        }

        private void RollOptions(int count)
        {
            _options.Clear();

            var pool = new List<RunRewardDefinition>(SourcePool);
            int take = Math.Min(count, pool.Count);

            for (int i = 0; i < take; i++)
            {
                int idx = Rng.Next(pool.Count);
                _options.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
        }

        private RunRewardDefinition PickOneExcluding(List<RunRewardDefinition> exclude)
        {
            var remaining = new List<RunRewardDefinition>();
            foreach (RunRewardDefinition def in SourcePool)
            {
                if (!exclude.Contains(def)) remaining.Add(def);
            }

            return remaining.Count == 0 ? null : remaining[Rng.Next(remaining.Count)];
        }

        private void PublishChanged()
        {
            var options = new RunRewardOptionViewState[_options.Count];
            for (int index = 0; index < _options.Count; index++)
            {
                RunRewardDefinition reward = _options[index];
                options[index] = new RunRewardOptionViewState(
                    index,
                    reward.DisplayName,
                    reward.Description,
                    reward.IconKey);
            }

            State = new RunRewardViewState(
                GameFlowSession.BuildSummary(),
                GameFlowSession.CurrentBattleGrantsArtifact,
                CanUseRewarded(_rerollAdReady) &&
                    !_rerollUsed &&
                    !_rewardClaimed,
                BuildRerollLabel(),
                GameFlowSession.CurrentBattleGrantsArtifact &&
                    CanUseRewarded(_extraRewardAdReady) &&
                    !_extraRewardUsed &&
                    !_rewardClaimed,
                BuildExtraRewardLabel(),
                CanUseRewarded(_rewardDoubleAdReady) &&
                    !_rewardDoubleUsed &&
                    !_rewardClaimed,
                BuildRewardDoubleLabel(),
                options);
            Changed?.Invoke(State);
        }

        private string BuildRerollLabel()
        {
            if (_rewardClaimed)
            {
                return "보상 선택 완료";
            }

            if (_rerollUsed)
            {
                return "리롤 사용 완료";
            }

            if (_adsRemoved)
            {
                return "광고 없이 리롤";
            }

            return _rerollAdReady
                ? "광고 보고 리롤"
                : "광고 준비 중...";
        }

        private string BuildExtraRewardLabel()
        {
            if (_rewardClaimed)
            {
                return "보상 선택 완료";
            }

            if (_extraRewardUsed)
            {
                return "추가 보상 사용 완료";
            }

            if (_adsRemoved)
            {
                return "광고 없이 추가 보상";
            }

            return _extraRewardAdReady
                ? "광고 보고 추가 보상"
                : "광고 준비 중...";
        }

        private string BuildRewardDoubleLabel()
        {
            if (_rewardClaimed)
            {
                return "보상 선택 완료";
            }

            if (_rewardDoubleEnabled)
            {
                return "보상 2배 적용됨";
            }

            if (_adsRemoved)
            {
                return "광고 없이 보상 2배";
            }

            return _rewardDoubleAdReady
                ? "광고 보고 보상 2배"
                : "광고 준비 중...";
        }

        private bool CanUseRewarded(bool adReady)
        {
            return _adsRemoved || adReady;
        }

        private static void ApplyReward(RunRewardDefinition reward)
        {
            switch (reward.Kind)
            {
                case RunRewardKind.Relic:
                    GameFlowSession.AddRelic(reward.Relic);
                    GameFlowSession.MarkRewardClaimed();
                    break;
                case RunRewardKind.Symbol:
                    GameFlowSession.ApplySymbolReward(reward.Symbol, reward.Amount);
                    break;
                default:
                    GameFlowSession.ApplyReward(reward.Type);
                    break;
            }
        }
    }

    public sealed class RunRewardViewState
    {
        public static readonly RunRewardViewState Empty =
            new(
                string.Empty,
                false,
                false,
                "광고 준비 중...",
                false,
                "광고 준비 중...",
                false,
                "광고 준비 중...",
                Array.Empty<RunRewardOptionViewState>());

        private readonly RunRewardOptionViewState[] _options;

        public RunRewardViewState(
            string summary,
            bool isBigReward,
            bool canReroll,
            string rerollLabel,
            bool canAddReward,
            string addRewardLabel,
            bool canDoubleReward,
            string doubleRewardLabel,
            IReadOnlyList<RunRewardOptionViewState> options)
        {
            Summary = summary ?? string.Empty;
            IsBigReward = isBigReward;
            CanReroll = canReroll;
            RerollLabel = rerollLabel ?? string.Empty;
            CanAddReward = canAddReward;
            AddRewardLabel = addRewardLabel ?? string.Empty;
            CanDoubleReward = canDoubleReward;
            DoubleRewardLabel = doubleRewardLabel ?? string.Empty;
            _options = Copy(options);
        }

        public string Summary { get; }

        public bool IsBigReward { get; }

        public bool CanReroll { get; }

        public string RerollLabel { get; }

        public bool CanAddReward { get; }

        public string AddRewardLabel { get; }

        public bool CanDoubleReward { get; }

        public string DoubleRewardLabel { get; }

        public IReadOnlyList<RunRewardOptionViewState> Options => _options;

        private static RunRewardOptionViewState[] Copy(
            IReadOnlyList<RunRewardOptionViewState> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<RunRewardOptionViewState>();
            }

            var copy = new RunRewardOptionViewState[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }
    }

    public readonly struct RunRewardOptionViewState
    {
        public RunRewardOptionViewState(
            int index,
            string title,
            string description,
            string iconKey)
        {
            Index = index;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            IconKey = iconKey ?? string.Empty;
        }

        public int Index { get; }

        public string Title { get; }

        public string Description { get; }

        public string IconKey { get; }
    }
}
