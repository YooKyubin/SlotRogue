using System;
using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public sealed class TurnResult
    {
        public TurnResult(IReadOnlyList<CombatEvent> events, BattleStateSnapshot finalState)
        {
            Events = events ?? Array.Empty<CombatEvent>();
            FinalState = finalState;
        }

        public IReadOnlyList<CombatEvent> Events { get; }

        public BattleStateSnapshot FinalState { get; }
    }
}
