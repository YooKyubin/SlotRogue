using NUnit.Framework;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Tests.GameFlow
{
    public sealed class FirstRunTutorialFlowTests
    {
        [TearDown]
        public void TearDown()
        {
            GameFlowSession.EndRun();
        }

        [Test]
        public void StartTutorialRun_SetsTutorialMode()
        {
            GameFlowSession.StartTutorialRun();

            Assert.That(GameFlowSession.HasRun, Is.True);
            Assert.That(GameFlowSession.IsTutorialRun, Is.True);
            Assert.That(GameFlowSession.OwnedRelics.Count, Is.EqualTo(0));
        }

        [Test]
        public void StartNewRun_ClearsTutorialMode()
        {
            GameFlowSession.StartTutorialRun();

            GameFlowSession.StartNewRun();

            Assert.That(GameFlowSession.IsTutorialRun, Is.False);
        }

        [Test]
        public void CompleteTutorialAndContinueAsNormalRun_ClearsTutorialMode()
        {
            GameFlowSession.StartTutorialRun();

            GameFlowSession.CompleteTutorialAndContinueAsNormalRun();

            Assert.That(GameFlowSession.HasRun, Is.True);
            Assert.That(GameFlowSession.IsTutorialRun, Is.False);
        }
    }
}
