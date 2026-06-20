using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SlotRogue.Data.Combat;
using SlotRogue.Data.GameFlow;
using SlotRogue.UI.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class EncounterSelectorTests
    {
        [Test]
        public void Select_SameSeedAndBattleNumber_ReturnsSameResult()
        {
            MonsterDefinition first = CreateMonster();
            MonsterDefinition second = CreateMonster();
            EncounterTable table = CreateTable(
                "DeterministicTable",
                Group(
                    CreateEncounter("FIRST", EncounterTier.Normal, new[] { first }, weight: 1),
                    CreateEncounter("SECOND", EncounterTier.Normal, new[] { second }, weight: 1)));
            var selector = new EncounterSelector();
            var request = new EncounterSelectionRequest(
                table,
                EncounterTier.Normal,
                themeGroupIndex: 0,
                runSeed: 1234,
                battleNumber: 7);

            EncounterSelection firstSelection = selector.Select(request);
            EncounterSelection secondSelection = selector.Select(request);

            Assert.That(secondSelection.Monsters[0].Definition, Is.SameAs(firstSelection.Monsters[0].Definition));
            Destroy(table, first, second);
        }

        [Test]
        public void Select_UsesOnlyRequestedThemeGroup()
        {
            MonsterDefinition firstGroupMonster = CreateMonster();
            MonsterDefinition secondGroupMonster = CreateMonster();
            EncounterTable table = CreateTable(
                "ThemeGroupTable",
                Group(CreateEncounter("GROUP-0", EncounterTier.Normal, new[] { firstGroupMonster })),
                Group(CreateEncounter("GROUP-1", EncounterTier.Normal, new[] { secondGroupMonster })));
            var selector = new EncounterSelector();

            EncounterSelection selection = selector.Select(new EncounterSelectionRequest(
                table,
                EncounterTier.Normal,
                themeGroupIndex: 1,
                runSeed: 1,
                battleNumber: 1));

            Assert.That(selection.Monsters[0].Definition, Is.SameAs(secondGroupMonster));
            Destroy(table, firstGroupMonster, secondGroupMonster);
        }

        [Test]
        public void Select_DifferentTier_ExcludesCandidateInsideThemeGroup()
        {
            MonsterDefinition normal = CreateMonster();
            MonsterDefinition elite = CreateMonster();
            EncounterTable table = CreateTable(
                "TierTable",
                Group(
                    CreateEncounter("NORMAL", EncounterTier.Normal, new[] { normal }),
                    CreateEncounter("ELITE", EncounterTier.Elite, new[] { elite })));
            var selector = new EncounterSelector();

            EncounterSelection selection = selector.Select(
                new EncounterSelectionRequest(
                    table,
                    EncounterTier.Elite,
                    themeGroupIndex: 0,
                    runSeed: 1,
                    battleNumber: 1));

            Assert.That(selection.Monsters[0].Definition, Is.SameAs(elite));
            Destroy(table, normal, elite);
        }

        [Test]
        public void Select_PreservesMonsterDefinitionOrder()
        {
            MonsterDefinition first = CreateMonster();
            MonsterDefinition second = CreateMonster();
            MonsterDefinition third = CreateMonster();
            EncounterTable table = CreateTable(
                "OrderTable",
                Group(CreateEncounter("THREE", EncounterTier.Normal, new[] { first, second, third })));
            var selector = new EncounterSelector();

            EncounterSelection selection = selector.Select(
                new EncounterSelectionRequest(
                    table,
                    EncounterTier.Normal,
                    themeGroupIndex: 0,
                    runSeed: 1,
                    battleNumber: 1));

            Assert.That(selection.Monsters[0].Definition, Is.SameAs(first));
            Assert.That(selection.Monsters[1].Definition, Is.SameAs(second));
            Assert.That(selection.Monsters[2].Definition, Is.SameAs(third));
            Destroy(table, first, second, third);
        }

        [Test]
        public void Select_AssignsFormationSlotsByMonsterOrder()
        {
            MonsterDefinition first = CreateMonster();
            MonsterDefinition second = CreateMonster();
            MonsterDefinition third = CreateMonster();
            EncounterTable table = CreateTable(
                "FormationTable",
                Group(CreateEncounter("THREE", EncounterTier.Normal, new[] { first, second, third })));
            var selector = new EncounterSelector();

            EncounterSelection selection = selector.Select(
                new EncounterSelectionRequest(
                    table,
                    EncounterTier.Normal,
                    themeGroupIndex: 0,
                    runSeed: 1,
                    battleNumber: 1));

            Assert.That(selection.Monsters[0].FormationSlot, Is.EqualTo(0));
            Assert.That(selection.Monsters[1].FormationSlot, Is.EqualTo(1));
            Assert.That(selection.Monsters[2].FormationSlot, Is.EqualTo(2));
            Destroy(table, first, second, third);
        }

        [Test]
        public void Select_NoCandidates_FailsWithRequestContext()
        {
            MonsterDefinition elite = CreateMonster();
            EncounterTable table = CreateTable(
                "MissingCandidateTable",
                Group(CreateEncounter("ELITE", EncounterTier.Elite, new[] { elite })));
            var selector = new EncounterSelector();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => selector.Select(new EncounterSelectionRequest(
                    table,
                    EncounterTier.Normal,
                    themeGroupIndex: 0,
                    runSeed: 1,
                    battleNumber: 12)));

            StringAssert.Contains("MissingCandidateTable", exception.Message);
            StringAssert.Contains("Normal", exception.Message);
            StringAssert.Contains("ThemeGroupIndex=0", exception.Message);
            StringAssert.Contains("BattleNumber=12", exception.Message);
            Destroy(table, elite);
        }

        [Test]
        public void Select_InvalidThemeGroupIndex_Fails()
        {
            MonsterDefinition monster = CreateMonster();
            EncounterTable table = CreateTable(
                "InvalidGroupTable",
                Group(CreateEncounter("NORMAL", EncounterTier.Normal, new[] { monster })));
            var selector = new EncounterSelector();

            Assert.Throws<ArgumentOutOfRangeException>(() => selector.Select(new EncounterSelectionRequest(
                table,
                EncounterTier.Normal,
                themeGroupIndex: 1,
                runSeed: 1,
                battleNumber: 1)));
            Destroy(table, monster);
        }

        [Test]
        public void SelectWeightedCandidate_UsesWeightBoundaries()
        {
            MonsterDefinition firstMonster = CreateMonster();
            MonsterDefinition secondMonster = CreateMonster();
            EncounterDefinition first = CreateEncounter("FIRST", EncounterTier.Normal, new[] { firstMonster }, weight: 2);
            EncounterDefinition second = CreateEncounter("SECOND", EncounterTier.Normal, new[] { secondMonster }, weight: 3);
            var candidates = new[] { first, second };

            Assert.That(InvokeSelectWeightedCandidate(candidates, roll: 0), Is.SameAs(first));
            Assert.That(InvokeSelectWeightedCandidate(candidates, roll: 1), Is.SameAs(first));
            Assert.That(InvokeSelectWeightedCandidate(candidates, roll: 2), Is.SameAs(second));
            Assert.That(InvokeSelectWeightedCandidate(candidates, roll: 4), Is.SameAs(second));
            Assert.Throws<TargetInvocationException>(() => InvokeSelectWeightedCandidate(candidates, roll: 5));

            Destroy(firstMonster, secondMonster);
        }

        [Test]
        public void ThemeIndexSelector_SameInput_ReturnsSameIndex()
        {
            var selector = new EncounterThemeIndexSelector();

            int first = selector.Select(runSeed: 1234, themeSectionIndex: 2, themeGroupCount: 4);
            int second = selector.Select(runSeed: 1234, themeSectionIndex: 2, themeGroupCount: 4);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void ThemeIndexSelector_ReturnsIndexInsideThemeGroupCount()
        {
            var selector = new EncounterThemeIndexSelector();

            for (int themeSectionIndex = 0; themeSectionIndex < 32; themeSectionIndex++)
            {
                int result = selector.Select(runSeed: 77, themeSectionIndex, themeGroupCount: 5);

                Assert.That(result, Is.GreaterThanOrEqualTo(0));
                Assert.That(result, Is.LessThan(5));
            }
        }

        [Test]
        public void ThemeIndexSelector_SingleThemeGroup_ReturnsZero()
        {
            var selector = new EncounterThemeIndexSelector();

            Assert.That(selector.Select(runSeed: 1, themeSectionIndex: 0, themeGroupCount: 1), Is.EqualTo(0));
            Assert.That(selector.Select(runSeed: 999, themeSectionIndex: 20, themeGroupCount: 1), Is.EqualTo(0));
        }

        [Test]
        public void ThemeIndexSelector_DifferentThemeSectionIndex_CanReturnDifferentResult()
        {
            var selector = new EncounterThemeIndexSelector();
            bool foundDifferent = false;
            int first = selector.Select(runSeed: 1234, themeSectionIndex: 0, themeGroupCount: 4);

            for (int themeSectionIndex = 1; themeSectionIndex < 16; themeSectionIndex++)
            {
                int next = selector.Select(runSeed: 1234, themeSectionIndex, themeGroupCount: 4);
                if (next != first)
                {
                    foundDifferent = true;
                    break;
                }
            }

            Assert.That(foundDifferent, Is.True);
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        public void ThemeIndexSelector_AdjacentThemeSections_DoNotRepeatTheme(int themeGroupCount)
        {
            var selector = new EncounterThemeIndexSelector();
            int previous = selector.Select(runSeed: 1234, themeSectionIndex: 0, themeGroupCount);

            for (int themeSectionIndex = 1; themeSectionIndex < 32; themeSectionIndex++)
            {
                int current = selector.Select(runSeed: 1234, themeSectionIndex, themeGroupCount);

                Assert.That(current, Is.Not.EqualTo(previous));
                previous = current;
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ThemeIndexSelector_InvalidThemeGroupCount_Fails(int themeGroupCount)
        {
            var selector = new EncounterThemeIndexSelector();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                selector.Select(runSeed: 1, themeSectionIndex: 0, themeGroupCount));
        }

        [Test]
        public void ThemeIndexSelector_InvalidThemeSectionIndex_Fails()
        {
            var selector = new EncounterThemeIndexSelector();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                selector.Select(runSeed: 1, themeSectionIndex: -1, themeGroupCount: 2));
        }

        private static EncounterDefinition InvokeSelectWeightedCandidate(
            IReadOnlyList<EncounterDefinition> candidates,
            int roll)
        {
            MethodInfo method = typeof(EncounterSelector).GetMethod(
                "SelectWeightedCandidate",
                BindingFlags.Static | BindingFlags.NonPublic);

            return (EncounterDefinition)method.Invoke(null, new object[] { candidates, roll });
        }

        private static MonsterDefinition CreateMonster()
        {
            return ScriptableObject.CreateInstance<MonsterDefinition>();
        }

        private static EncounterDefinition[] Group(params EncounterDefinition[] encounters)
        {
            return encounters;
        }

        private static EncounterTable CreateTable(string name, params EncounterDefinition[][] groups)
        {
            EncounterTable table = ScriptableObject.CreateInstance<EncounterTable>();
            table.name = name;
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

        private static void Destroy(params UnityEngine.Object[] objects)
        {
            for (int index = 0; index < objects.Length; index++)
            {
                UnityEngine.Object.DestroyImmediate(objects[index]);
            }
        }
    }
}
