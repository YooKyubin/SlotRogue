using NUnit.Framework;
using SlotRogue.Data.GameFlow;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class GameFlowSessionInfiniteTests
    {
        [SetUp]
        public void SetUp()
        {
            RunMapNodeCatalog.ConfigureGraph(null);
            GameFlowSession.StartNewRun();
            GameFlowSession.ElitePeriod = 5;
            GameFlowSession.BossPeriod = 10;
        }

        [TearDown]
        public void TearDown()
        {
            RunMapNodeCatalog.ConfigureGraph(null);
            GameFlowSession.ElitePeriod = 5;
            GameFlowSession.BossPeriod = 10;
        }

        [Test]
        public void StartNewRun_FirstBattleIsNormal()
        {
            Assert.That(GameFlowSession.CurrentBattleNumber, Is.EqualTo(1));
            Assert.That(GameFlowSession.CurrentTier, Is.EqualTo(EncounterTier.Normal));
            Assert.That(GameFlowSession.CurrentBattleGrantsArtifact, Is.False);
        }

        [TestCase(1, EncounterTier.Normal)]
        [TestCase(4, EncounterTier.Normal)]
        [TestCase(5, EncounterTier.Elite)]
        [TestCase(9, EncounterTier.Normal)]
        [TestCase(10, EncounterTier.Boss)]
        [TestCase(15, EncounterTier.Elite)]
        [TestCase(20, EncounterTier.Boss)]
        public void GetTierForBattle_BossTakesPrecedenceOverElite(int battleNumber, EncounterTier expected)
        {
            Assert.That(GameFlowSession.GetTierForBattle(battleNumber), Is.EqualTo(expected));
        }

        [Test]
        public void AdvanceToNextBattle_IncrementsBattleNumber()
        {
            GameFlowSession.AdvanceToNextBattle();
            Assert.That(GameFlowSession.CurrentBattleNumber, Is.EqualTo(2));
        }

        [Test]
        public void CompleteInfiniteVictory_NormalBattle_HealsSmallAmount()
        {
            GameFlowSession.CompleteInfiniteVictory(remainingPlayerHp: 10);

            // 10 + NormalWinHeal(4) = 14
            Assert.That(GameFlowSession.PlayerCurrentHp, Is.EqualTo(14));
            Assert.That(GameFlowSession.Victories, Is.EqualTo(1));
        }

        [Test]
        public void CompleteInfiniteVictory_NormalHeal_DoesNotExceedMax()
        {
            GameFlowSession.CompleteInfiniteVictory(remainingPlayerHp: 29);

            Assert.That(GameFlowSession.PlayerCurrentHp, Is.EqualTo(GameFlowSession.PlayerMaxHp));
        }

        [Test]
        public void CompleteInfiniteVictory_EliteBattle_DoesNotHeal()
        {
            // 전투 5번까지 진행 → 엘리트
            for (int i = 1; i < 5; i++) GameFlowSession.AdvanceToNextBattle();
            Assert.That(GameFlowSession.CurrentTier, Is.EqualTo(EncounterTier.Elite));

            GameFlowSession.CompleteInfiniteVictory(remainingPlayerHp: 10);

            Assert.That(GameFlowSession.PlayerCurrentHp, Is.EqualTo(10));
            Assert.That(GameFlowSession.CurrentBattleGrantsArtifact, Is.True);
        }

        [Test]
        public void BuildForTier_ProducesSingleScaledEnemy()
        {
            RunEncounterRoster roster =
                RunEncounterRosterBuilder.BuildForTier(EncounterTier.Boss, level: 1, fallback: null);

            Assert.That(roster.Enemies, Has.Length.EqualTo(1));
            Assert.That(roster.Schedules, Has.Length.EqualTo(1));
            Assert.That(roster.FormationSlots[0], Is.EqualTo(0));
            // Boss level1: 46 + 1*10 = 56
            Assert.That(roster.Enemies[0].MaxHp, Is.EqualTo(56));
        }
    }
}
