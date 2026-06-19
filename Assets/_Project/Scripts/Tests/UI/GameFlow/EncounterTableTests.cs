using System.Reflection;
using NUnit.Framework;
using SlotRogue.Data.Combat;
using SlotRogue.Data.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class EncounterTableTests
    {
        [Test]
        public void EncounterDefinition_ValidDefinition_PassesValidation()
        {
            MonsterDefinition monster = CreateMonster();
            EncounterDefinition encounter = CreateEncounter(
                "E-NORMAL-MOON-RABBIT",
                EncounterTier.Normal,
                new[] { monster },
                weight: 1,
                minCycle: 0,
                maxCycle: -1);

            bool valid = encounter.TryValidate(out string error);

            Assert.That(valid, Is.True, error);
            Object.DestroyImmediate(monster);
        }

        [Test]
        public void EncounterDefinition_InvalidMonsterCount_FailsValidation()
        {
            MonsterDefinition first = CreateMonster();
            MonsterDefinition second = CreateMonster();
            MonsterDefinition third = CreateMonster();
            MonsterDefinition fourth = CreateMonster();
            EncounterDefinition encounter = CreateEncounter(
                "E-TOO-MANY",
                EncounterTier.Normal,
                new[] { first, second, third, fourth });

            bool valid = encounter.TryValidate(out string error);

            Assert.That(valid, Is.False);
            StringAssert.Contains("Monster count", error);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(third);
            Object.DestroyImmediate(fourth);
        }

        [Test]
        public void EncounterDefinition_InvalidCycleRange_FailsValidation()
        {
            MonsterDefinition monster = CreateMonster();
            EncounterDefinition encounter = CreateEncounter(
                "E-BAD-CYCLE",
                EncounterTier.Elite,
                new[] { monster },
                minCycle: 3,
                maxCycle: 2);

            bool valid = encounter.TryValidate(out string error);

            Assert.That(valid, Is.False);
            StringAssert.Contains("MaxCycle", error);
            Object.DestroyImmediate(monster);
        }

        [Test]
        public void EncounterTable_DuplicateIds_FailValidation()
        {
            MonsterDefinition monster = CreateMonster();
            EncounterTable table = CreateTable(
                CreateEncounter("E-DUPLICATE", EncounterTier.Normal, new[] { monster }),
                CreateEncounter("E-DUPLICATE", EncounterTier.Boss, new[] { monster }));

            bool valid = table.TryValidate(out string error);

            Assert.That(valid, Is.False);
            StringAssert.Contains("Duplicate encounter id", error);
            Object.DestroyImmediate(table);
            Object.DestroyImmediate(monster);
        }

        [Test]
        public void EncounterTable_EmptyId_FailsValidation()
        {
            MonsterDefinition monster = CreateMonster();
            EncounterTable table = CreateTable(
                CreateEncounter(string.Empty, EncounterTier.Normal, new[] { monster }));

            bool valid = table.TryValidate(out string error);

            Assert.That(valid, Is.False);
            StringAssert.Contains("Id cannot be empty", error);
            Object.DestroyImmediate(table);
            Object.DestroyImmediate(monster);
        }

        [Test]
        public void EncounterTable_NullEncounterArray_FailsValidation()
        {
            EncounterTable table = CreateTable(null);

            bool valid = table.TryValidate(out string error);

            Assert.That(valid, Is.False);
            StringAssert.Contains("Encounter array cannot be null", error);
            Object.DestroyImmediate(table);
        }

        [Test]
        public void EncounterDefinition_DoesNotExposeFormationSlot()
        {
            Assert.That(
                typeof(EncounterDefinition).GetProperty("FormationSlot"),
                Is.Null);
        }

        private static MonsterDefinition CreateMonster()
        {
            return ScriptableObject.CreateInstance<MonsterDefinition>();
        }

        private static EncounterTable CreateTable(params EncounterDefinition[] encounters)
        {
            EncounterTable table = ScriptableObject.CreateInstance<EncounterTable>();
            FieldInfo field = typeof(EncounterTable).GetField(
                "_encounters",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(table, encounters);
            return table;
        }

        private static EncounterDefinition CreateEncounter(
            string id,
            EncounterTier tier,
            MonsterDefinition[] monsters,
            int weight = 1,
            int minCycle = 0,
            int maxCycle = -1)
        {
            var encounter = new EncounterDefinition();
            SetField(encounter, "_id", id);
            SetField(encounter, "_tier", tier);
            SetField(encounter, "_monsters", monsters);
            SetField(encounter, "_weight", weight);
            SetField(encounter, "_minCycle", minCycle);
            SetField(encounter, "_maxCycle", maxCycle);
            return encounter;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
