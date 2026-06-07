using System.Collections.Generic;
using SlotRogue.Relics.Data;
using UnityEngine;

namespace SlotRogue.Relics
{
    /// <summary>
    /// 플레이어가 보유한 유물 목록을 관리하고, 특정 타이밍에 조건을 검사해 효과를 적용한다.
    /// 전투 흐름은 각 시점에 <see cref="Execute"/>만 호출하면 된다.
    /// </summary>
    public sealed class RelicSystem
    {
        private readonly List<RelicDataSO> _relics = new List<RelicDataSO>();
        private readonly RelicConditionChecker _checker;
        private readonly RelicEffectExecutor _executor;

        public RelicSystem(RelicConditionChecker checker = null, RelicEffectExecutor executor = null)
        {
            _checker = checker ?? new RelicConditionChecker();
            _executor = executor ?? new RelicEffectExecutor();
        }

        public IReadOnlyList<RelicDataSO> Relics => _relics;
        public RelicEffectExecutor Executor => _executor;
        public RelicConditionChecker Checker => _checker;

        public void AddRelic(RelicDataSO relic)
        {
            if (relic != null && !_relics.Contains(relic))
            {
                _relics.Add(relic);
            }
        }

        public void RemoveRelic(RelicDataSO relic)
        {
            _relics.Remove(relic);
        }

        public void SetRelics(IEnumerable<RelicDataSO> relics)
        {
            _relics.Clear();
            if (relics == null)
            {
                return;
            }

            foreach (RelicDataSO relic in relics)
            {
                AddRelic(relic);
            }
        }

        public void Clear()
        {
            _relics.Clear();
        }

        /// <summary>
        /// 지정 타이밍에 발동 가능한 모든 유물의 조건을 검사하고 효과를 적용한다.
        /// </summary>
        public void Execute(RelicTriggerTiming timing, RelicContext context)
        {
            if (context == null)
            {
                return;
            }

            context.CurrentTiming = timing;

            for (int index = 0; index < _relics.Count; index++)
            {
                RelicDataSO relic = _relics[index];
                if (relic == null || relic.TriggerTiming != timing)
                {
                    continue;
                }

                if (!_checker.IsMet(relic.Condition, context))
                {
                    continue;
                }

                int repeat = ResolveRepeatCount(relic, context);
                if (repeat <= 0)
                {
                    continue;
                }

                IReadOnlyList<RelicEffectData> effects = relic.Effects;
                for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                {
                    _executor.Apply(effects[effectIndex], repeat, context);
                }

                Debug.Log($"[Relic] {relic.RelicName} 발동 (x{repeat}) @ {timing}");
            }
        }

        private int ResolveRepeatCount(RelicDataSO relic, RelicContext context)
        {
            switch (relic.ApplyMode)
            {
                case RelicApplyMode.Once:
                    return 1;
                case RelicApplyMode.PerAllPattern:
                    return context.Patterns?.Count ?? 0;
                case RelicApplyMode.PerMatchedPattern:
                    return _checker.GetMatchedPatternCount(relic.Condition, context);
                case RelicApplyMode.PerStack:
                    return Mathf.Max(1, context.EnemyPoisonStacks);
                default:
                    return 1;
            }
        }
    }
}
