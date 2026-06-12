using System;
using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public readonly struct EnemyUpcomingTurn
    {
        public EnemyUpcomingTurn(
            CombatParticipantId participantId,
            int turnIndex,
            IReadOnlyList<CombatEffect> actions)
        {
            ParticipantId = participantId;
            TurnIndex = turnIndex;
            Actions = actions ?? Array.Empty<CombatEffect>();
        }

        public CombatParticipantId ParticipantId { get; }

        public int TurnIndex { get; }

        public IReadOnlyList<CombatEffect> Actions { get; }
    }
}
