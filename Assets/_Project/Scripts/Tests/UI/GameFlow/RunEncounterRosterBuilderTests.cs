using System;
using NUnit.Framework;
using SlotRogue.Data.GameFlow;
using SlotRogue.UI.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class RunEncounterRosterBuilderTests
    {
        [Test]
        public void Build_WithEncounterEntries_UsesEntryCountAndFormationSlots()
        {
            var encounter = ScriptableObject.CreateInstance<RunEncounterDefinition>();
            encounter.entries = new[]
            {
                new RunEncounterEntry { formationSlot = 0 },
                new RunEncounterEntry { formationSlot = 2 },
            };

            var node = ScriptableObject.CreateInstance<RunMapNodeDefinition>();
            node.nodeId = "test-duo";
            node.nodeType = RunMapNodeType.Monster;
            node.floor = 2;
            node.encounter = encounter;

            RunEncounterRoster roster = RunEncounterRosterBuilder.Build(node, floor: 2, inspectorFallback: null);

            Assert.That(roster.Enemies, Has.Length.EqualTo(2));
            Assert.That(roster.FormationSlots[0], Is.EqualTo(0));
            Assert.That(roster.FormationSlots[1], Is.EqualTo(2));
        }

        [Test]
        public void Build_WithoutEncounter_ThrowsInvalidOperationException()
        {
            var node = ScriptableObject.CreateInstance<RunMapNodeDefinition>();
            node.nodeId = "test-single";
            node.nodeType = RunMapNodeType.Monster;
            node.floor = 1;

            Assert.Throws<InvalidOperationException>(() =>
                RunEncounterRosterBuilder.Build(node, floor: 1, inspectorFallback: null));
        }
    }
}
