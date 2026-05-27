using System.Collections.Generic;
using NUnit.Framework;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;

namespace SlotRogue.Core.Tests.Combat
{
    public sealed class BattlePresenterTests
    {
        [Test]
        public void Consume_EmitsAllCombatEventsInOrder_ThenTurnCompleted()
        {
            var presenter = new BattlePresenter();
            var publishedKinds = new List<CombatEventKind>();
            BattleStateSnapshot completedSnapshot = default;
            bool completedCalled = false;

            presenter.CombatEventEmitted += evt => publishedKinds.Add(evt.Kind);
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

            Assert.That(publishedKinds, Is.EqualTo(new[]
            {
                CombatEventKind.PlayerDamageToMonster,
                CombatEventKind.MonsterActionExecuted,
                CombatEventKind.MonsterDamageToPlayer
            }));
            Assert.That(completedCalled, Is.True);
            Assert.That(completedSnapshot.PlayerHp, Is.EqualTo(expectedFinal.PlayerHp));
            Assert.That(completedSnapshot.MonsterHp, Is.EqualTo(expectedFinal.MonsterHp));
            Assert.That(completedSnapshot.PatternIndex, Is.EqualTo(expectedFinal.PatternIndex));
        }
    }
}
