using SlotRogue.Data.GameFlow;
using SlotRogue.Relics.Pool;
using SlotRogue.Slot.Data;
using System;
using System.Collections.Generic;

namespace SlotRogue.UI.GameFlow
{
    public static class GameFlowSession
    {
        private const int DefaultPlayerMaxHp = 100;

        /// <summary>저장 포맷 버전. 호환되지 않는 저장본은 복원 시 무시합니다.</summary>
        private const int SaveVersion = 1;

        /// <summary>새 런이 시작될 때 발생합니다(저장본 무효화 등에 사용).</summary>
        public static event Action RunStarted;

        /// <summary>런이 종료될 때 발생합니다(저장본 무효화 등에 사용).</summary>
        public static event Action RunEnded;

        // ── 무한모드 진행 (Infinite Mode) ───────────────────────────────
        // v1 출시 스코프. 맵/노드 없이 인덱스 기반으로 전투를 반복하며,
        // 등급은 CurrentBattleNumber로부터 생성합니다.

        private static readonly WaveSchedule DefaultWaveSchedule = WaveSchedule.CreateDefault();

        /// <summary>
        /// 무한모드 여부. true면 전투는 맵 노드 대신 등급(Tier) 기반으로 적을 구성합니다.
        /// 맵 API는 그대로 두고 이 플래그로만 우회합니다(스토리모드 재연결 대비).
        /// </summary>
        public static bool IsInfiniteMode { get; private set; }

        /// <summary>현재 진행 중인 전투 번호(1-base).</summary>
        public static int CurrentBattleNumber { get; private set; }

        /// <summary>런 동안 Encounter 선택 재현성에 사용하는 시드.</summary>
        public static int RunSeed { get; private set; }

        /// <summary>
        /// 런 동안 유지되는 가변 심볼 풀(가방). 슬롯은 이 풀의 개수에 비례해 심볼을 뽑습니다.
        /// 인스턴스는 불변(식별자 유지)이며 새 런 시작 시 개수만 Reset합니다.
        /// </summary>
        public static SlotSymbolPool SlotPool { get; } = new SlotSymbolPool();

        /// <summary>무한모드 전투 표시용 제목.</summary>
        public static string CurrentEncounterTitle => CurrentTier switch
        {
            EncounterTier.Boss => "BOSS",
            EncounterTier.Elite => "ELITE",
            _ => $"BATTLE {CurrentBattleNumber}",
        };

        /// <summary>현재 전투의 등급.</summary>
        public static EncounterTier CurrentTier =>
            CurrentBattleNumber > 0
                ? DefaultWaveSchedule.Evaluate(CurrentBattleNumber).EncounterTier
                : EncounterTier.Normal;

        /// <summary>현재(직전) 전투가 유물 보상을 주는가 (엘리트/보스만).</summary>
        public static bool CurrentBattleGrantsArtifact =>
            CurrentTier is EncounterTier.Elite or EncounterTier.Boss;

        /// <summary>다음 전투로 진행합니다. 보상/회복 처리가 끝난 뒤 호출합니다.</summary>
        public static void AdvanceToNextBattle()
        {
            EnsureRunStarted();
            CurrentBattleNumber++;
        }

        /// <summary>
        /// 무한모드 전투 승리 처리. HP를 반영하고 승리 수를 올린 뒤,
        /// 일반 전투면 소량 회복합니다. (엘리트/보스 보상은 유물 선택에서 별도 처리)
        /// </summary>
        public static void CompleteInfiniteVictory(int remainingPlayerHp)
        {
            CompleteBattleVictory(remainingPlayerHp);

            if (CurrentTier == EncounterTier.Normal)
            {
                PlayerCurrentHp = Math.Min(
                    PlayerCurrentHp + RewardEconomy.NormalWinHeal, PlayerMaxHp);
            }
        }

        public static bool HasRun { get; private set; }

        public static bool IsTutorialRun { get; private set; }

        public static int PlayerMaxHp { get; private set; }

        public static int PlayerCurrentHp { get; private set; }

        public static int BattleIndex { get; private set; }

        public static int Victories { get; private set; }

        public static int RewardsClaimed { get; private set; }

        public static int DamageBonus { get; private set; }

        public static int DefenseBonus { get; private set; }

        public static bool HasRevivedThisRun { get; private set; }

        public static bool IsDefeatPending { get; private set; }

        public static bool CanRevive =>
            HasRun &&
            IsDefeatPending &&
            !HasRevivedThisRun;

        public static int RevivePlayerHp => Math.Max(1, (PlayerMaxHp + 1) / 2);

