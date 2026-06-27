using NUnit.Framework;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Tests.GameFlow
{
    /// <summary>
    /// 런 저장/복원의 라운드트립을 검증합니다(영속화 직렬화 계약).
    /// </summary>
    public sealed class RunSaveRoundTripTests
    {
        [SetUp]
        public void SetUp()
        {
            GameFlowSession.StartNewRun();
        }

        [Test]
        public void CaptureThenRestore_PreservesProgress()
        {
            GameFlowSession.AdvanceToNextBattle();
            GameFlowSession.ApplyReward(RunRewardType.MaxHpUp);
            GameFlowSession.ApplySymbolReward(SlotSymbolType.Seven, 3);
            GameFlowSession.AddRelic(RelicCatalog.RewardPool[0]);

            int battle = GameFlowSession.CurrentBattleNumber;
            int maxHp = GameFlowSession.PlayerMaxHp;
            int currentHp = GameFlowSession.PlayerCurrentHp;
            int relicCount = GameFlowSession.OwnedRelics.Count;
            int sevenCount = GameFlowSession.SlotPool.GetCount(SlotSymbolType.Seven);

            RunSaveData saved = GameFlowSession.CaptureSave();

            // 상태를 새 런으로 초기화한 뒤 복원해 라운드트립을 확인한다.
            GameFlowSession.StartNewRun();
            bool restored = GameFlowSession.RestoreFromSave(saved);

            Assert.That(restored, Is.True);
            Assert.That(GameFlowSession.CurrentBattleNumber, Is.EqualTo(battle));
            Assert.That(GameFlowSession.PlayerMaxHp, Is.EqualTo(maxHp));
            Assert.That(GameFlowSession.PlayerCurrentHp, Is.EqualTo(currentHp));
            Assert.That(GameFlowSession.OwnedRelics.Count, Is.EqualTo(relicCount));
            Assert.That(GameFlowSession.SlotPool.GetCount(SlotSymbolType.Seven),
                Is.EqualTo(sevenCount));
        }

        [Test]
        public void RestoreFromSave_RejectsVersionMismatch()
        {
            RunSaveData saved = GameFlowSession.CaptureSave();
            saved.version += 1;

            Assert.That(GameFlowSession.RestoreFromSave(saved), Is.False);
        }

        [Test]
        public void RestoreFromSave_RejectsDeadRun()
        {
            RunSaveData saved = GameFlowSession.CaptureSave();
            saved.playerCurrentHp = 0;

            Assert.That(GameFlowSession.RestoreFromSave(saved), Is.False);
        }

        [Test]
        public void IsResumable_FalseDuringDefeatPending()
        {
            GameFlowSession.BeginBattleDefeat();

            Assert.That(GameFlowSession.IsResumable, Is.False);
        }

        [Test]
        public void IsResumable_TrueForActiveInfiniteRun()
        {
            Assert.That(GameFlowSession.IsResumable, Is.True);
        }
    }
}
