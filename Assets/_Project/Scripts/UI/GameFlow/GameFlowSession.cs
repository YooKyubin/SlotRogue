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

        public static StarterArtifactId SelectedStarterArtifactId { get; private set; }

        public static RunMapNodeDefinition CurrentMapNode { get; private set; }

        public static RunMapNodeDefinition CurrentEncounterNode { get; private set; }

        public static RunMapGraphDefinition CurrentMapGraph { get; private set; }

        public static IReadOnlyList<string> VisitedMapNodeIds => visitedMapNodeIds;

        public static bool HasStarterArtifact => SelectedStarterArtifactId != StarterArtifactId.None;

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
            SelectedStarterArtifactId = StarterArtifactId.None;
            CurrentMapGraph = RunMapNodeCatalog.BuildDefaultGraph();
            CurrentMapNode =
                CurrentMapGraph.GetNode(RunMapNodeCatalog.StartNode.NodeId) ??
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

        public static void SelectStarterArtifact(StarterArtifactId artifactId)
        {
            EnsureRunStarted();
            SelectedStarterArtifactId = artifactId;
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
            StarterArtifactDefinition artifact = StarterArtifactCatalog.Get(SelectedStarterArtifactId);

            return
                $"HP {PlayerCurrentHp}/{PlayerMaxHp}\n" +
                $"Battles entered: {BattleIndex}\n" +
                $"Victories: {Victories}\n" +
                $"Rewards: {RewardsClaimed}\n" +
                $"Current Node: {CurrentMapNode.DisplayName}\n" +
                $"Starter Artifact: {artifact.DisplayName}\n" +
                $"Run Bonuses: damage +{DamageBonus}, defense +{DefenseBonus}";
        }
    }
}