        // ── v23 유물 인벤토리 ────────────────────────────────────────────
        // 시작 유물 + 보상으로 획득한 유물을 누적한다. 전투마다 RelicTurnResolver가 소비한다.
        // 보유 로직은 인스턴스 클래스 RelicInventory에 위임한다(책임 분리 + 테스트 가능).
        private static readonly RelicInventory _inventory = new();
        private static readonly RelicContributionAccumulator _relicContributions = new();
        private static readonly SlotSymbolContributionAccumulator _slotSymbolContributions = new();

        /// <summary>이 런에서 보유한 v23 유물 목록(시작 유물 포함).</summary>
        public static IReadOnlyList<RelicDefinition> OwnedRelics => _inventory.Owned;

        public static bool HasStarterRelic => _inventory.HasStarter;

        /// <summary>시작 유물은 런에 하나만 유지합니다.</summary>
        public static bool SelectStarterRelic(RelicDefinition relic)
        {
            EnsureRunStarted();
            return _inventory.SelectStarter(relic);
        }

        /// <summary>v23 유물을 보유 목록에 추가합니다(중복 허용 — 동일 유물 누적 가능).</summary>
        public static void AddRelic(RelicDefinition relic)
        {
            EnsureRunStarted();
            _inventory.Add(relic);
        }

        public static void StartNewRun()
        {
            StartRun(isTutorialRun: false);
        }

        public static void StartTutorialRun()
        {
            StartRun(isTutorialRun: true);
        }

        private static void StartRun(bool isTutorialRun)
        {
            HasRun = true;
            IsTutorialRun = isTutorialRun;
            IsInfiniteMode = true; // v1: 무한모드. 스토리모드 재개 시 별도 진입점에서 false 설정.
            PlayerMaxHp = isTutorialRun
                ? TutorialBattleDefinition.PlayerMaxHp
                : DefaultPlayerMaxHp;
            PlayerCurrentHp = PlayerMaxHp;
            BattleIndex = 0;
            CurrentBattleNumber = 1;
            RunSeed = GenerateRunSeed();
            Victories = 0;
            RewardsClaimed = 0;
            DamageBonus = 0;
            DefenseBonus = 0;
            HasRevivedThisRun = false;
            IsDefeatPending = false;
            _inventory.Clear();
            _relicContributions.Clear();
            _slotSymbolContributions.Clear();
            SlotPool.Reset();

            if (isTutorialRun)
            {
                _inventory.Add(TutorialBattleDefinition.TrainingBatteryRelic);
            }

            RunStarted?.Invoke();
        }

        public static void EnsureRunStarted()
        {
            if (!HasRun)
            {
                StartNewRun();
            }
        }

        public static void CompleteBattleVictory(int remainingPlayerHp)
        {
            EnsureRunStarted();
            IsDefeatPending = false;
            PlayerCurrentHp = Math.Max(1, Math.Min(remainingPlayerHp, PlayerMaxHp));
            Victories++;
        }

        public static void BeginBattleDefeat()
        {
            EnsureRunStarted();
            IsDefeatPending = true;
            PlayerCurrentHp = 0;
        }

        public static bool TryRevive()
        {
            if (!CanRevive)
            {
                return false;
            }

            HasRevivedThisRun = true;
            IsDefeatPending = false;
            PlayerCurrentHp = RevivePlayerHp;
            return true;
        }

        public static void CompleteBattleDefeat()
        {
            EndRun();
        }

        public static void EndRun()
        {
            IsDefeatPending = false;
            HasRun = false;
            IsTutorialRun = false;
            RunEnded?.Invoke();
        }

        /// <summary>
        /// 저장/복원 가능한 상태인지 여부. 패배 대기(HP 0)나 튜토리얼은 저장하지 않습니다.
        /// </summary>
        public static bool IsResumable =>
            HasRun && !IsTutorialRun && !IsDefeatPending && PlayerCurrentHp > 0;

        /// <summary>현재 런 상태를 직렬화용 스냅샷으로 캡처합니다.</summary>
        public static RunSaveData CaptureSave()
        {
            IReadOnlyList<SlotSymbolType> symbols = SlotSymbolPool.Symbols;
            var symbolTypes = new int[symbols.Count];
            var symbolCounts = new int[symbols.Count];
            for (int index = 0; index < symbols.Count; index++)
            {
                symbolTypes[index] = (int)symbols[index];
                symbolCounts[index] = SlotPool.GetCount(symbols[index]);
            }

            IReadOnlyList<RelicDefinition> relics = _inventory.Owned;
            var relicIds = new string[relics.Count];
            for (int index = 0; index < relics.Count; index++)
            {
                relicIds[index] = relics[index].Id;
            }

            return new RunSaveData
            {
                version = SaveVersion,
                isTutorialRun = IsTutorialRun,
                isInfiniteMode = IsInfiniteMode,
                playerMaxHp = PlayerMaxHp,
                playerCurrentHp = PlayerCurrentHp,
                battleIndex = BattleIndex,
                currentBattleNumber = CurrentBattleNumber,
                runSeed = RunSeed,
                victories = Victories,
                rewardsClaimed = RewardsClaimed,
                damageBonus = DamageBonus,
                defenseBonus = DefenseBonus,
                hasRevivedThisRun = HasRevivedThisRun,
                relicIds = relicIds,
                symbolTypes = symbolTypes,
                symbolCounts = symbolCounts,
            };
        }

