using System;
using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class MonsterTurnSchedule
    {
        private readonly IReadOnlyList<IReadOnlyList<CombatEffect>> _turns;
        private int _upcomingTurnIndex;

        public MonsterTurnSchedule(IReadOnlyList<CombatEffect> singleTurn)
            : this(WrapSingleTurn(singleTurn))
        {
        }

        public MonsterTurnSchedule(IReadOnlyList<IReadOnlyList<CombatEffect>> turns)
        {
            if (turns == null || turns.Count == 0)
            {
                _turns = new[] { Array.Empty<CombatEffect>() };
            }
            else
            {
                _turns = turns;
            }

            _upcomingTurnIndex = 0;
        }

        public MonsterTurnSchedule(params CombatEffect[][] turns)
            : this(WrapTurnArrays(turns))
        {
        }

        public int TurnCount => _turns.Count;

        public int UpcomingTurnIndex => _upcomingTurnIndex;

        public IReadOnlyList<CombatEffect> UpcomingActions =>
            _turns[_upcomingTurnIndex % _turns.Count];

        public void Reset()
        {
            _upcomingTurnIndex = 0;
        }

        public IReadOnlyList<CombatEffect> ConsumeUpcomingTurn()
        {
            IReadOnlyList<CombatEffect> actions = UpcomingActions;
            _upcomingTurnIndex = (_upcomingTurnIndex + 1) % _turns.Count;
            return actions;
        }

        private static IReadOnlyList<IReadOnlyList<CombatEffect>> WrapSingleTurn(
            IReadOnlyList<CombatEffect> singleTurn)
        {
            return new[] { singleTurn ?? Array.Empty<CombatEffect>() };
        }

        private static IReadOnlyList<IReadOnlyList<CombatEffect>> WrapTurnArrays(CombatEffect[][] turns)
        {
            if (turns == null || turns.Length == 0)
            {
                return new[] { Array.Empty<CombatEffect>() };
            }

            var wrapped = new IReadOnlyList<CombatEffect>[turns.Length];
            for (int index = 0; index < turns.Length; index++)
            {
                wrapped[index] = turns[index] ?? Array.Empty<CombatEffect>();
            }

            return wrapped;
        }
    }
}
