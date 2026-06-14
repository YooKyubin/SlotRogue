using System;
using System.Collections.Generic;

namespace SlotRogue.Core.Combat
{
    public readonly struct EnemyActionContext
    {
        private readonly CombatParticipant[] _enemies;

        public EnemyActionContext(
            CombatParticipant self,
            CombatParticipant player,
            IReadOnlyList<CombatParticipant> enemies,
            int turnNumber)
        {
            Self = self ?? throw new ArgumentNullException(nameof(self));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            _enemies = CloneEnemies(enemies);
            TurnNumber = turnNumber;
        }

        public CombatParticipant Self { get; }

        public CombatParticipant Player { get; }

        public IReadOnlyList<CombatParticipant> Enemies => CloneEnemies(_enemies);

        public int TurnNumber { get; }

        private static CombatParticipant[] CloneEnemies(IReadOnlyList<CombatParticipant> enemies)
        {
            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            if (enemies.Count == 0)
            {
                return Array.Empty<CombatParticipant>();
            }

            var copy = new CombatParticipant[enemies.Count];
            for (int index = 0; index < enemies.Count; index++)
            {
                copy[index] = enemies[index] ?? throw new ArgumentNullException(nameof(enemies));
            }

            return copy;
        }
    }
}
