using System;
using System.Linq;
using NUnit.Framework;
using SlotRogue.Data.Combat;
using SlotRogue.UI.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class EncounterSelectionTests
    {
        [Test]
        public void EnemyFormationLayout_OneMonster_UsesCenterSlot()
        {
            Assert.That(EnemyFormationLayout.ResolveSlots(1), Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void EnemyFormationLayout_TwoMonsters_UsesSideSlots()
        {
            Assert.That(EnemyFormationLayout.ResolveSlots(2), Is.EqualTo(new[] { 0, 2 }));
        }

        [Test]
        public void EnemyFormationLayout_ThreeMonsters_UsesAllSlots()
        {
            Assert.That(EnemyFormationLayout.ResolveSlots(3), Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void EnemyFormationLayout_ZeroMonsters_Fails()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyFormationLayout.ResolveSlots(0));
        }

        [Test]
        public void EnemyFormationLayout_FourMonsters_Fails()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyFormationLayout.ResolveSlots(4));
        }

        [Test]
        public void EncounterSelection_NullMonsters_Fails()
        {
            Assert.Throws<ArgumentNullException>(() => new EncounterSelection(null));
        }

        [Test]
        public void EncounterSelection_EmptyMonsters_Fails()
        {
            Assert.Throws<ArgumentException>(() => new EncounterSelection(Array.Empty<SelectedEncounterMonster>()));
        }

        [Test]
        public void SelectedEncounterMonster_NullDefinition_Fails()
        {
            Assert.Throws<ArgumentNullException>(() => new SelectedEncounterMonster(null, formationSlot: 1));
        }

        [Test]
        public void EncounterSelection_PreservesMonsterOrderAndFormationSlots()
        {
            MonsterDefinition first = CreateMonster();
            MonsterDefinition second = CreateMonster();
            MonsterDefinition third = CreateMonster();
            int[] slots = EnemyFormationLayout.ResolveSlots(3).ToArray();

            var selection = new EncounterSelection(new[]
            {
                new SelectedEncounterMonster(first, slots[0]),
                new SelectedEncounterMonster(second, slots[1]),
                new SelectedEncounterMonster(third, slots[2]),
            });

            Assert.That(selection.Monsters[0].Definition, Is.SameAs(first));
            Assert.That(selection.Monsters[0].FormationSlot, Is.EqualTo(0));
            Assert.That(selection.Monsters[1].Definition, Is.SameAs(second));
            Assert.That(selection.Monsters[1].FormationSlot, Is.EqualTo(1));
            Assert.That(selection.Monsters[2].Definition, Is.SameAs(third));
            Assert.That(selection.Monsters[2].FormationSlot, Is.EqualTo(2));

            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
            UnityEngine.Object.DestroyImmediate(third);
        }

        private static MonsterDefinition CreateMonster()
        {
            return ScriptableObject.CreateInstance<MonsterDefinition>();
        }
    }
}
