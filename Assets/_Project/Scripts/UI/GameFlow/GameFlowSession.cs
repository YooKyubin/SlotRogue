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

        public const int BaseSpinCoinReward = 1;
        public const int BaseSwapCountPerPlayerTurn = 1;
        public const int DefaultRelicSlotCapacity = 5;
        public const int MaxRelicSlotCapacity = 7;

        /// <summary>새 런이 시작될 때 발생합니다(저장본 무효화 등에 사용).</summary>
        public static event Action RunStarted;

        /// <summary>런이 종료될 때 발생합니다(저장본 무효화 등에 사용).</summary>
        public static event Action RunEnded;

        public static event Action<int> RunCoinsChanged;

        // ── 무한모드 진행 (Infinite Mode) ───────────────────────────────
        // v1 출시 스코프. 맵/노드 없이 인덱스 기반으로 전투를 반복하며,
        // 등급은 CurrentBattleNumber로부터 생성합니다.

        private static readonly WaveSchedule DefaultWaveSchedule = WaveSchedule.CreateDefault();
        private static readonly Dictionary<SlotSymbolType, int> InitialSymbolWeights =
            BuildDefaultInitialSymbolWeights();
        private static readonly Dictionary<SlotSymbolType, int> InitialSymbolBaseDamage =
            BuildDefaultInitialSymbolBaseDamage();
        private static readonly Dictionary<SlotSymbolType, int> SymbolBaseDamageBonuses =
            BuildDefaultSymbolBaseDamageBonuses();
        private static bool _restoredFromSave;

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
        /// 런 동안 유지되는 심볼별 한 칸 출현 확률값 테이블.
        /// 인스턴스는 불변(식별자 유지)이며 새 런 시작 시 가중치만 Reset합니다.
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

        public static int RunCoins { get; private set; }

        public static int RelicSlotCapacity { get; private set; } = DefaultRelicSlotCapacity;

        public static bool HasRevivedThisRun { get; private set; }

        public static bool IsDefeatPending { get; private set; }

        public static bool CanRevive =>
            HasRun &&
            IsDefeatPending &&
            !HasRevivedThisRun;

        public static int RevivePlayerHp => Math.Max(1, (PlayerMaxHp + 1) / 2);

        public static int SwapCountPerPlayerTurn =>
            BaseSwapCountPerPlayerTurn +
            RelicSpecRunner.ResolveRuleModifier(BuildOwnedSpecs(), RelicEffectKind.SwapCountDelta);

        /// <summary>보유한 상점 할인 유물(R-27/R-28 등)이 주는 유물 가격 할인 합.</summary>
        public static int ShopDiscount =>
            RelicSpecRunner.ResolveRuleModifier(BuildOwnedSpecs(), RelicEffectKind.ShopDiscount);

        public static int AddSpinCoins()
        {
            EnsureRunStarted();
            SetRunCoins(RunCoins + BaseSpinCoinReward);
            return BaseSpinCoinReward;
        }

        public static bool TrySpendRunCoins(int amount)
        {
            EnsureRunStarted();
            int cost = Math.Max(0, amount);
            if (RunCoins < cost)
            {
                return false;
            }

            SetRunCoins(RunCoins - cost);
            return true;
        }

        public static void AddRunCoins(int amount)
        {
            EnsureRunStarted();
            SetRunCoins(RunCoins + Math.Max(0, amount));
        }

        private static void SetRunCoins(int value)
        {
            int clamped = Math.Max(0, value);
            if (RunCoins == clamped)
            {
                return;
            }

            RunCoins = clamped;
            RunCoinsChanged?.Invoke(RunCoins);
        }

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

        /// <summary>유물을 보유 목록에 추가합니다. 점유 유물은 현재 슬롯 수를 넘을 수 없습니다.</summary>
        public static void AddRelic(RelicDefinition relic)
        {
            TryAddRelic(relic);
        }

        public static bool TryAddRelic(RelicDefinition relic)
        {
            EnsureRunStarted();
            if (!CanAddRelic(relic))
            {
                return false;
            }

            _inventory.Add(relic);
            return true;
        }

        public static bool CanAddRelic(RelicDefinition relic)
        {
            EnsureRunStarted();
            if (relic == null)
            {
                return false;
            }

            return relic.OccupiesSlot &&
                _inventory.Owned.Count < RelicSlotCapacity;
        }

        public static bool TryIncreaseRelicSlotCapacity(int amount)
        {
            EnsureRunStarted();
            int before = RelicSlotCapacity;
            RelicSlotCapacity = Math.Min(
                MaxRelicSlotCapacity,
                Math.Max(DefaultRelicSlotCapacity, RelicSlotCapacity + Math.Max(0, amount)));
            return RelicSlotCapacity > before;
        }

        private static readonly List<RelicSpec> _proposalSpecs = new();

        /// <summary>제안(처치 보상)으로 획득한 영구 엔진 효과 스펙. 유물과 함께 전투 엔진이 소비한다.</summary>
        public static IReadOnlyList<RelicSpec> ProposalSpecs => _proposalSpecs;

        /// <summary>엔진 효과 제안을 픽했을 때 스펙을 이 런 동안 영구 누적한다.</summary>
        public static void AddProposalSpec(RelicSpec spec)
        {
            if (spec != null)
            {
                _proposalSpecs.Add(spec);
            }
        }

        /// <summary>보유 유물을 v29 명세(<see cref="RelicSpec"/>) 목록으로 변환한다(엔진 입력용).</summary>
        private static IReadOnlyList<RelicSpec> BuildOwnedSpecs()
        {
            IReadOnlyList<RelicDefinition> owned = _inventory.Owned;
            var specs = new List<RelicSpec>(owned.Count);
            for (int index = 0; index < owned.Count; index++)
            {
                RelicSpec spec = RelicSpecCatalog.GetById(owned[index]?.Id);
                if (spec != null)
                {
                    specs.Add(spec);
                }
            }

            return specs;
        }

        /// <summary>전투 시작 트리거(OnBattleStart) 유물의 별조각 획득을 적용한다(R-21 등).</summary>
        public static void ApplyBattleStartRelicCoins()
        {
            int coins = RelicSpecRunner.ResolveEventCoins(
                BuildOwnedSpecs(),
                RelicTrigger.OnBattleStart,
                new RelicRuntimeContext(false, false, RunCoins, 0, false, 0));
            if (coins != 0)
            {
                AddRunCoins(coins);
            }
        }

        /// <summary>처치 트리거(OnKill) 유물의 회복을 적용한다(R-33 등). 승리로 HP가 정산된 뒤 호출한다.</summary>
        public static void ApplyKillRelicHeal()
        {
            int heal = RelicSpecRunner.ResolveEventHeal(
                BuildOwnedSpecs(),
                RelicTrigger.OnKill,
                new RelicRuntimeContext(false, false, RunCoins, 0, false, 0));
            if (heal > 0)
            {
                PlayerCurrentHp = Math.Min(PlayerCurrentHp + heal, PlayerMaxHp);
            }
        }

        /// <summary>웨이브(전투)가 지나면 소멸·웨이브 유물의 수명을 감소시키고 만료분을 제거한다.</summary>
        public static void TickRelicWaveLifetimes() => _inventory.TickWaveLifetimes();

        public static int CountOwnedRelic(string relicId)
        {
            if (string.IsNullOrEmpty(relicId))
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<RelicDefinition> owned = _inventory.Owned;
            for (int index = 0; index < owned.Count; index++)
            {
                if (owned[index] != null && owned[index].Id == relicId)
                {
                    count++;
                }
            }

            return count;
        }

        public static void ConfigureInitialSymbolWeights(
            int cherry,
            int lemon,
            int clover,
            int bell,
            int diamond,
            int seven)
        {
            SetInitialSymbolWeight(SlotSymbolType.Cherry, cherry);
            SetInitialSymbolWeight(SlotSymbolType.Lemon, lemon);
            SetInitialSymbolWeight(SlotSymbolType.Clover, clover);
            SetInitialSymbolWeight(SlotSymbolType.Bell, bell);
            SetInitialSymbolWeight(SlotSymbolType.Diamond, diamond);
            SetInitialSymbolWeight(SlotSymbolType.Seven, seven);
        }

        public static void ConfigureInitialSlotPoolCounts(
            int cherry,
            int lemon,
            int clover,
            int bell,
            int diamond,
            int seven)
        {
            ConfigureInitialSymbolWeights(cherry, lemon, clover, bell, diamond, seven);
        }

        public static void ConfigureInitialSymbolBaseDamage(
            int cherry,
            int lemon,
            int clover,
            int bell,
            int diamond,
            int seven)
        {
            SetInitialSymbolBaseDamage(SlotSymbolType.Cherry, cherry);
            SetInitialSymbolBaseDamage(SlotSymbolType.Lemon, lemon);
            SetInitialSymbolBaseDamage(SlotSymbolType.Clover, clover);
            SetInitialSymbolBaseDamage(SlotSymbolType.Bell, bell);
            SetInitialSymbolBaseDamage(SlotSymbolType.Diamond, diamond);
            SetInitialSymbolBaseDamage(SlotSymbolType.Seven, seven);
            ApplyConfiguredSymbolBaseDamage();
        }

        public static void ApplyInitialSymbolWeightConfigurationToCurrentRunIfFresh()
        {
            if (!CanApplyInitialSymbolWeightConfigurationToCurrentRun())
            {
                return;
            }

            ApplyConfiguredInitialSymbolWeights();
        }

        public static void ApplyInitialSlotPoolConfigurationToCurrentRunIfFresh()
        {
            ApplyInitialSymbolWeightConfigurationToCurrentRunIfFresh();
        }

        public static void StartNewRun()
        {
            StartRun(isTutorialRun: false);
        }

        public static void StartTutorialRun()
        {
            StartRun(isTutorialRun: true);
        }

        public static void CompleteTutorialAndContinueAsNormalRun()
        {
            EnsureRunStarted();
            if (!IsTutorialRun)
            {
                return;
            }

            IsTutorialRun = false;
            PlayerMaxHp = DefaultPlayerMaxHp;
            PlayerCurrentHp = PlayerMaxHp;
            IsDefeatPending = false;
        }

        private static void StartRun(bool isTutorialRun)
        {
            HasRun = true;
            IsTutorialRun = isTutorialRun;
            IsInfiniteMode = true; // v1: 무한모드. 스토리모드 재개 시 별도 진입점에서 false 설정.
            PlayerMaxHp = DefaultPlayerMaxHp;
            PlayerCurrentHp = PlayerMaxHp;
            BattleIndex = 0;
            CurrentBattleNumber = 1;
            RunSeed = GenerateRunSeed();
            Victories = 0;
            RewardsClaimed = 0;
            DamageBonus = 0;
            DefenseBonus = 0;
            SetRunCoins(0);
            RelicSlotCapacity = DefaultRelicSlotCapacity;
            HasRevivedThisRun = false;
            IsDefeatPending = false;
            _restoredFromSave = false;
            _inventory.Clear();
            _proposalSpecs.Clear();
            _relicContributions.Clear();
            _slotSymbolContributions.Clear();
            ApplyConfiguredInitialSymbolWeights();
            ResetSymbolBaseDamageBonuses();
            ApplyConfiguredSymbolBaseDamage();

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
            _restoredFromSave = false;
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
            var symbolBaseDamageBonuses = new int[symbols.Count];
            for (int index = 0; index < symbols.Count; index++)
            {
                symbolTypes[index] = (int)symbols[index];
                symbolCounts[index] = SlotPool.GetWeight(symbols[index]);
                symbolBaseDamageBonuses[index] =
                    GetSymbolBaseDamageBonus(symbols[index]);
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
                runCoins = RunCoins,
                relicSlotCapacity = RelicSlotCapacity,
                hasRevivedThisRun = HasRevivedThisRun,
                relicIds = relicIds,
                symbolTypes = symbolTypes,
                symbolBaseDamageBonuses = symbolBaseDamageBonuses,
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
            SetRunCoins(data.runCoins);
            RelicSlotCapacity = data.relicSlotCapacity > 0
                ? Math.Min(MaxRelicSlotCapacity, Math.Max(DefaultRelicSlotCapacity, data.relicSlotCapacity))
                : DefaultRelicSlotCapacity;
            HasRevivedThisRun = data.hasRevivedThisRun;
            IsDefeatPending = false;
            _restoredFromSave = true;

            _inventory.Clear();
            if (data.relicIds != null)
            {
                for (int index = 0; index < data.relicIds.Length; index++)
                {
                    RelicDefinition relic = RelicCatalog.GetById(data.relicIds[index]);
                    if (relic != null &&
                        relic.OccupiesSlot &&
                        _inventory.Owned.Count < RelicSlotCapacity)
                    {
                        _inventory.Add(relic);
                    }
                }
            }

            _relicContributions.Clear();
            _slotSymbolContributions.Clear();
            ResetSymbolBaseDamageBonuses();
            RestoreSymbolBaseDamageBonuses(
                data.symbolTypes,
                data.symbolBaseDamageBonuses);
            ApplyConfiguredSymbolBaseDamage();

            SlotPool.Reset();
            if (data.symbolTypes != null &&
                data.symbolCounts != null &&
                data.symbolTypes.Length == data.symbolCounts.Length)
            {
                for (int index = 0; index < data.symbolTypes.Length; index++)
                {
                    SlotPool.SetWeight(
                        (SlotSymbolType)data.symbolTypes[index],
                        data.symbolCounts[index]);
                }
            }

            return true;
        }

        private static Dictionary<SlotSymbolType, int> BuildDefaultInitialSymbolWeights()
        {
            var weights = new Dictionary<SlotSymbolType, int>();
            IReadOnlyList<SlotSymbolType> symbols = SlotSymbolPool.Symbols;
            for (int index = 0; index < symbols.Count; index++)
            {
                SlotSymbolType symbol = symbols[index];
                weights[symbol] = SlotSymbolPool.DefaultWeightFor(symbol);
            }

            return weights;
        }

        private static Dictionary<SlotSymbolType, int> BuildDefaultInitialSymbolBaseDamage()
        {
            var values = new Dictionary<SlotSymbolType, int>();
            IReadOnlyList<SlotSymbolType> symbols = SlotSymbolPool.Symbols;
            for (int index = 0; index < symbols.Count; index++)
            {
                SlotSymbolType symbol = symbols[index];
                values[symbol] = DefaultBaseDamageFor(symbol);
            }

            return values;
        }

        private static Dictionary<SlotSymbolType, int> BuildDefaultSymbolBaseDamageBonuses()
        {
            var values = new Dictionary<SlotSymbolType, int>();
            IReadOnlyList<SlotSymbolType> symbols = SlotSymbolPool.Symbols;
            for (int index = 0; index < symbols.Count; index++)
            {
                values[symbols[index]] = 0;
            }

            return values;
        }

        private static void SetInitialSymbolWeight(SlotSymbolType symbol, int weight)
        {
            InitialSymbolWeights[symbol] = Math.Max(0, weight);
        }

        private static void SetInitialSymbolBaseDamage(SlotSymbolType symbol, int value)
        {
            InitialSymbolBaseDamage[symbol] = Math.Max(0, value);
        }

        private static void ApplyConfiguredInitialSymbolWeights()
        {
            SlotPool.Reset();
            IReadOnlyList<SlotSymbolType> symbols = SlotSymbolPool.Symbols;
            for (int index = 0; index < symbols.Count; index++)
            {
                SlotSymbolType symbol = symbols[index];
                int weight = InitialSymbolWeights.TryGetValue(symbol, out int configured)
                    ? configured
                    : SlotSymbolPool.DefaultWeightFor(symbol);
                SlotPool.SetWeight(symbol, weight);
            }
        }

        private static void ApplyConfiguredSymbolBaseDamage()
        {
            SlotSymbolAttackValues.Configure(
                BaseDamageWithBonus(SlotSymbolType.Cherry),
                BaseDamageWithBonus(SlotSymbolType.Lemon),
                BaseDamageWithBonus(SlotSymbolType.Clover),
                BaseDamageWithBonus(SlotSymbolType.Bell),
                BaseDamageWithBonus(SlotSymbolType.Diamond),
                BaseDamageWithBonus(SlotSymbolType.Seven));
        }

        private static int BaseDamageWithBonus(SlotSymbolType symbol)
        {
            int baseValue = InitialSymbolBaseDamage.TryGetValue(symbol, out int configured)
                ? configured
                : DefaultBaseDamageFor(symbol);
            int bonus = SymbolBaseDamageBonuses.TryGetValue(symbol, out int configuredBonus)
                ? configuredBonus
                : 0;
            return Math.Max(0, baseValue + bonus);
        }

        private static void ResetSymbolBaseDamageBonuses()
        {
            IReadOnlyList<SlotSymbolType> symbols = SlotSymbolPool.Symbols;
            for (int index = 0; index < symbols.Count; index++)
            {
                SymbolBaseDamageBonuses[symbols[index]] = 0;
            }
        }

        private static int GetSymbolBaseDamageBonus(SlotSymbolType symbol)
        {
            return SymbolBaseDamageBonuses.TryGetValue(symbol, out int bonus)
                ? Math.Max(0, bonus)
                : 0;
        }

        private static void RestoreSymbolBaseDamageBonuses(
            int[] symbolTypes,
            int[] bonuses)
        {
            if (symbolTypes == null ||
                bonuses == null ||
                symbolTypes.Length != bonuses.Length)
            {
                return;
            }

            for (int index = 0; index < symbolTypes.Length; index++)
            {
                SlotSymbolType symbol = (SlotSymbolType)symbolTypes[index];
                if (!IsKnownSlotSymbol(symbol))
                {
                    continue;
                }

                SymbolBaseDamageBonuses[symbol] = Math.Max(0, bonuses[index]);
            }
        }

        private static bool IsKnownSlotSymbol(SlotSymbolType symbol)
        {
            IReadOnlyList<SlotSymbolType> symbols = SlotSymbolPool.Symbols;
            for (int index = 0; index < symbols.Count; index++)
            {
                if (symbols[index] == symbol)
                {
                    return true;
                }
            }

            return false;
        }

        private static int DefaultBaseDamageFor(SlotSymbolType symbol) => symbol switch
        {
            SlotSymbolType.Cherry => SlotSymbolAttackValues.DefaultCherryDamage,
            SlotSymbolType.Lemon => SlotSymbolAttackValues.DefaultLemonDamage,
            SlotSymbolType.Clover => SlotSymbolAttackValues.DefaultCloverDamage,
            SlotSymbolType.Bell => SlotSymbolAttackValues.DefaultBellDamage,
            SlotSymbolType.Diamond => SlotSymbolAttackValues.DefaultDiamondDamage,
            SlotSymbolType.Seven => SlotSymbolAttackValues.DefaultSevenDamage,
            _ => 0,
        };

        private static bool CanApplyInitialSymbolWeightConfigurationToCurrentRun()
        {
            return HasRun &&
                !_restoredFromSave &&
                CurrentBattleNumber == 1 &&
                Victories == 0 &&
                RewardsClaimed == 0 &&
                DamageBonus == 0 &&
                DefenseBonus == 0 &&
                !HasRevivedThisRun &&
                !IsDefeatPending;
        }

        public static void ApplyReward(RunRewardType rewardType)
        {
            EnsureRunStarted();

            if (rewardType == RunRewardType.RunCoins)
            {
                AddRunCoins(RewardEconomy.RunCoinRewardAmount);
                RewardsClaimed++;
                return;
            }

            RunVitals applied = RewardEconomy.Apply(
                new RunVitals(PlayerMaxHp, PlayerCurrentHp, DamageBonus, DefenseBonus),
                rewardType);
            PlayerMaxHp = applied.MaxHp;
            PlayerCurrentHp = applied.CurrentHp;
            DamageBonus = applied.DamageBonus;
            DefenseBonus = applied.DefenseBonus;

            RewardsClaimed++;
        }

        /// <summary>심볼별 한 칸 출현 확률값을 변경하는 보상을 적용합니다.</summary>
        public static void ApplySymbolReward(SlotSymbolType symbol, int amount)
        {
            ApplySymbolWeightReward(symbol, amount);
        }

        public static void ApplySymbolWeightReward(SlotSymbolType symbol, int amount)
        {
            EnsureRunStarted();
            SlotPool.AddWeight(symbol, amount);
            RewardsClaimed++;
        }

        public static void ApplySymbolWeightReward(
            IReadOnlyList<SlotSymbolType> symbols,
            int amount,
            bool countRewardClaim = true)
        {
            EnsureRunStarted();
            if (symbols != null)
            {
                for (int index = 0; index < symbols.Count; index++)
                {
                    SlotPool.AddWeight(symbols[index], amount);
                }
            }

            if (countRewardClaim)
            {
                RewardsClaimed++;
            }
        }

        public static void ApplySymbolBaseDamageReward(
            IReadOnlyList<SlotSymbolType> symbols,
            int amount,
            bool countRewardClaim = true)
        {
            EnsureRunStarted();
            if (symbols != null)
            {
                for (int index = 0; index < symbols.Count; index++)
                {
                    SlotSymbolType symbol = symbols[index];
                    int current = SymbolBaseDamageBonuses.TryGetValue(symbol, out int bonus)
                        ? bonus
                        : 0;
                    SymbolBaseDamageBonuses[symbol] = Math.Max(0, current + amount);
                }
            }

            ApplyConfiguredSymbolBaseDamage();
            if (countRewardClaim)
            {
                RewardsClaimed++;
            }
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
