using System.IO;
using SlotRogue.Data.Combat;
using SlotRogue.Data.GameFlow;
using UnityEditor;
using UnityEngine;

namespace SlotRogue.Editor.GameFlow
{
    public static class RunMapGraphAssetBuilder
    {
        private const string MapNodesFolder = "Assets/_Project/Data/GameFlow/MapNodes";
        private const string EncountersFolder = "Assets/_Project/Data/GameFlow/Encounters";
        private const string GraphAssetPath = "Assets/_Project/Resources/DefaultRunMapGraph.asset";
        private const string DefaultMonsterPath = "Assets/_Project/Data/Monsters/Goblin.asset";

        [MenuItem("SlotRogue/Game Flow/Build Default Map Graph Assets")]
        public static void BuildDefaultMapGraphAssets()
        {
            EnsureFolder(MapNodesFolder);
            EnsureFolder(EncountersFolder);
            EnsureFolder("Assets/_Project/Resources");

            MonsterDefinition defaultMonster = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(DefaultMonsterPath);
            RunEncounterDefinition singleMonster = LoadOrCreateEncounter(
                $"{EncountersFolder}/Encounter_SingleMonster.asset",
                defaultMonster,
                new[] { 1 });
            RunEncounterDefinition singleElite = LoadOrCreateEncounter(
                $"{EncountersFolder}/Encounter_SingleElite.asset",
                defaultMonster,
                new[] { 1 });
            RunEncounterDefinition boss = LoadOrCreateEncounter(
                $"{EncountersFolder}/Encounter_Boss.asset",
                defaultMonster,
                new[] { 1 });
            RunEncounterDefinition duo2A = LoadOrCreateEncounter(
                $"{EncountersFolder}/Encounter_Duo2A.asset",
                defaultMonster,
                new[] { 0, 2 });
            RunEncounterDefinition eliteTrio2B = LoadOrCreateEncounter(
                $"{EncountersFolder}/Encounter_EliteTrio2B.asset",
                defaultMonster,
                new[] { 0, 1, 2 });

            NodeSpec[] specs = CreateDefaultNodeSpecs(
                singleMonster,
                singleElite,
                boss,
                duo2A,
                eliteTrio2B);

            var nodeAssets = new RunMapNodeDefinition[specs.Length];
            for (int index = 0; index < specs.Length; index++)
            {
                nodeAssets[index] = LoadOrCreateNode(specs[index]);
            }

            RunMapGraphDefinition graph = AssetDatabase.LoadAssetAtPath<RunMapGraphDefinition>(GraphAssetPath);
            if (graph == null)
            {
                graph = ScriptableObject.CreateInstance<RunMapGraphDefinition>();
                AssetDatabase.CreateAsset(graph, GraphAssetPath);
            }

            graph.nodes = nodeAssets;
            graph.edges = CreateDefaultEdges();
            EditorUtility.SetDirty(graph);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RunMapGraphAssetBuilder] Built {nodeAssets.Length} nodes and graph at {GraphAssetPath}.");
        }

        private static NodeSpec[] CreateDefaultNodeSpecs(
            RunEncounterDefinition singleMonster,
            RunEncounterDefinition singleElite,
            RunEncounterDefinition boss,
            RunEncounterDefinition duo2A,
            RunEncounterDefinition eliteTrio2B)
        {
            return new[]
            {
                Spec("start-0", "Start", "Run starting point.", RunMapNodeType.Start, 0, 1, null),
                Spec("monster-1-0", "Monster 1-A", "A safer opening fight.", RunMapNodeType.Monster, 1, 0, singleMonster),
                Spec("monster-1-1", "Monster 1-B", "A balanced opening fight.", RunMapNodeType.Monster, 1, 1, singleMonster),
                Spec("elite-1-2", "Elite 1-C", "A risky opening elite route.", RunMapNodeType.Elite, 1, 2, singleElite),
                Spec("monster-2-0", "Duo 2-A", "A left-side two-enemy fight.", RunMapNodeType.Monster, 2, 0, duo2A),
                Spec("elite-2-1", "Elite Trio 2-B", "A stronger three-enemy center route.", RunMapNodeType.Elite, 2, 1, eliteTrio2B),
                Spec("monster-2-2", "Monster 2-C", "A right-side monster route.", RunMapNodeType.Monster, 2, 2, singleMonster),
                Spec("monster-3-0", "Monster 3-A", "A steady left branch.", RunMapNodeType.Monster, 3, 0, singleMonster),
                Spec("monster-3-1", "Monster 3-B", "A steady center branch.", RunMapNodeType.Monster, 3, 1, singleMonster),
                Spec("elite-3-2", "Elite 3-C", "A high-pressure right branch.", RunMapNodeType.Elite, 3, 2, singleElite),
                Spec("monster-4-0", "Monster 4-A", "A safer late route.", RunMapNodeType.Monster, 4, 0, singleMonster),
                Spec("elite-4-1", "Elite 4-B", "A dangerous late route.", RunMapNodeType.Elite, 4, 1, singleElite),
                Spec("monster-4-2", "Monster 4-C", "A safer late route.", RunMapNodeType.Monster, 4, 2, singleMonster),
                Spec("monster-5-0", "Monster 5-A", "A final preparation fight.", RunMapNodeType.Monster, 5, 0, singleMonster),
                Spec("monster-5-1", "Monster 5-B", "A final preparation fight.", RunMapNodeType.Monster, 5, 1, singleMonster),
                Spec("elite-5-2", "Elite 5-C", "A final elite before the boss.", RunMapNodeType.Elite, 5, 2, singleElite),
                Spec("boss-6-1", "Boss Gate", "The boss blocks the route.", RunMapNodeType.Boss, 6, 1, boss),
            };
        }

