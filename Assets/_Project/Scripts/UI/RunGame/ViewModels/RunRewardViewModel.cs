using System;
using System.Collections.Generic;
using R3;
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
        private const string StarterTitle = "시작 유물 선택";
        private const string RewardTitle = "보상 선택";

        private readonly IRunRewardService _service;
        private readonly List<RunRewardDefinition> _options = new();
        private int _optionCount = BaseOptionCount;
        private bool _isStarterSelection;
        private bool _rerollAdReady;
        private bool _extraRewardAdReady;
        private bool _rewardDoubleAdReady;
        private bool _adsRemoved;
        private bool _rerollUsed;
        private bool _extraRewardUsed;
        private bool _rewardDoubleUsed;
        private bool _rewardDoubleEnabled;
        private bool _rewardClaimed;
        private bool _hasRefreshed;

        private readonly ReactiveProperty<RunRewardViewState> _state =
            new(RunRewardViewState.Empty);

        public RunRewardViewModel(IRunRewardService service = null)
        {
            _service = service ?? new RunRewardService();
        }

        public event Action RewardClaimed;

        public ReadOnlyReactiveProperty<RunRewardViewState> State => _state;

        /// <summary>
        /// 직전 <see cref="Refresh"/> 시점 기준으로, 지금 화면이 '시작 유물 선택' 단계인지 여부.
        /// 흐름 제어자가 수령 후 다음 처리(첫 전투 진입 vs 다음 전투 진행)를 분기하는 데 씁니다.
        /// </summary>
        public bool IsStarterSelection => _isStarterSelection;

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

            if (_isStarterSelection)
            {
                _service.ApplyStarter(reward);
            }
            else
            {
                _service.Apply(reward);

                if (_rewardDoubleEnabled)
                {
                    _service.Apply(reward);
                }
            }

            PublishChanged();
            RewardClaimed?.Invoke();
        }

        public void Refresh()
        {
            _hasRefreshed = true;
            _isStarterSelection = _service.IsStarterSelection;
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
            if (!_hasRefreshed)
            {
                return;
            }

            PublishChanged();
        }

        public void ApplyRewardedExtraReward()
        {
            if (_extraRewardUsed ||
                _rewardClaimed ||
                !_service.CurrentBattleGrantsArtifact)
            {
                return;
            }

            RunRewardDefinition extra = _service.PickOneExcluding(_options);
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
            _options.AddRange(
                _isStarterSelection
                    ? _service.RollStarterOptions(count)
                    : _service.RollOptions(count));
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
                    reward.Rarity);
            }

            _state.Value = new RunRewardViewState(
                _isStarterSelection ? StarterTitle : RewardTitle,
                _service.BuildSummary(),
                _service.CurrentBattleGrantsArtifact,
                CanUseRewarded(_rerollAdReady) &&
                    !_rerollUsed &&
                    !_rewardClaimed,
                BuildRerollLabel(),
                !_isStarterSelection &&
                    _service.CurrentBattleGrantsArtifact &&
                    CanUseRewarded(_extraRewardAdReady) &&
                    !_extraRewardUsed &&
                    !_rewardClaimed,
                BuildExtraRewardLabel(),
                !_isStarterSelection &&
                    CanUseRewarded(_rewardDoubleAdReady) &&
                    !_rewardDoubleUsed &&
                    !_rewardClaimed,
                BuildRewardDoubleLabel(),
                options);
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
    }

    public sealed class RunRewardViewState
    {
        public static readonly RunRewardViewState Empty =
            new(
                string.Empty,
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
            string title,
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
            Title = title ?? string.Empty;
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

        public string Title { get; }

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
            SlotRogue.UI.GameFlow.RewardRarity rarity = SlotRogue.UI.GameFlow.RewardRarity.Common)
        {
            Index = index;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Rarity = rarity;
        }

        public int Index { get; }

        public string Title { get; }

        public string Description { get; }

        public SlotRogue.UI.GameFlow.RewardRarity Rarity { get; }
    }
}
