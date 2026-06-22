using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using SlotRogue.Data.GameFlow;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;
using SlotRogue.Slot.ViewModels;
using SlotRogue.UI.Combat;
using SlotRogue.UI.Combat.Presentation;
using SlotRogue.UI.SlotPresentation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if DOTWEEN
using DG.Tweening;
#endif

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// Owns RunGame battle scene references, asset lifetime, and controller assembly.
    /// Battle order and rules are delegated to <see cref="BattleFlowController"/>.
    /// </summary>
    public class BattleSceneCompositionRoot : MonoBehaviour
    {
        // TEMPORARY TEST HOOK: use this MonsterDefinition to verify SO-driven monster
        // actions and Intent UI. Remove this override when the real encounter selection
        // path supplies MonsterDefinition assets.
        [SerializeField] private MonsterDefinition _devMonsterDefinitionOverride;

        [SerializeField] private EncounterTable _encounterTable;
        [SerializeField] private WaveScheduleDefinition _waveScheduleDefinition;
        [SerializeField] private EncounterBalanceSettings _encounterBalanceSettings;

        [SerializeField] private RunBattleScreenView _view;
        [SerializeField] private FloatingCombatTextLayerView _floatingTextLayerView;
        [SerializeField] private TurnBannerView _turnBannerView;
        [SerializeField] private SlotLeverView _spinLeverView;
        [SerializeField] private SlotMachineFrameView _slotMachineFrameView;
        [SerializeField] private SlotPresentationManager _slotPresentationManager;

        private BattleFlowController _battleFlowController;
        private readonly EncounterSelector _encounterSelector = new();
        private readonly EncounterThemeIndexSelector _themeIndexSelector = new();
        private readonly RunBattleResultRecorder _resultRecorder = new();
        private CancellationTokenSource _battleStartCts;
        private CancellationTokenSource _presentationCts;
        private AsyncOperationHandle<SlotPatternCatalogAsset> _slotPatternCatalogHandle;
        private SlotPatternCatalogAsset _loadedSlotPatternCatalog;
        private readonly AddressableSpriteProvider _slotSymbolSpriteProvider = new(string.Empty);
        private Sprite[] _loadedSlotSymbolSprites;
        private Sprite[] _loadedSlotSpinSymbolSprites;
        private bool _hasSlotPatternCatalogHandle;

        public event Action BattleVictory;

        public event Action BattleDefeat;

        public event Action<BattleTutorialSignal> TutorialSignalRaised;

        public void BeginBattle()
        {
            GameFlowSession.EnsureRunStarted();
            CancelBattleStart();
            CancelPresentation();
            DisposeBattleFlow();

            _battleStartCts = new CancellationTokenSource();
            BeginBattleAsync(_battleStartCts.Token).Forget();
        }

        public Sprite GetDefeatingMonsterPortrait()
        {
            return _view != null ? _view.GetPrimaryEnemyPortrait() : null;
        }

        public bool TryRevive()
        {
            if (!GameFlowSession.CanRevive ||
                _battleFlowController == null)
            {
                return false;
            }

            ApplySlotSymbolSprites();
            if (!_battleFlowController.TryRevivePlayer(GameFlowSession.RevivePlayerHp) ||
                !GameFlowSession.TryRevive())
            {
                return false;
            }

            _resultRecorder.CancelPendingDefeat();
            return true;
        }

        public void FinalizePendingDefeat()
        {
            _resultRecorder.FinalizePendingDefeat();
        }

        public void DevApplyStatusTurn(
            StatusEffectKind statusEffectKind,
            int duration,
            int magnitude,
            StatusStackMode stackMode,
            bool includeDamage,
            int damage,
            int attackCount)
        {
            if (_battleFlowController == null)
            {
                Debug.LogWarning("[BattleSceneCompositionRoot] Battle flow is not initialized.");
                return;
            }

            _battleFlowController.DevApplyStatusTurn(
                statusEffectKind,
                duration,
                magnitude,
                stackMode,
                includeDamage,
                damage,
                attackCount);
        }

        public void SetTutorialSpinBlocked(bool blocked)
        {
            _battleFlowController?.SetSpinInputBlocked(blocked);
        }

        public void SetTutorialTargetSelectionBlocked(bool blocked)
        {
            _battleFlowController?.SetTargetSelectionBlocked(blocked);
        }

        protected virtual void OnDisable()
        {
            _battleStartCts?.Cancel();
            _presentationCts?.Cancel();

#if DOTWEEN
            transform.DOKill(true);
#endif
        }

        protected virtual void OnDestroy()
        {
            CancelBattleStart();
            CancelPresentation();
            DisposeBattleFlow();
            _slotSymbolSpriteProvider.Dispose();
            SlotPatternCatalog.ClearRuntimeCatalogOverride(_loadedSlotPatternCatalog);

            if (_hasSlotPatternCatalogHandle)
            {
                Addressables.Release(_slotPatternCatalogHandle);
                _hasSlotPatternCatalogHandle = false;
            }
        }

        private async UniTask BeginBattleAsync(CancellationToken cancellationToken)
        {
            try
            {
                await EnsureSlotPatternCatalogAsync(cancellationToken);
                await EnsureSlotSymbolSpritesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested || !ValidateSceneReferences())
            {
                return;
            }

            _presentationCts = new CancellationTokenSource();
            ApplySlotSymbolSprites();
            _battleFlowController = CreateBattleFlowController(_presentationCts.Token);
            _battleFlowController.TutorialSignalRaised += HandleTutorialSignalRaised;
            _battleFlowController.BattleCompleted += HandleBattleCompleted;
            _battleFlowController.BeginBattle(CreateBattleFlowContext());
        }

        private BattleFlowController CreateBattleFlowController(CancellationToken cancellationToken)
        {
            var slotViewModel = new SlotMachineViewModel(
                new SlotMachineService(
                    new System.Random(),
                    GameFlowSession.SlotPool,
                    CreateSpinOverride()),
                new SlotPatternResolver(),
                new SlotResultCalculator(),
                new SlotCombatRequestBuilder());
            var spinSequence = new RunBattleSpinSequence(_spinLeverView, _slotMachineFrameView);
            var slotTurnController =
                new SlotTurnController(slotViewModel, spinSequence, _slotPresentationManager);

            var combatViewModel = new CombatViewModel();
            var commands = new CombatPresentationCommandDispatcher(
                _floatingTextLayerView,
                _turnBannerView,
                _view,
                _view);
            var presentationHost = new CombatPresentationHost(gameObject, commands);
            CombatPresentationPipeline pipeline = CombatPresentationPipeline.CreateDefault(presentationHost);
            var presentationController =
                new BattlePresentationController(pipeline, combatViewModel);
            var screenController = new BattleScreenController(_view);

            return new BattleFlowController(
                new BattleSystem(),
                new SlotCombatRequestToCombatEffectsConverter(),
                new RelicTurnResolver(),
                new CombatTurnRequestBuilder(),
                slotTurnController,
                presentationController,
                combatViewModel,
                screenController,
                cancellationToken);
        }

        private BattleFlowContext CreateBattleFlowContext()
        {
            CombatParticipant player = RunCombatParticipantFactory.CreatePlayer(
                GameFlowSession.PlayerMaxHp,
                GameFlowSession.PlayerCurrentHp);
            RunEncounterRoster encounterRoster = CreateEncounterRoster();

            return new BattleFlowContext(
                player,
                encounterRoster,
                GameFlowSession.OwnedRelics,
                GameFlowSession.DamageBonus,
                GameFlowSession.DefenseBonus,
                GameFlowSession.CurrentEncounterTitle);
        }

        private RunEncounterRoster CreateEncounterRoster()
        {
            if (GameFlowSession.IsTutorialRun)
            {
                return CreateTutorialEncounterRoster();
            }

            int battleNumber = GameFlowSession.CurrentBattleNumber;
            WaveSchedule waveSchedule = CreateWaveSchedule();
            WaveResult wave = waveSchedule.Evaluate(battleNumber);
            var buildContext = new EncounterBuildContext(
                wave.EncounterTier,
                battleNumber,
                wave.ThemeSectionIndex);
            EncounterBalanceConfig balanceConfig = CreateEncounterBalanceConfig();

            if (_devMonsterDefinitionOverride != null)
            {
                // TEMPORARY TEST HOOK: this bypasses the tier-based infinite-mode builder
                // only while manually verifying MonsterDefinition/MonsterTurnPattern assets.
                EncounterSelection encounterSelection = CreateDevOverrideSelection(_devMonsterDefinitionOverride);
                return RunEncounterRosterBuilder.Build(encounterSelection, buildContext, balanceConfig);
            }

            if (_encounterTable == null)
            {
                throw new InvalidOperationException(
                    "[BattleSceneCompositionRoot] EncounterTable is missing. " +
                    "Assign an EncounterTable asset before starting battle without Dev Monster Override.");
            }

            int themeGroupIndex = _themeIndexSelector.Select(
                GameFlowSession.RunSeed,
                wave.ThemeSectionIndex,
                _encounterTable.ThemeGroupCount);
            var request = new EncounterSelectionRequest(
                _encounterTable,
                wave.EncounterTier,
                themeGroupIndex,
                GameFlowSession.RunSeed,
                battleNumber);
            EncounterSelection selection = _encounterSelector.Select(request);
            return RunEncounterRosterBuilder.Build(selection, buildContext, balanceConfig);
        }

        private static EncounterSelection CreateDevOverrideSelection(MonsterDefinition definition)
        {
            int formationSlot = EnemyFormationLayout.ResolveSlots(monsterCount: 1)[0];
            return new EncounterSelection(new[]
            {
                new SelectedEncounterMonster(definition, formationSlot),
            });
        }

        private WaveSchedule CreateWaveSchedule()
        {
            if (_waveScheduleDefinition == null)
            {
                throw new InvalidOperationException(
                    "[BattleSceneCompositionRoot] WaveScheduleDefinition is missing. " +
                    "Assign a WaveScheduleDefinition asset before starting battle without Dev Monster Override.");
            }

            if (!_waveScheduleDefinition.TryValidate(out string error))
            {
                throw new InvalidOperationException(
                    $"[BattleSceneCompositionRoot] WaveScheduleDefinition '{_waveScheduleDefinition.name}' is invalid:" +
                    $"{Environment.NewLine}{error}");
            }

            return WaveSchedule.FromDefinition(_waveScheduleDefinition);
        }

        private EncounterBalanceConfig CreateEncounterBalanceConfig()
        {
            if (_encounterBalanceSettings == null)
            {
                throw new InvalidOperationException(
                    "[BattleSceneCompositionRoot] EncounterBalanceSettings is missing. " +
                    "Assign an EncounterBalanceSettings asset before starting battle.");
            }

            return _encounterBalanceSettings.CreateConfig();
        }

        private RunEncounterRoster CreateTutorialEncounterRoster()
        {
            if (_devMonsterDefinitionOverride == null)
            {
                Debug.LogError(
                    "[BattleSceneCompositionRoot] Tutorial mode requires a serialized monster definition.");
                return RunEncounterRosterBuilder.BuildForTier(
                    GameFlowSession.CurrentTier,
                    GameFlowSession.CurrentBattleNumber);
            }

            EnemyEncounterUnit leftEnemy = CreateTutorialEnemy(
                _devMonsterDefinitionOverride,
                TutorialBattleDefinition.LeftMonsterRosterIndex,
                TutorialBattleDefinition.LeftMonsterFormationSlot,
                TutorialBattleDefinition.LeftMonsterMaxHp,
                new[]
                {
                    CreateTutorialActionPlan(
                        actionKey: 1,
                        actionName: "Attack",
                        effectKind: CombatEffectKind.Damage,
                        amount: TutorialBattleDefinition.LeftMonsterAttack,
                        target: CombatEffectTarget.Enemy),
                },
                new EnemyActionPresentationMap(new[]
                {
                    new EnemyActionPresentation(
                        new EnemyActionKey(1),
                        "공격",
                        intentIcon: null),
                }));

            EnemyEncounterUnit rightEnemy = CreateTutorialEnemy(
                _devMonsterDefinitionOverride,
                TutorialBattleDefinition.RightMonsterRosterIndex,
                TutorialBattleDefinition.RightMonsterFormationSlot,
                TutorialBattleDefinition.RightMonsterMaxHp,
                new[]
                {
                    CreateTutorialActionPlan(
                        actionKey: 1,
                        actionName: "Defend",
                        effectKind: CombatEffectKind.Shield,
                        amount: TutorialBattleDefinition.RightMonsterShield,
                        target: CombatEffectTarget.Self),
                    CreateTutorialActionPlan(
                        actionKey: 2,
                        actionName: "Attack",
                        effectKind: CombatEffectKind.Damage,
                        amount: TutorialBattleDefinition.RightMonsterAttack,
                        target: CombatEffectTarget.Enemy),
                },
                new EnemyActionPresentationMap(new[]
                {
                    new EnemyActionPresentation(
                        new EnemyActionKey(1),
                        "방어",
                        intentIcon: null),
                    new EnemyActionPresentation(
                        new EnemyActionKey(2),
                        "공격",
                        intentIcon: null),
                }));

            return new RunEncounterRoster(new[] { leftEnemy, rightEnemy });
        }

        private static Func<SlotSpinResult> CreateSpinOverride()
        {
            if (!GameFlowSession.IsTutorialRun)
            {
                return null;
            }

            int spinIndex = 0;
            return () =>
            {
                return TutorialSlotSpinFactory.CreateSpin(spinIndex++);
            };
        }

        private static EnemyEncounterUnit CreateTutorialEnemy(
            MonsterDefinition definition,
            int rosterIndex,
            int formationSlot,
            int maxHp,
            EnemyActionPlan[] plans,
            EnemyActionPresentationMap presentationMap)
        {
            CombatParticipant participant = RunCombatParticipantFactory.CreateEnemy(
                rosterIndex,
                Mathf.Max(1, maxHp));
            var combatant = new EnemyCombatant(
                participant,
                new FixedSequenceEnemyActionPlanner(plans));
            return new EnemyEncounterUnit(
                combatant,
                definition,
                formationSlot,
                presentationMap);
        }

        private static EnemyActionPlan CreateTutorialActionPlan(
            int actionKey,
            string actionName,
            CombatEffectKind effectKind,
            int amount,
            CombatEffectTarget target)
        {
            return EnemyActionPlan.FromActions(new[]
            {
                new EnemyPlannedAction(
                    new EnemyActionKey(actionKey),
                    actionName,
                    new[]
                    {
                        EnemyActionEffect.FromCombatEffect(new CombatEffect(
                            effectKind,
                            amount,
                            target)),
                    }),
            });
        }

        private bool ValidateSceneReferences()
        {
            EnsureSceneReferences();

            if (_view == null || !_view.EnsureReferences())
            {
                Debug.LogError("[BattleSceneCompositionRoot] Battle screen view is incomplete.");
                return false;
            }

            if (!_view.HasRequiredControls())
            {
                Debug.LogError("[BattleSceneCompositionRoot] Battle action controls are incomplete.");
                return false;
            }

            bool isValid = true;
            isValid &= ValidateReference(_slotPresentationManager, nameof(_slotPresentationManager));
            return isValid;
        }

        private void EnsureSceneReferences()
        {
            _view ??= ResolveSceneComponent<RunBattleScreenView>();
            _floatingTextLayerView ??= ResolveSceneComponent<FloatingCombatTextLayerView>();
            _turnBannerView ??= ResolveSceneComponent<TurnBannerView>();
            _spinLeverView ??= ResolveSceneComponent<SlotLeverView>();
            _slotMachineFrameView ??= ResolveSceneComponent<SlotMachineFrameView>();
            _slotPresentationManager ??= ResolveSceneComponent<SlotPresentationManager>();
        }

        private T ResolveSceneComponent<T>() where T : Component
        {
            return SceneComponentResolver.FindInSceneRoot<T>(transform);
        }

        private bool ValidateReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[BattleSceneCompositionRoot] Required scene reference '{fieldName}' is missing.", this);
            return false;
        }

        private async UniTask EnsureSlotPatternCatalogAsync(CancellationToken cancellationToken)
        {
            if (_loadedSlotPatternCatalog != null)
            {
                SlotPatternCatalog.SetRuntimeCatalogOverride(_loadedSlotPatternCatalog);
                return;
            }

            if (!_hasSlotPatternCatalogHandle)
            {
                _slotPatternCatalogHandle =
                    Addressables.LoadAssetAsync<SlotPatternCatalogAsset>(SlotPatternCatalog.Address);
                _hasSlotPatternCatalogHandle = true;
            }

            while (!_slotPatternCatalogHandle.IsDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (_slotPatternCatalogHandle.Status != AsyncOperationStatus.Succeeded ||
                _slotPatternCatalogHandle.Result == null)
            {
                string reason = _slotPatternCatalogHandle.OperationException?.Message ?? "unknown error";
                Debug.LogError(
                    $"[BattleSceneCompositionRoot] Addressable '{SlotPatternCatalog.Address}' load failed: {reason}. " +
                    "The in-memory default slot pattern catalog will be used.");
                Addressables.Release(_slotPatternCatalogHandle);
                _hasSlotPatternCatalogHandle = false;
                return;
            }

            _loadedSlotPatternCatalog = _slotPatternCatalogHandle.Result;
            SlotPatternCatalog.SetRuntimeCatalogOverride(_loadedSlotPatternCatalog);
        }

        private async UniTask EnsureSlotSymbolSpritesAsync(CancellationToken cancellationToken)
        {
            if (_loadedSlotSymbolSprites != null)
            {
                return;
            }

            _loadedSlotSymbolSprites = await LoadSpriteTableAsync(
                SlotSymbolIconKeys.NormalSpriteKeys,
                "normal slot symbols",
                cancellationToken);
            _loadedSlotSpinSymbolSprites = await LoadSpriteTableAsync(
                SlotSymbolIconKeys.AnimationSpriteKeys,
                "animated slot symbols",
                cancellationToken);
        }

        private async UniTask<Sprite[]> LoadSpriteTableAsync(
            string[] keys,
            string tableName,
            CancellationToken cancellationToken)
        {
            if (keys == null || keys.Length == 0)
            {
                return null;
            }

            var sprites = new Sprite[keys.Length];
            for (int index = 0; index < keys.Length; index++)
            {
                sprites[index] = await _slotSymbolSpriteProvider.LoadAsync(
                    keys[index],
                    cancellationToken);
                if (sprites[index] == null)
                {
                    Debug.LogWarning(
                        $"[BattleSceneCompositionRoot] Addressable {tableName} sprite '{keys[index]}' load failed. " +
                        "The serialized slot symbol sprite table will be kept.");
                    return null;
                }
            }

            return sprites;
        }

        private void ApplySlotSymbolSprites()
        {
            if (_slotPresentationManager == null || _loadedSlotSymbolSprites == null)
            {
                return;
            }

            _slotPresentationManager.SetSymbolSprites(
                _loadedSlotSymbolSprites,
                _loadedSlotSpinSymbolSprites);
        }

        private void HandleBattleCompleted(BattleFlowResult result)
        {
            _resultRecorder.Record(result);

            if (result.EndReason == BattleEndReason.Victory)
            {
                BattleVictory?.Invoke();
                return;
            }

            BattleDefeat?.Invoke();
        }

        private void DisposeBattleFlow()
        {
            if (_battleFlowController == null)
            {
                return;
            }

            _battleFlowController.TutorialSignalRaised -= HandleTutorialSignalRaised;
            _battleFlowController.BattleCompleted -= HandleBattleCompleted;
            _battleFlowController.Dispose();
            _battleFlowController = null;
        }

        private void HandleTutorialSignalRaised(BattleTutorialSignal signal)
        {
            TutorialSignalRaised?.Invoke(signal);
        }

        private void CancelPresentation()
        {
            _presentationCts?.Cancel();
            _presentationCts?.Dispose();
            _presentationCts = null;
        }

        private void CancelBattleStart()
        {
            _battleStartCts?.Cancel();
            _battleStartCts?.Dispose();
            _battleStartCts = null;
        }
    }
}
