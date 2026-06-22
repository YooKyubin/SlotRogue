using NUnit.Framework;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;
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
            Assert.That(GameFlowSession.PlayerMaxHp, Is.EqualTo(20));
            Assert.That(GameFlowSession.PlayerCurrentHp, Is.EqualTo(20));
            Assert.That(GameFlowSession.OwnedRelics.Count, Is.EqualTo(1));
            Assert.That(
                GameFlowSession.OwnedRelics[0].Id,
                Is.EqualTo("TUTORIAL-TRAINING-BATTERY"));
        }

        [Test]
        public void StartNewRun_ClearsTutorialMode()
        {
            GameFlowSession.StartTutorialRun();

            GameFlowSession.StartNewRun();

            Assert.That(GameFlowSession.IsTutorialRun, Is.False);
        }

        [Test]
        public void FirstSpin_ProducesCherryAndLemonPatterns()
        {
            SlotSpinResult spinResult = TutorialSlotSpinFactory.CreateFirstSpin();
            var matches = new SlotPatternResolver().ResolveAll(spinResult);

            Assert.That(HasPattern(matches, SlotSymbolType.Cherry, 3), Is.True);
            Assert.That(HasPattern(matches, SlotSymbolType.Lemon, 3), Is.True);
        }

        [Test]
        public void FirstSpin_TriggersTrainingBatteryDamageAndBlock()
        {
            GameFlowSession.StartTutorialRun();
            SlotSpinResult spinResult = TutorialSlotSpinFactory.CreateFirstSpin();
            var matches = new SlotPatternResolver().ResolveAll(spinResult);
            var result = new RelicEffectRunner().Resolve(
                matches,
                GameFlowSession.OwnedRelics,
                new RelicBattleContext(
                    playerCurrentHp: 20,
                    playerMaxHp: 20,
                    enemyCurrentHp: 6,
                    enemyMaxHp: 6,
                    enemyHasAnyStatus: false));

            Assert.That(result.AdditionalDamage, Is.EqualTo(3));
            Assert.That(result.AdditionalBlock, Is.EqualTo(4));
        }

        [Test]
        public void SecondSpin_DefeatsShieldedRightTutorialMonster()
        {
            GameFlowSession.StartTutorialRun();
            SlotSpinResult spinResult = TutorialSlotSpinFactory.CreateSecondSpin();
            var matches = new SlotPatternResolver().ResolveAll(spinResult);
            var calculation = new SlotResultCalculator().Calculate(matches);
            var result = new RelicEffectRunner().Resolve(
                matches,
                GameFlowSession.OwnedRelics,
                new RelicBattleContext(
                    playerCurrentHp: 20,
                    playerMaxHp: 20,
                    enemyCurrentHp: 8,
                    enemyMaxHp: 8,
                    enemyHasAnyStatus: false));

            Assert.That(HasPattern(matches, SlotSymbolType.Cherry, 5), Is.True);
            Assert.That(
                calculation.Damage + result.AdditionalDamage,
                Is.GreaterThanOrEqualTo(11));
        }

        private static bool HasPattern(
            System.Collections.Generic.IReadOnlyList<SlotPatternMatch> matches,
            SlotSymbolType symbol,
            int minimumCells)
        {
            for (int index = 0; index < matches.Count; index++)
            {
                SlotPatternMatch match = matches[index];
                if (match.Symbol == symbol && match.MatchedCells.Count >= minimumCells)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
