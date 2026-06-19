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
                CreateEncounter("FIRST", EncounterTier.Normal, new[] { first }, weight: 1),
                CreateEncounter("SECOND", EncounterTier.Normal, new[] { second }, weight: 1));
            var selector = new EncounterSelector();
            var request = new EncounterSelectionRequest(table, EncounterTier.Normal, cycle: 0, runSeed: 1234, battleNumber: 7);

            EncounterSelection firstSelection = selector.Select(request);
            EncounterSelection secondSelection = selector.Select(request);

            Assert.That(secondSelection.Monsters[0].Definition, Is.SameAs(firstSelection.Monsters[0].Definition));
            Destroy(table, first, second);
        }

        [Test]
        public void Select_DifferentTier_ExcludesCandidate()
        {
            MonsterDefinition normal = CreateMonster();
            MonsterDefinition elite = CreateMonster();
            EncounterTable table = CreateTable(
                "TierTable",
                CreateEncounter("NORMAL", EncounterTier.Normal, new[] { normal }),
                CreateEncounter("ELITE", EncounterTier.Elite, new[] { elite }));
            var selector = new EncounterSelector();

            EncounterSelection selection = selector.Select(
                new EncounterSelectionRequest(table, EncounterTier.Elite, cycle: 0, runSeed: 1, battleNumber: 1));

            Assert.That(selection.Monsters[0].Definition, Is.SameAs(elite));
            Destroy(table, normal, elite);
        }

        [Test]
        public void Select_MinCycle_ExcludesFutureCandidate()
        {
            MonsterDefinition current = CreateMonster();
            MonsterDefinition future = CreateMonster();
            EncounterTable table = CreateTable(
                "MinCycleTable",
                CreateEncounter("CURRENT", EncounterTier.Normal, new[] { current }, minCycle: 0),
                CreateEncounter("FUTURE", EncounterTier.Normal, new[] { future }, minCycle: 2));
            var selector = new EncounterSelector();

            EncounterSelection selection = selector.Select(
                new EncounterSelectionRequest(table, EncounterTier.Normal, cycle: 1, runSeed: 1, battleNumber: 1));

            Assert.That(selection.Monsters[0].Definition, Is.SameAs(current));
            Destroy(table, current, future);
        }

        [Test]
        public void Select_MaxCycle_ExcludesExpiredCandidate()
        {
            MonsterDefinition expired = CreateMonster();
            MonsterDefinition current = CreateMonster();
            EncounterTable table = CreateTable(
                "MaxCycleTable",
                CreateEncounter("EXPIRED", EncounterTier.Normal, new[] { expired }, maxCycle: 1),
                CreateEncounter("CURRENT", EncounterTier.Normal, new[] { current }, maxCycle: 3));
            var selector = new EncounterSelector();

            EncounterSelection selection = selector.Select(
                new EncounterSelectionRequest(table, EncounterTier.Normal, cycle: 2, runSeed: 1, battleNumber: 1));

            Assert.That(selection.Monsters[0].Definition, Is.SameAs(current));
            Destroy(table, expired, current);
        }

        [Test]
        public void Select_UnlimitedMaxCycle_KeepsCandidate()
        {
            MonsterDefinition unlimited = CreateMonster();
            EncounterTable table = CreateTable(
                "UnlimitedCycleTable",
                CreateEncounter("UNLIMITED", EncounterTier.Normal, new[] { unlimited }, maxCycle: -1));
            var selector = new EncounterSelector();

            EncounterSelection selection = selector.Select(
                new EncounterSelectionRequest(table, EncounterTier.Normal, cycle: 99, runSeed: 1, battleNumber: 1));

            Assert.That(selection.Monsters[0].Definition, Is.SameAs(unlimited));
            Destroy(table, unlimited);
        }

        [Test]
        public void Select_PreservesMonsterDefinitionOrder()
        {
            MonsterDefinition first = CreateMonster();
            MonsterDefinition second = CreateMonster();
            MonsterDefinition third = CreateMonster();
            EncounterTable table = CreateTable(
                "OrderTable",
                CreateEncounter("THREE", EncounterTier.Normal, new[] { first, second, third }));
            var selector = new EncounterSelector();

            EncounterSelection selection = selector.Select(
                new EncounterSelectionRequest(table, EncounterTier.Normal, cycle: 0, runSeed: 1, battleNumber: 1));

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
                CreateEncounter("THREE", EncounterTier.Normal, new[] { first, second, third }));
            var selector = new EncounterSelector();

            EncounterSelection selection = selector.Select(
                new EncounterSelectionRequest(table, EncounterTier.Normal, cycle: 0, runSeed: 1, battleNumber: 1));

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
                CreateEncounter("ELITE", EncounterTier.Elite, new[] { elite }));
            var selector = new EncounterSelector();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => selector.Select(new EncounterSelectionRequest(
                    table,
                    EncounterTier.Normal,
                    cycle: 3,
                    runSeed: 1,
                    battleNumber: 12)));

            StringAssert.Contains("MissingCandidateTable", exception.Message);
            StringAssert.Contains("Normal", exception.Message);
            StringAssert.Contains("Cycle=3", exception.Message);
            StringAssert.Contains("BattleNumber=12", exception.Message);
            Destroy(table, elite);
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

        private static EncounterTable CreateTable(string name, params EncounterDefinition[] encounters)
        {
            EncounterTable table = ScriptableObject.CreateInstance<EncounterTable>();
            table.name = name;
            SetField(table, "_encounters", encounters);
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

        private static void Destroy(params UnityEngine.Object[] objects)
        {
            for (int index = 0; index < objects.Length; index++)
            {
                UnityEngine.Object.DestroyImmediate(objects[index]);
            }
        }
    }
}