        /// <summary>
        /// 저장 스냅샷에서 런을 복원합니다. 포맷 불일치/무효 데이터면 false를 반환하고 상태를 바꾸지 않습니다.
        /// 카탈로그에서 사라진 유물 Id는 건너뜁니다(앱 업데이트 호환).
        /// </summary>
        public static bool RestoreFromSave(RunSaveData data)
        {
            if (data == null ||
                data.version != SaveVersion ||
                data.playerCurrentHp <= 0 ||
                data.currentBattleNumber < 1)
            {
                return false;
            }

            HasRun = true;
            IsTutorialRun = data.isTutorialRun;
            IsInfiniteMode = data.isInfiniteMode;
            PlayerMaxHp = Math.Max(1, data.playerMaxHp);
            PlayerCurrentHp = Math.Min(Math.Max(1, data.playerCurrentHp), PlayerMaxHp);
            BattleIndex = Math.Max(0, data.battleIndex);
            CurrentBattleNumber = Math.Max(1, data.currentBattleNumber);
            RunSeed = data.runSeed;
            Victories = Math.Max(0, data.victories);
            RewardsClaimed = Math.Max(0, data.rewardsClaimed);
            DamageBonus = Math.Max(0, data.damageBonus);
            DefenseBonus = Math.Max(0, data.defenseBonus);
            HasRevivedThisRun = data.hasRevivedThisRun;
            IsDefeatPending = false;

            _inventory.Clear();
            if (data.relicIds != null)
            {
                for (int index = 0; index < data.relicIds.Length; index++)
                {
                    RelicDefinition relic = RelicCatalog.GetById(data.relicIds[index]);
                    if (relic != null)
                    {
                        _inventory.Add(relic);
                    }
                }
            }

            _relicContributions.Clear();
            _slotSymbolContributions.Clear();

            SlotPool.Reset();
            if (data.symbolTypes != null &&
                data.symbolCounts != null &&
                data.symbolTypes.Length == data.symbolCounts.Length)
            {
                for (int index = 0; index < data.symbolTypes.Length; index++)
                {
                    SlotPool.SetCount(
                        (SlotSymbolType)data.symbolTypes[index],
                        data.symbolCounts[index]);
                }
            }

            return true;
        }

        public static void ApplyReward(RunRewardType rewardType)
        {
            EnsureRunStarted();

            RunVitals applied = RewardEconomy.Apply(
                new RunVitals(PlayerMaxHp, PlayerCurrentHp, DamageBonus, DefenseBonus),
                rewardType);
            PlayerMaxHp = applied.MaxHp;
            PlayerCurrentHp = applied.CurrentHp;
            DamageBonus = applied.DamageBonus;
            DefenseBonus = applied.DefenseBonus;

            RewardsClaimed++;
        }

        /// <summary>슬롯 풀에 심볼을 추가하는 보상을 적용합니다.</summary>
        public static void ApplySymbolReward(SlotSymbolType symbol, int amount)
        {
            EnsureRunStarted();
            SlotPool.Add(symbol, amount);
            RewardsClaimed++;
        }

        /// <summary>유물 보상 선택을 보상 카운트에 반영합니다(유물 추가는 AddRelic이 담당).</summary>
        public static void MarkRewardClaimed()
        {
            EnsureRunStarted();
            RewardsClaimed++;
        }

        public static void RecordRelicContributions(
            IReadOnlyList<RelicContributionSnapshot> contributions)
        {
            _relicContributions.Add(contributions);
        }

        public static void RecordSlotSymbolContributions(
            IReadOnlyList<SlotSymbolContributionSnapshot> contributions)
        {
            _slotSymbolContributions.Add(contributions);
        }

        public static IReadOnlyList<RelicContributionSnapshot>
            GetRelicContributionSummary()
        {
            return _relicContributions.SnapshotForRelics(_inventory.Owned);
        }

        public static IReadOnlyList<SlotSymbolContributionSnapshot>
            GetSlotSymbolContributionSummary()
        {
            return _slotSymbolContributions.SnapshotForSymbols(SlotSymbolPool.Symbols);
        }

        private static int GenerateRunSeed()
        {
            // TickCount는 해상도가 낮아 짧은 간격에 두 런이 같은 시드를 받을 수 있다.
            // Guid 해시는 호출마다 충돌 없이 분산된 시드를 보장한다.
            return Guid.NewGuid().GetHashCode();
        }
    }
}
