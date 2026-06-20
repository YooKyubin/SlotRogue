using NUnit.Framework;
using SlotRogue.UI.App;
using UnityEngine;

namespace SlotRogue.UI.Tests.App
{
    public sealed class FirstRunTutorialStateTests
    {
        [SetUp]
        public void SetUp()
        {
            FirstRunTutorialState.ResetForDebug();
        }

        [TearDown]
        public void TearDown()
        {
            FirstRunTutorialState.ResetForDebug();
        }

        [Test]
        public void MarkCompleted_PersistsCompletedFlag()
        {
            Assert.That(FirstRunTutorialState.IsCompleted, Is.False);

            FirstRunTutorialState.MarkCompleted();

            Assert.That(FirstRunTutorialState.IsCompleted, Is.True);
            Assert.That(
                PlayerPrefs.GetInt(FirstRunTutorialState.CompletedKey, 0),
                Is.EqualTo(1));
        }

        [Test]
        public void ResetForDebug_RemovesCompletedFlag()
        {
            FirstRunTutorialState.MarkCompleted();

            FirstRunTutorialState.ResetForDebug();

            Assert.That(FirstRunTutorialState.IsCompleted, Is.False);
        }
    }
}
