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
            if (optionIndex < 0 || optionIndex >= _options.Count)
            {
                return;
            }

            RunRewardDefinition reward = _options[optionIndex];

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

            RewardClaimed?.Invoke();
        }

        public void Refresh()
        {
            _optionCount = BaseOptionCount;
            RollOptions(_optionCount);
            PublishChanged();
        }

        public void RerollRewards()
        {
            RollOptions(_optionCount);
            PublishChanged();
        }

        public void AddExtraReward()
        {
            if (!GameFlowSession.CurrentBattleGrantsArtifact)
            {
                return;
            }

            RunRewardDefinition extra = PickOneExcluding(_options);
            if (extra == null)
            {
                return;
            }

            _options.Add(extra);
            _optionCount = _options.Count;
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
                options);
            Changed?.Invoke(State);
        }
    }

    public sealed class RunRewardViewState
    {
        public static readonly RunRewardViewState Empty =
            new(string.Empty, false, Array.Empty<RunRewardOptionViewState>());

        private readonly RunRewardOptionViewState[] _options;

        public RunRewardViewState(
            string summary,
            bool isBigReward,
            IReadOnlyList<RunRewardOptionViewState> options)
        {
            Summary = summary ?? string.Empty;
            IsBigReward = isBigReward;
            _options = Copy(options);
        }

        public string Summary { get; }

        public bool IsBigReward { get; }

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
