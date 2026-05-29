using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class BattlePresenterTests
    {
        [Test]
        public void Consume_EmitsTurnResult_ThenTurnCompleted()
        {
            var presenter = new BattlePresenter();
            TurnResult publishedResult = null;
            BattleStateSnapshot completedSnapshot = default;
            bool turnReceivedCalled = false;
            bool completedCalled = false;

            presenter.TurnReceived += result =>
            {
                turnReceivedCalled = true;
                publishedResult = result;
            };
            presenter.TurnCompleted += snapshot =>
            {
                completedCalled = true;
                completedSnapshot = snapshot;
            };

            var events = new[]
            {
                CombatEvent.PlayerDamageToMonster(5),
                CombatEvent.MonsterActionExecuted(new MonsterAction(MonsterActionKind.Attack, 10, 0, null)),
                CombatEvent.MonsterDamageToPlayer(10),
            };
            var expectedFinal = new BattleStateSnapshot(
                playerHp: 20,
                playerMaxHp: 30,
                monsterHp: 45,
                monsterMaxHp: 50,
                patternIndex: 1,
                endReason: BattleEndReason.None);
            var result = new TurnResult(events, expectedFinal);

            presenter.Consume(result);

            Assert.That(turnReceivedCalled, Is.True);
            Assert.That(publishedResult, Is.SameAs(result));
            Assert.That(publishedResult.Events.Count, Is.EqualTo(3));
            Assert.That(publishedResult.Events[0].Kind, Is.EqualTo(CombatEventKind.PlayerDamageToMonster));
            Assert.That(completedCalled, Is.True);
            Assert.That(completedSnapshot.PlayerHp, Is.EqualTo(expectedFinal.PlayerHp));
            Assert.That(completedSnapshot.MonsterHp, Is.EqualTo(expectedFinal.MonsterHp));
            Assert.That(completedSnapshot.PatternIndex, Is.EqualTo(expectedFinal.PatternIndex));
        }
    }
}
