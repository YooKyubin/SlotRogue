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
        /// <summary>현재 전투 후 보상 화면에서 추가 보상 광고 슬롯을 허용하는지 여부.</summary>
        bool CurrentBattleGrantsArtifact { get; }

        /// <summary>
        /// 지금 보상 화면이 레거시 '시작 유물 선택' 단계인지 여부.
        /// 릴 잠금 프로토타입에서는 시작 유물을 지급하지 않으므로 기본 구현은 false입니다.
        /// </summary>
        bool IsStarterSelection { get; }

        /// <summary>현재 진행 요약 텍스트.</summary>
        string BuildSummary();

        /// <summary>등급 풀에서 중복 없이 최대 count개를 추첨합니다.</summary>
        IReadOnlyList<RunRewardDefinition> RollOptions(int count);

        /// <summary>레거시 시작 유물 풀에서 중복 없이 최대 count개를 추첨합니다.</summary>
        IReadOnlyList<RunRewardDefinition> RollStarterOptions(int count);

        /// <summary>이미 제시된 보상을 제외하고 하나를 추첨합니다. 없으면 null.</summary>
        RunRewardDefinition PickOneExcluding(IReadOnlyList<RunRewardDefinition> exclude);

        /// <summary>선택한 보상을 런 상태에 적용합니다.</summary>
        void Apply(RunRewardDefinition reward);

        /// <summary>레거시 시작 유물을 런 상태에 적용합니다.</summary>
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
            BuildAvailableSourcePool();

        public bool CurrentBattleGrantsArtifact =>
            false;

        public bool IsStarterSelection => false;

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
            int take = Math.Min(Math.Max(0, count), pool.Count);
            var rolled = new List<RunRewardDefinition>(take);

            for (int i = 0; i < take; i++)
            {
                int index = _rng.Next(pool.Count);
                rolled.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return rolled;
        }

        private static IReadOnlyList<RunRewardDefinition> BuildAvailableSourcePool()
        {
            IReadOnlyList<RunRewardDefinition> source =
                RunRewardCatalog.ForTier(GameFlowSession.CurrentTier);
            var pool = new List<RunRewardDefinition>(source.Count);
            for (int index = 0; index < source.Count; index++)
            {
                RunRewardDefinition reward = source[index];
                if (IsRewardAvailable(reward))
                {
                    pool.Add(reward);
                }
            }

            return pool;
        }

        private static bool IsRewardAvailable(RunRewardDefinition reward)
        {
            if (reward == null)
            {
                return false;
            }

            if (reward.Kind != RunRewardKind.Proposal)
            {
                return false;
            }

            if (reward.ProposalEffect == RunProposalEffectKind.None)
            {
                return false;
            }

            return reward.ProposalEffect != RunProposalEffectKind.RelicSlotCapacity ||
                GameFlowSession.RelicSlotCapacity < GameFlowSession.MaxRelicSlotCapacity;
        }

        private IReadOnlyList<RunRewardDefinition> RollFromExcluding(
            IReadOnlyList<RunRewardDefinition> source,
            int count,
            IReadOnlyList<RunRewardDefinition> exclude)
        {
            var pool = new List<RunRewardDefinition>();
            foreach (RunRewardDefinition def in source)
            {
                if (exclude == null || !exclude.Contains(def))
                {
                    pool.Add(def);
                }
            }

            return RollFrom(pool, count);
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
            if (reward == null)
            {
                return;
            }

            switch (reward.Kind)
            {
                case RunRewardKind.Relic:
                    GameFlowSession.AddRelic(reward.Relic);
                    GameFlowSession.MarkRewardClaimed();
                    break;
                case RunRewardKind.Symbol:
                    GameFlowSession.ApplySymbolReward(reward.Symbol, reward.Amount);
                    break;
                case RunRewardKind.Proposal:
                    ApplyProposal(reward);
                    break;
                default:
                    GameFlowSession.ApplyReward(reward.Type);
                    break;
            }
        }

        private static void ApplyProposal(RunRewardDefinition reward)
        {
            switch (reward.ProposalEffect)
            {
                case RunProposalEffectKind.SymbolWeight:
                    GameFlowSession.ApplySymbolWeightReward(reward.Symbols, reward.Amount);
                    break;
                case RunProposalEffectKind.SymbolBaseDamage:
                    GameFlowSession.ApplySymbolBaseDamageReward(reward.Symbols, reward.Amount);
                    break;
                case RunProposalEffectKind.RunCoins:
                    GameFlowSession.AddRunCoins(reward.Amount);
                    GameFlowSession.MarkRewardClaimed();
                    break;
                case RunProposalEffectKind.RelicSlotCapacity:
                    GameFlowSession.TryIncreaseRelicSlotCapacity(reward.Amount);
                    GameFlowSession.MarkRewardClaimed();
                    break;
                case RunProposalEffectKind.EngineEffect:
                    GameFlowSession.AddProposalSpec(RelicSpecCatalog.GetProposalById(reward.ProposalId));
                    GameFlowSession.MarkRewardClaimed();
                    break;
                default:
                    GameFlowSession.MarkRewardClaimed();
                    break;
            }
        }

        public void ApplyStarter(RunRewardDefinition reward)
        {
            GameFlowSession.SelectStarterRelic(reward?.Relic);
        }
    }
}
