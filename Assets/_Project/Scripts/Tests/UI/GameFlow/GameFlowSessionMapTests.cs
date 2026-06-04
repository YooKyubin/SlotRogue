using NUnit.Framework;
using SlotRogue.Data.GameFlow;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class GameFlowSessionMapTests
    {
        [SetUp]
        public void SetUp()
        {
            RunMapNodeCatalog.ConfigureGraph(null);
            GameFlowSession.StartNewRun();
            GameFlowSession.SelectStarterArtifact(StarterArtifactId.BeginnerBlade);
        }

        [TearDown]
        public void TearDown()
        {
            RunMapNodeCatalog.ConfigureGraph(null);
        }

        [Test]
        public void StartNewRun_CurrentMapNodeIsStart()
        {
            Assert.That(GameFlowSession.CurrentMapNode.NodeType, Is.EqualTo(RunMapNodeType.Start));
            Assert.That(GameFlowSession.CurrentMapNode.Floor, Is.EqualTo(0));
            Assert.That(GameFlowSession.VisitedMapNodeIds, Contains.Item(GameFlowSession.CurrentMapNode.NodeId));
        }

        [Test]
        public void StartNewRun_CurrentMapGraphContainsFullRoute()
        {
            Assert.That(GameFlowSession.CurrentMapGraph.Nodes, Has.Length.EqualTo(17));
            Assert.That(GameFlowSession.CurrentMapGraph.Edges, Has.Length.EqualTo(34));
            Assert.That(GameFlowSession.CurrentMapGraph.GetNode("boss-6-1").NodeType, Is.EqualTo(RunMapNodeType.Boss));
        }

        [Test]
        public void GetNextMapChoices_FromStart_ReturnsSelectableNodes()
        {
            RunMapNodeDefinition[] choices = GameFlowSession.GetNextMapChoices();

            Assert.That(choices, Has.Length.EqualTo(3));
            Assert.That(choices[0].Floor, Is.EqualTo(1));
            Assert.That(choices[0].NodeType, Is.EqualTo(RunMapNodeType.Monster));
            Assert.That(choices[2].NodeType, Is.EqualTo(RunMapNodeType.Elite));
        }

        [Test]
        public void SelectNextMapNode_ValidChoice_UpdatesCurrentAndEncounter()
        {
            RunMapNodeDefinition selected = GameFlowSession.GetNextMapChoices()[1];

            bool selectedNode = GameFlowSession.SelectNextMapNode(selected.NodeId);

            Assert.That(selectedNode, Is.True);
            Assert.That(GameFlowSession.CurrentMapNode.NodeId, Is.EqualTo(selected.NodeId));
            Assert.That(GameFlowSession.CurrentEncounterNode.NodeId, Is.EqualTo(selected.NodeId));
            Assert.That(GameFlowSession.VisitedMapNodeIds, Contains.Item(selected.NodeId));
            Assert.That(GameFlowSession.BattleIndex, Is.EqualTo(1));
        }

        [Test]
        public void SelectNextMapNode_UnconnectedChoice_DoesNotAdvance()
        {
            bool selectedNode = GameFlowSession.SelectNextMapNode("monster-2-0");

            Assert.That(selectedNode, Is.False);
            Assert.That(GameFlowSession.CurrentMapNode.NodeType, Is.EqualTo(RunMapNodeType.Start));
            Assert.That(GameFlowSession.BattleIndex, Is.EqualTo(0));
        }

        [Test]
        public void SelectNextMapNode_InvalidChoice_DoesNotAdvance()
        {
            bool selectedNode = GameFlowSession.SelectNextMapNode("not-a-node");

            Assert.That(selectedNode, Is.False);
            Assert.That(GameFlowSession.CurrentMapNode.NodeType, Is.EqualTo(RunMapNodeType.Start));
            Assert.That(GameFlowSession.BattleIndex, Is.EqualTo(0));
        }
    }
}