        private static RunMapGraphEdge[] CreateDefaultEdges()
        {
            return new[]
            {
                Edge("start-0", "monster-1-0"),
                Edge("start-0", "monster-1-1"),
                Edge("start-0", "elite-1-2"),
                Edge("monster-1-0", "monster-2-0"),
                Edge("monster-1-0", "elite-2-1"),
                Edge("monster-1-1", "monster-2-0"),
                Edge("monster-1-1", "elite-2-1"),
                Edge("monster-1-1", "monster-2-2"),
                Edge("elite-1-2", "elite-2-1"),
                Edge("elite-1-2", "monster-2-2"),
                Edge("monster-2-0", "monster-3-0"),
                Edge("monster-2-0", "monster-3-1"),
                Edge("elite-2-1", "monster-3-0"),
                Edge("elite-2-1", "monster-3-1"),
                Edge("elite-2-1", "elite-3-2"),
                Edge("monster-2-2", "monster-3-1"),
                Edge("monster-2-2", "elite-3-2"),
                Edge("monster-3-0", "monster-4-0"),
                Edge("monster-3-0", "elite-4-1"),
                Edge("monster-3-1", "monster-4-0"),
                Edge("monster-3-1", "elite-4-1"),
                Edge("monster-3-1", "monster-4-2"),
                Edge("elite-3-2", "elite-4-1"),
                Edge("elite-3-2", "monster-4-2"),
                Edge("monster-4-0", "monster-5-0"),
                Edge("monster-4-0", "monster-5-1"),
                Edge("elite-4-1", "monster-5-0"),
                Edge("elite-4-1", "monster-5-1"),
                Edge("elite-4-1", "elite-5-2"),
                Edge("monster-4-2", "monster-5-1"),
                Edge("monster-4-2", "elite-5-2"),
                Edge("monster-5-0", "boss-6-1"),
                Edge("monster-5-1", "boss-6-1"),
                Edge("elite-5-2", "boss-6-1"),
            };
        }

        private static RunEncounterDefinition LoadOrCreateEncounter(
            string assetPath,
            MonsterDefinition monster,
            int[] formationSlots)
        {
            RunEncounterDefinition encounter = AssetDatabase.LoadAssetAtPath<RunEncounterDefinition>(assetPath);
            if (encounter == null)
            {
                encounter = ScriptableObject.CreateInstance<RunEncounterDefinition>();
                AssetDatabase.CreateAsset(encounter, assetPath);
            }

            var entries = new RunEncounterEntry[formationSlots.Length];
            for (int index = 0; index < formationSlots.Length; index++)
            {
                entries[index] = new RunEncounterEntry
                {
                    monster = monster,
                    formationSlot = formationSlots[index],
                };
            }

            encounter.entries = entries;
            EditorUtility.SetDirty(encounter);
            return encounter;
        }

        private static RunMapNodeDefinition LoadOrCreateNode(NodeSpec spec)
        {
            string assetPath = $"{MapNodesFolder}/Node_{spec.NodeId}.asset";
            RunMapNodeDefinition node = AssetDatabase.LoadAssetAtPath<RunMapNodeDefinition>(assetPath);
            if (node == null)
            {
                node = ScriptableObject.CreateInstance<RunMapNodeDefinition>();
                AssetDatabase.CreateAsset(node, assetPath);
            }

            ApplyNodeSpec(node, spec);
            EditorUtility.SetDirty(node);
            return node;
        }

        private static void ApplyNodeSpec(RunMapNodeDefinition node, NodeSpec spec)
        {
            SerializedObject serializedObject = new(node);
            serializedObject.FindProperty("_nodeID").stringValue = spec.NodeId;
            serializedObject.FindProperty("_displayName").stringValue = spec.DisplayName;
            serializedObject.FindProperty("_description").stringValue = spec.Description;
            serializedObject.FindProperty("_nodeType").enumValueIndex = (int)spec.NodeType;
            serializedObject.FindProperty("_floor").intValue = spec.Floor;
            serializedObject.FindProperty("_lane").intValue = spec.Lane;
            serializedObject.FindProperty("_encounter").objectReferenceValue = spec.Encounter;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static NodeSpec Spec(
            string nodeId,
            string displayName,
            string description,
            RunMapNodeType nodeType,
            int floor,
            int lane,
            RunEncounterDefinition encounter)
        {
            return new NodeSpec(nodeId, displayName, description, nodeType, floor, lane, encounter);
        }

        private static RunMapGraphEdge Edge(string fromNodeId, string toNodeId)
        {
            return new RunMapGraphEdge
            {
                fromNodeId = fromNodeId,
                toNodeId = toNodeId,
            };
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private sealed class NodeSpec
        {
            public NodeSpec(
                string nodeId,
                string displayName,
                string description,
                RunMapNodeType nodeType,
                int floor,
                int lane,
                RunEncounterDefinition encounter)
            {
                NodeId = nodeId;
                DisplayName = displayName;
                Description = description;
                NodeType = nodeType;
                Floor = floor;
                Lane = lane;
                Encounter = encounter;
            }

            public string NodeId { get; }

            public string DisplayName { get; }

            public string Description { get; }

            public RunMapNodeType NodeType { get; }

            public int Floor { get; }

            public int Lane { get; }

            public RunEncounterDefinition Encounter { get; }
        }
    }
}
