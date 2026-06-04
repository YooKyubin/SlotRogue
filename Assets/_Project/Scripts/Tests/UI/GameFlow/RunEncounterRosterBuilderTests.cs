using System;
using NUnit.Framework;
using SlotRogue.Data.GameFlow;
using SlotRogue.UI.GameFlow;
using UnityEditor;
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

            RunMapNodeDefinition node = CreateNode(
                nodeId: "test-duo",
                nodeType: RunMapNodeType.Monster,
                floor: 2,
                encounter: encounter);

            RunEncounterRoster roster = RunEncounterRosterBuilder.Build(node, floor: 2, inspectorFallback: null);

            Assert.That(roster.Enemies, Has.Length.EqualTo(2));
            Assert.That(roster.FormationSlots[0], Is.EqualTo(0));
            Assert.That(roster.FormationSlots[1], Is.EqualTo(2));
        }

        [Test]
        public void Build_WithoutEncounter_ThrowsInvalidOperationException()
        {
            RunMapNodeDefinition node = CreateNode(
                nodeId: "test-single",
                nodeType: RunMapNodeType.Monster,
                floor: 1);

            Assert.Throws<InvalidOperationException>(() =>
                RunEncounterRosterBuilder.Build(node, floor: 1, inspectorFallback: null));
        }

        private static RunMapNodeDefinition CreateNode(
            string nodeId,
            RunMapNodeType nodeType,
            int floor,
            RunEncounterDefinition encounter = null!)
        {
            var node = ScriptableObject.CreateInstance<RunMapNodeDefinition>();
            var serializedObject = new SerializedObject(node);
            serializedObject.FindProperty("_nodeID").stringValue = nodeId;
            serializedObject.FindProperty("_nodeType").enumValueIndex = (int)nodeType;
            serializedObject.FindProperty("_floor").intValue = floor;
            serializedObject.FindProperty("_encounter").objectReferenceValue = encounter;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return node;
        }
    }
}
