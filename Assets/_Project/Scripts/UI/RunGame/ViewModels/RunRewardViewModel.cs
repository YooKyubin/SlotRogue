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
        /// <summary>기본 제시 개수(택N). 일반/큰 보상 모두 기본 3택.</summary>
        private const int BaseOptionCount = 3;

        private static readonly Random Rng = new();

        private readonly List<RunRewardDefinition> _options = new();
        private int _optionCount = BaseOptionCount;

        // ── 상태 ────────────────────────────────────────────────────────

        public IReadOnlyList<RunRewardDefinition> Rewards => _options;
        public string Summary { get; private set; }

        /// <summary>현재 전투가 엘리트/보스인가 (큰 보상 화면 여부).</summary>
        public bool IsBigReward => GameFlowSession.CurrentBattleGrantsArtifact;

        /// <summary>현재 등급에 맞는 보상 풀.</summary>
        private IReadOnlyList<RunRewardDefinition> SourcePool =>
            IsBigReward ? RunRewardCatalog.BigRewards : RunRewardCatalog.NormalRewards;

        // ── 이벤트 ──────────────────────────────────────────────────────

        /// <summary>보상이 선택되어 다음 전투로 진행해야 할 때 발행합니다.</summary>
        public event Action RewardClaimed;

        /// <summary>보상 목록/요약이 갱신되어 View를 다시 렌더링해야 할 때 발행합니다.</summary>
        public event Action Changed;

        // ── 생성 ────────────────────────────────────────────────────────

        public RunRewardViewModel()
        {
            Refresh();
        }

        // ── 커맨드 ──────────────────────────────────────────────────────

        /// <summary>View가 보상 버튼 클릭 시 호출합니다.</summary>
        public void ClaimReward(RunRewardDefinition reward)
        {
            if (reward == null) return;

            if (reward.Kind == RunRewardKind.Symbol)
            {
                GameFlowSession.ApplySymbolReward(reward.Symbol, reward.Amount);
            }
            else
            {
                GameFlowSession.ApplyReward(reward.Type);
            }

            RewardClaimed?.Invoke();
        }

        /// <summary>화면 진입 시 최신 데이터로 갱신하고 보상을 새로 뽑습니다.</summary>
        public void Refresh()
        {
            Summary = GameFlowSession.BuildSummary();
            _optionCount = BaseOptionCount;
            RollOptions(_optionCount);
        }

        // ── 광고 스텁 ────────────────────────────────────────────────────
        // SDK 미설치 상태라 광고 없이 즉시 동작합니다. 추후 RewardedAd 성공 콜백에서 호출하도록 교체.

        /// <summary>[광고] 보상을 모두 다시 뽑습니다(개수 유지).</summary>
        public void RerollRewards()
        {
            RollOptions(_optionCount);
            Changed?.Invoke();
        }

        /// <summary>[광고] 큰 보상에서 선택지를 1개 추가합니다. (엘리트/보스 전용)</summary>
        public void AddExtraReward()
        {
            if (!IsBigReward) return;

            RunRewardDefinition extra = PickOneExcluding(_options);
            if (extra == null) return; // 풀 소진

            _options.Add(extra);
            _optionCount = _options.Count;
            Changed?.Invoke();
        }

        // ── 내부 ────────────────────────────────────────────────────────

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
    }
}
