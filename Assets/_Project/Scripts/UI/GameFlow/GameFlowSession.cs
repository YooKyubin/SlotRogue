using SlotRogue.Data.GameFlow;
using System;
using System.Collections.Generic;

namespace SlotRogue.UI.GameFlow
{
    public static class GameFlowSession
    {
        private const int DefaultPlayerMaxHp = 30;
        private static readonly List<string> visitedMapNodeIds = new();

        public static bool HasRun { get; private set; }

        public static int PlayerMaxHp { get; private set; }

        public static int PlayerCurrentHp { get; private set; }

        public static int BattleIndex { get; private set; }

        public static int Victories { get; private set; }

        public static int RewardsClaimed { get; private set; }

        public static int DamageBonus { get; private set; }

        public static int DefenseBonus { get; private set; }

        public static string SelectedArtifactId { get; private set; } = string.Empty;

        public static RunMapNodeDefinition CurrentMapNode { get; private set; }

        public static RunMapNodeDefinition CurrentEncounterNode { get; private set; }

        public static RunMapGraphDefinition CurrentMapGraph { get; private set; }

        public static IReadOnlyList<string> VisitedMapNodeIds => visitedMapNodeIds;

        public static bool HasStarterArtifact => !string.IsNullOrEmpty(SelectedArtifactId);

        public static void StartNewRun()
        {
            HasRun = true;
            PlayerMaxHp = DefaultPlayerMaxHp;
            PlayerCurrentHp = DefaultPlayerMaxHp;
            BattleIndex = 0;
            Victories = 0;
            RewardsClaimed = 0;
            DamageBonus = 0;
            DefenseBonus = 0;
            SelectedArtifactId = string.Empty;
            CurrentMapGraph = RunMapNodeCatalog.BuildDefaultGraph();
            CurrentMapNode =
                CurrentMapGraph.GetNode("start-0") ??
                RunMapNodeCatalog.StartNode;
            CurrentEncounterNode = null;
            visitedMapNodeIds.Clear();
            visitedMapNodeIds.Add(CurrentMapNode.NodeId);
        }

        public static void EnsureRunStarted()
        {
            if (!HasRun)
            {
                StartNewRun();
            }
        }

        public static void SelectArtifact(string artifactId)
        {
            EnsureRunStarted();
            SelectedArtifactId = artifactId ?? string.Empty;
        }

        public static void SelectStarterArtifact(StarterArtifactId artifactId)
        {
            ArtifactDefinitionSO so = StarterArtifactCatalog.Get(artifactId);
            SelectArtifact(so?.ArtifactId ?? string.Empty);
        }

        public static RunMapNodeDefinition[] GetNextMapChoices()
        {
            EnsureRunStarted();
            return RunMapNodeCatalog.BuildNextChoices(CurrentMapGraph, CurrentMapNode);
        }

        public static bool SelectNextMapNode(string nodeId)
        {
            EnsureRunStarted();
            RunMapNodeDefinition[] choices = GetNextMapChoices();
            RunMapNodeDefinition selectedNode = RunMapNodeCatalog.FindChoice(choices, nodeId);

            if (selectedNode == null)
            {
                return false;
            }

            CurrentMapNode = selectedNode;
            CurrentEncounterNode = selectedNode;
            if (!visitedMapNodeIds.Contains(selectedNode.NodeId))
            {
                visitedMapNodeIds.Add(selectedNode.NodeId);
            }

            BattleIndex++;
            return true;
        }

        public static bool IsMapNodeVisited(string nodeId)
        {
            EnsureRunStarted();
            return visitedMapNodeIds.Contains(nodeId);
        }

        public static bool IsMapNodeSelectable(string nodeId)
        {
            EnsureRunStarted();
            RunMapNodeDefinition[] choices = GetNextMapChoices();
            return RunMapNodeCatalog.FindChoice(choices, nodeId) != null;
        }

        public static void CompleteBattleVictory(int remainingPlayerHp)
        {
            EnsureRunStarted();
            PlayerCurrentHp = Math.Max(1, Math.Min(remainingPlayerHp, PlayerMaxHp));
            Victories++;
        }

        public static void CompleteBattleDefeat()
        {
            HasRun = false;
        }

        public static void ApplyReward(RunRewardType rewardType)
        {
            EnsureRunStarted();

            switch (rewardType)
            {
                case RunRewardType.Heal:
                    PlayerCurrentHp = Math.Min(PlayerCurrentHp + 8, PlayerMaxHp);
                    break;
                case RunRewardType.DamageBonus:
                    DamageBonus += 2;
                    break;
                case RunRewardType.DefenseBonus:
                    DefenseBonus += 2;
                    break;
                default:
                    break;
            }

            RewardsClaimed++;
        }

        public static string BuildSummary()
        {
            ArtifactDefinitionSO artifact = StarterArtifactCatalog.GetById(SelectedArtifactId);

            return
                $"HP {PlayerCurrentHp}/{PlayerMaxHp}\n" +
                $"진입 전투: {BattleIndex}\n" +
                $"승리: {Victories}\n" +
                $"보상: {RewardsClaimed}\n" +
                $"현재 노드: {CurrentMapNode.DisplayName}\n" +
                $"시작 유물: {artifact?.DisplayName ?? "없음"}\n" +
                $"런 보너스: 피해 +{DamageBonus}, 방어 +{DefenseBonus}";
        }
    }
}
