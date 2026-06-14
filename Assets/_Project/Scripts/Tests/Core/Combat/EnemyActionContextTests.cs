using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Core.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class EnemyActionContextTests
    {
        [Test]
        public void Constructor_CopiesEnemyList()
        {
            CombatParticipant enemy0 = Enemy(100);
            CombatParticipant enemy1 = Enemy(101);
            var enemies = new[] { enemy0 };

            var context = new EnemyActionContext(enemy0, Player(), enemies, turnNumber: 3);

            enemies[0] = enemy1;

            Assert.That(context.Enemies[0], Is.SameAs(enemy0));
        }

        [Test]
        public void Enemies_ReturnsCopy()
        {
            CombatParticipant enemy0 = Enemy(100);
            var context = new EnemyActionContext(enemy0, Player(), new[] { enemy0 }, turnNumber: 3);

            IReadOnlyList<CombatParticipant> firstRead = context.Enemies;
            IReadOnlyList<CombatParticipant> secondRead = context.Enemies;

            Assert.That(firstRead, Is.Not.SameAs(secondRead));
            Assert.That(secondRead[0], Is.SameAs(enemy0));
        }

        private static CombatParticipant Player() =>
            new(maxHp: 50, currentHp: 50, shield: 0, new CombatParticipantId(1), CombatTeam.Player);

        private static CombatParticipant Enemy(int id) =>
            new(maxHp: 20, currentHp: 20, shield: 0, new CombatParticipantId(id), CombatTeam.Enemy);
    }
}
