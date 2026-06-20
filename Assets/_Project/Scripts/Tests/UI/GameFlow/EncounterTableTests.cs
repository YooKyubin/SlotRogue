using System;
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
                weight: 1);

            bool valid = encounter.TryValidate(out string error);

            Assert.That(valid, Is.True, error);
            UnityEngine.Object.DestroyImmediate(monster);
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
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
            UnityEngine.Object.DestroyImmediate(third);
            UnityEngine.Object.DestroyImmediate(fourth);
        }

        [Test]
        public void EncounterDefinition_DoesNotExposeCycleRange()
        {
            Assert.That(typeof(EncounterDefinition).GetProperty("MinCycle"), Is.Null);
            Assert.That(typeof(EncounterDefinition).GetProperty("MaxCycle"), Is.Null);
        }

        [Test]
        public void EncounterTable_ThemeGroupCount_ReturnsGroupLength()
        {
            MonsterDefinition monster = CreateMonster();
            EncounterTable table = CreateTable(
                Group(CreateEncounter("E-A", EncounterTier.Normal, new[] { monster })),
                Group(CreateEncounter("E-B", EncounterTier.Elite, new[] { monster })));

            Assert.That(table.ThemeGroupCount, Is.EqualTo(2));
            UnityEngine.Object.DestroyImmediate(table);
            UnityEngine.Object.DestroyImmediate(monster);
        }

        [Test]
        public void EncounterTable_GetEncounters_ReturnsRequestedThemeGroup()
        {
            MonsterDefinition monster = CreateMonster();
            EncounterDefinition first = CreateEncounter("E-A", EncounterTier.Normal, new[] { monster });
            EncounterDefinition second = CreateEncounter("E-B", EncounterTier.Normal, new[] { monster });
            EncounterTable table = CreateTable(Group(first), Group(second));

            Assert.That(table.GetEncounters(1)[0], Is.SameAs(second));
            UnityEngine.Object.DestroyImmediate(table);
            UnityEngine.Object.DestroyImmediate(monster);
        }

        [Test]
        public void EncounterTable_GetEncounters_InvalidIndex_Fails()
        {
            MonsterDefinition monster = CreateMonster();
            EncounterTable table = CreateTable(
                Group(CreateEncounter("E-A", EncounterTier.Normal, new[] { monster })));

            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetEncounters(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetEncounters(1));
            UnityEngine.Object.DestroyImmediate(table);
            UnityEngine.Object.DestroyImmediate(monster);
        }

        [Test]
        public void EncounterTable_DuplicateIdsAcrossThemeGroups_FailValidation()
        {
            MonsterDefinition monster = CreateMonster();
            EncounterTable table = CreateTable(
                Group(CreateEncounter("E-DUPLICATE", EncounterTier.Normal, new[] { monster })),
                Group(CreateEncounter("E-DUPLICATE", EncounterTier.Boss, new[] { monster })));

            bool valid = table.TryValidate(out string error);

            Assert.That(valid, Is.False);
            StringAssert.Contains("Duplicate encounter id", error);
            UnityEngine.Object.DestroyImmediate(table);
            UnityEngine.Object.DestroyImmediate(monster);
        }

        [Test]
        public void EncounterTable_EmptyId_FailsValidation()
        {
            MonsterDefinition monster = CreateMonster();
            EncounterTable table = CreateTable(
                Group(CreateEncounter(string.Empty, EncounterTier.Normal, new[] { monster })));

            bool valid = table.TryValidate(out string error);

            Assert.That(valid, Is.False);
            StringAssert.Contains("Id cannot be empty", error);
            UnityEngine.Object.DestroyImmediate(table);
            UnityEngine.Object.DestroyImmediate(monster);
        }

        [Test]
        public void EncounterTable_NullThemeGroupArray_FailsValidation()
        {
            EncounterTable table = ScriptableObject.CreateInstance<EncounterTable>();

            bool valid = table.TryValidate(out string error);

            Assert.That(valid, Is.False);
            StringAssert.Contains("ThemeGroup array cannot be null", error);
            UnityEngine.Object.DestroyImmediate(table);
        }

        [Test]
        public void EncounterTable_EmptyThemeGroup_FailsValidation()
        {
            EncounterTable table = CreateTable(Group());

            bool valid = table.TryValidate(out string error);

            Assert.That(valid, Is.False);
            StringAssert.Contains("At least one EncounterDefinition", error);
            UnityEngine.Object.DestroyImmediate(table);
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

        private static EncounterDefinition[] Group(params EncounterDefinition[] encounters)
        {
            return encounters;
        }

        private static EncounterTable CreateTable(params EncounterDefinition[][] groups)
        {
            EncounterTable table = ScriptableObject.CreateInstance<EncounterTable>();
            Type groupType = typeof(EncounterTable).GetNestedType(
                "ThemeGroup",
                BindingFlags.NonPublic);
            Array themeGroups = Array.CreateInstance(groupType, groups.Length);
            FieldInfo encountersField = groupType.GetField(
                "_encounters",
                BindingFlags.Instance | BindingFlags.NonPublic);

            for (int index = 0; index < groups.Length; index++)
            {
                object group = Activator.CreateInstance(groupType, nonPublic: true);
                encountersField.SetValue(group, groups[index]);
                themeGroups.SetValue(group, index);
            }

            FieldInfo field = typeof(EncounterTable).GetField(
                "_themeGroups",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(table, themeGroups);
            return table;
        }

        private static EncounterDefinition CreateEncounter(
            string id,
            EncounterTier tier,
            MonsterDefinition[] monsters,
            int weight = 1)
        {
            var encounter = new EncounterDefinition();
            SetField(encounter, "_id", id);
            SetField(encounter, "_tier", tier);
            SetField(encounter, "_monsters", monsters);
            SetField(encounter, "_weight", weight);
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
