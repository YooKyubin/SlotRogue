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
            GameFlowSession.ApplySymbolBaseDamageReward(
                new[] { SlotSymbolType.Seven },
                5,
                countRewardClaim: false);
            GameFlowSession.AddSpinCoins();
            GameFlowSession.AddSpinCoins();
            GameFlowSession.TryIncreaseRelicSlotCapacity(1);
            GameFlowSession.AddRelic(RelicCatalog.GetById("R-02"));

            int battle = GameFlowSession.CurrentBattleNumber;
            int maxHp = GameFlowSession.PlayerMaxHp;
            int currentHp = GameFlowSession.PlayerCurrentHp;
            int runCoins = GameFlowSession.RunCoins;
            int relicSlotCapacity = GameFlowSession.RelicSlotCapacity;
            int relicCount = GameFlowSession.OwnedRelics.Count;
            int sevenWeight = GameFlowSession.SlotPool.GetWeight(SlotSymbolType.Seven);
            int sevenDamage = SlotSymbolAttackValues.DamageFor(SlotSymbolType.Seven);

            RunSaveData saved = GameFlowSession.CaptureSave();

            // 상태를 새 런으로 초기화한 뒤 복원해 라운드트립을 확인한다.
            GameFlowSession.StartNewRun();
            Assert.That(SlotSymbolAttackValues.DamageFor(SlotSymbolType.Seven),
                Is.Not.EqualTo(sevenDamage));

            bool restored = GameFlowSession.RestoreFromSave(saved);

            Assert.That(restored, Is.True);
            Assert.That(GameFlowSession.CurrentBattleNumber, Is.EqualTo(battle));
            Assert.That(GameFlowSession.PlayerMaxHp, Is.EqualTo(maxHp));
            Assert.That(GameFlowSession.PlayerCurrentHp, Is.EqualTo(currentHp));
            Assert.That(GameFlowSession.RunCoins, Is.EqualTo(runCoins));
            Assert.That(GameFlowSession.RelicSlotCapacity, Is.EqualTo(relicSlotCapacity));
            Assert.That(GameFlowSession.OwnedRelics.Count, Is.EqualTo(relicCount));
            Assert.That(GameFlowSession.SlotPool.GetWeight(SlotSymbolType.Seven),
                Is.EqualTo(sevenWeight));
            Assert.That(SlotSymbolAttackValues.DamageFor(SlotSymbolType.Seven),
                Is.EqualTo(sevenDamage));
        }

        [Test]
        public void RestoreFromSave_ReapplyingSameSave_DoesNotDoubleSymbolBaseDamage()
        {
            GameFlowSession.ApplySymbolBaseDamageReward(
                new[] { SlotSymbolType.Seven },
                5,
                countRewardClaim: false);

            int savedSevenDamage = SlotSymbolAttackValues.DamageFor(SlotSymbolType.Seven);
            RunSaveData saved = GameFlowSession.CaptureSave();

            GameFlowSession.StartNewRun();

            Assert.That(GameFlowSession.RestoreFromSave(saved), Is.True);
            Assert.That(SlotSymbolAttackValues.DamageFor(SlotSymbolType.Seven),
                Is.EqualTo(savedSevenDamage));

            Assert.That(GameFlowSession.RestoreFromSave(saved), Is.True);
            Assert.That(SlotSymbolAttackValues.DamageFor(SlotSymbolType.Seven),
                Is.EqualTo(savedSevenDamage));
        }

        [Test]
        public void TryAddRelic_StopsAtDefaultRelicSlotCapacity()
        {
            string[] relicIds = { "R-02", "R-03", "R-04", "R-05", "R-06", "R-07" };

            for (int index = 0; index < GameFlowSession.DefaultRelicSlotCapacity; index++)
            {
                Assert.That(GameFlowSession.TryAddRelic(RelicCatalog.GetById(relicIds[index])),
                    Is.True);
            }

            Assert.That(GameFlowSession.TryAddRelic(RelicCatalog.GetById(relicIds[5])),
                Is.False);
            Assert.That(
                GameFlowSession.OwnedRelics.Count,
                Is.EqualTo(GameFlowSession.DefaultRelicSlotCapacity));
        }

        [Test]
        public void TryIncreaseRelicSlotCapacity_CapsAtMaximum()
        {
            Assert.That(
                GameFlowSession.RelicSlotCapacity,
                Is.EqualTo(GameFlowSession.DefaultRelicSlotCapacity));

            Assert.That(GameFlowSession.TryIncreaseRelicSlotCapacity(1), Is.True);
            Assert.That(GameFlowSession.RelicSlotCapacity, Is.EqualTo(6));

            Assert.That(GameFlowSession.TryIncreaseRelicSlotCapacity(99), Is.True);
            Assert.That(
                GameFlowSession.RelicSlotCapacity,
                Is.EqualTo(GameFlowSession.MaxRelicSlotCapacity));

            Assert.That(GameFlowSession.TryIncreaseRelicSlotCapacity(1), Is.False);
            Assert.That(
                GameFlowSession.RelicSlotCapacity,
                Is.EqualTo(GameFlowSession.MaxRelicSlotCapacity));
        }

        [Test]
        public void RestoreFromSave_ClampsRelicCapacityAndOwnedRelics()
        {
            RunSaveData saved = GameFlowSession.CaptureSave();
            saved.relicSlotCapacity = 99;
            saved.relicIds = new[]
            {
                "R-02",
                "R-03",
                "R-04",
                "R-05",
                "R-06",
                "R-07",
                "R-08",
                "R-09",
            };

            bool restored = GameFlowSession.RestoreFromSave(saved);

            Assert.That(restored, Is.True);
            Assert.That(
                GameFlowSession.RelicSlotCapacity,
                Is.EqualTo(GameFlowSession.MaxRelicSlotCapacity));
            Assert.That(
                GameFlowSession.OwnedRelics.Count,
                Is.EqualTo(GameFlowSession.MaxRelicSlotCapacity));
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
