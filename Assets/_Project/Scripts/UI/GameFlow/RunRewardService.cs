using System;
using System.Collections.Generic;
using System.Linq;
using SlotRogue.Relics.Pool;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 보상 도메인 로직(등급별 풀 추첨 + 런 상태 적용)을 담당합니다(ADR-0020).
    /// RunRewardViewModel이 화면 상태만 갖도록, RNG/RunRewardCatalog/GameFlowSession 접근을 여기로 모읍니다.
    /// </summary>
    public interface IRunRewardService
    {
        /// <summary>현재(직전) 전투가 유물 보상을 주는가 (엘리트/보스).</summary>
        bool CurrentBattleGrantsArtifact { get; }

        /// <summary>
        /// 지금이 런 시작 직후의 '시작 유물 선택' 단계인지 여부.
        /// 시작 유물이 아직 정해지지 않았다면 true (전투 후 보상 단계가 아님).
        /// </summary>
        bool IsStarterSelection { get; }

        /// <summary>현재 진행 요약 텍스트.</summary>
        string BuildSummary();

        /// <summary>등급 풀에서 중복 없이 최대 count개를 추첨합니다.</summary>
        IReadOnlyList<RunRewardDefinition> RollOptions(int count);

        /// <summary>시작 유물 풀에서 중복 없이 최대 count개를 추첨합니다.</summary>
        IReadOnlyList<RunRewardDefinition> RollStarterOptions(int count);

        /// <summary>이미 제시된 보상을 제외하고 하나를 추첨합니다. 없으면 null.</summary>
        RunRewardDefinition PickOneExcluding(IReadOnlyList<RunRewardDefinition> exclude);

        /// <summary>선택한 보상을 런 상태에 적용합니다.</summary>
        void Apply(RunRewardDefinition reward);

        /// <summary>선택한 시작 유물을 런 상태에 적용합니다.</summary>
        void ApplyStarter(RunRewardDefinition reward);
    }

    /// <inheritdoc />
    public sealed class RunRewardService : IRunRewardService
    {
        private readonly Random _rng;

        public RunRewardService(Random rng = null)
        {
            _rng = rng ?? new Random();
        }

        private static IReadOnlyList<RunRewardDefinition> SourcePool =>
            RunRewardCatalog.ForTier(GameFlowSession.CurrentTier);

        public bool CurrentBattleGrantsArtifact =>
            GameFlowSession.CurrentBattleGrantsArtifact;

        public bool IsStarterSelection => !GameFlowSession.HasStarterRelic;

        public string BuildSummary() => RunSummaryPresenter.BuildRunSummary();

        public IReadOnlyList<RunRewardDefinition> RollOptions(int count)
        {
            return RollFrom(SourcePool, count);
        }

        public IReadOnlyList<RunRewardDefinition> RollStarterOptions(int count)
        {
            var starters = new List<RunRewardDefinition>();
            foreach (RelicDefinition relic in RelicCatalog.Starters)
            {
                starters.Add(new RunRewardDefinition(relic));
            }

            return RollFrom(starters, count);
        }

        private IReadOnlyList<RunRewardDefinition> RollFrom(
            IReadOnlyList<RunRewardDefinition> source,
            int count)
        {
            var pool = new List<RunRewardDefinition>(source);
            int take = Math.Min(count, pool.Count);
            var rolled = new List<RunRewardDefinition>(take);

            for (int i = 0; i < take; i++)
            {
                int index = _rng.Next(pool.Count);
                rolled.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return rolled;
        }

        public RunRewardDefinition PickOneExcluding(
            IReadOnlyList<RunRewardDefinition> exclude)
        {
            var remaining = new List<RunRewardDefinition>();
            foreach (RunRewardDefinition def in SourcePool)
            {
                if (exclude == null || !exclude.Contains(def))
                {
                    remaining.Add(def);
                }
            }

            return remaining.Count == 0 ? null : remaining[_rng.Next(remaining.Count)];
        }

        public void Apply(RunRewardDefinition reward)
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

        public void ApplyStarter(RunRewardDefinition reward)
        {
            GameFlowSession.SelectStarterRelic(reward?.Relic);
        }
    }
}
