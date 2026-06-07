using System.Collections.Generic;
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
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleCompositionRoot : MonoBehaviour
    {
        private readonly BattleSystem _battle = new();

        // 슬롯은 런별 가변 심볼 풀(GameFlowSession.SlotPool)에서 개수 비례로 뽑는다.
        private readonly SlotMachineViewModel _slotViewModel = new(
            new SlotMachineService(new System.Random(), GameFlowSession.SlotPool),
            new SlotPatternResolver(),
            new SlotResultCalculator(),
            new SlotCombatRequestBuilder());
        private readonly SlotPatternResolver _presentationPatternResolver = new();
        private readonly SlotCombatRequestToCombatEffectsConverter _converter = new();
        private readonly CombatEventConsoleLogger _eventLogger = new();
        private readonly RunCombatRequestResolver _requestResolver = new();
        private readonly CombatViewModel _combatViewModel = new();
        private readonly RunBattleScreenViewModel _screenViewModel = new();

        private BattleFlowController _flowController;
        private CombatPresentationHost _presentationHost;
        private CancellationTokenSource _presentationCts;
        private RunBattleSpinSequence _spinSequence;
        private RunBattleScreenStateUpdater _stateUpdater;

        [SerializeField] private RunBattleScreenView _view;
        [SerializeField] private FloatingDamageTextView _floatingDamageTextPrefab;
        [SerializeField] private SlotLeverView _spinLeverView;
        [SerializeField] private SlotMachineFrameView _slotMachineFrameView;
        [SerializeField] private SlotPresentationManager _slotPresentationManager;

        private RunCombatRequestResult _lastRequestResult;
        private bool _battleCompleted;
        private bool _spinRoutineRunning;
        private CombatParticipantId _selectedEnemyId;
        private RunEncounterRoster _encounterRoster;

        /// <summary>전투 승리 시 BattleView가 구독합니다.</summary>
        public event System.Action BattleVictory;

        /// <summary>전투 패배 시 BattleView가 구독합니다.</summary>
        public event System.Action BattleDefeat;

        private void Awake()
        {
            // 단일 씬 구조에서는 BeginBattle()이 Navigator에 의해 명시적으로 호출됩니다.
            // Awake에서는 씬 레퍼런스만 확보합니다.
            ResolveSceneReferences();
        }

        /// <summary>
        /// BattleView(OnEnter)가 전투 화면에 진입할 때 호출합니다.
        /// 매 전투마다 상태를 초기화하고 전투를 시작합니다.
        /// </summary>
        public void BeginBattle()
        {
            GameFlowSession.EnsureRunStarted();

            ResolveSceneReferences();

            if (_view == null || !_view.EnsureReferences())
            {
                Debug.LogError("[RunBattleCompositionRoot] RunBattleScreenView is missing required child views.");
                return;
            }

            if (!_view.HasRequiredControls())
            {
                Debug.LogError("[RunBattleCompositionRoot] RunBattle action controls are incomplete.");
                return;
            }

            // 중복 구독 방지: 재진입 시 기존 구독 제거
            _screenViewModel.Changed -= HandleScreenStateChanged;
            _view.SpinRequested -= HandleSpinClicked;
            _view.ContinueRequested -= NavigateToReward;
            _view.RestartRequested -= NavigateToStart;

            // 이전 전투 취소토큰 정리
            _presentationCts?.Cancel();
            _presentationCts?.Dispose();
            _presentationCts = null;

            // 상태 리셋
            _battleCompleted = false;
            _spinRoutineRunning = false;
            _lastRequestResult = null;

            _stateUpdater = new RunBattleScreenStateUpdater(_screenViewModel);
            _spinSequence = new RunBattleSpinSequence(_spinLeverView, _slotMachineFrameView);
            _spinSequence.SetupImmediate();

            _screenViewModel.Changed += HandleScreenStateChanged;
            _view.SpinRequested += HandleSpinClicked;
            _view.ContinueRequested += NavigateToReward;
            _view.RestartRequested += NavigateToStart;
            _view.Render(_screenViewModel.State);

            InitializePresentationStack();
            _screenViewModel.SetActionMode(RunBattleActionMode.Spin, spinInteractable: true);

            StartBattle();
            RefreshStatusText();
            RefreshSlotResultText();
        }

        private void OnDisable()
        {
#if DOTWEEN
            transform.DOKill(true);
#endif
        }

        private void OnDestroy()
        {
            if (_view != null)
            {
                _view.SpinRequested -= HandleSpinClicked;
                _view.ContinueRequested -= NavigateToReward;
                _view.RestartRequested -= NavigateToStart;
            }

            _screenViewModel.Changed -= HandleScreenStateChanged;
            _presentationCts?.Cancel();
            _presentationCts?.Dispose();
            _presentationCts = null;
        }

        public void Bind(
            RunBattleScreenView view,
            FloatingDamageTextView floatingDamageTextPrefab,
            MonsterDefinition monsterDefinition,
            SlotLeverView spinLeverView,
            SlotMachineFrameView slotMachineFrameView,
            SlotPresentationManager slotPresentationManager)
        {
            _view = view;
            _floatingDamageTextPrefab = floatingDamageTextPrefab;
            _spinLeverView = spinLeverView;
            _slotMachineFrameView = slotMachineFrameView;
            _slotPresentationManager = slotPresentationManager;
        }

        private void ResolveSceneReferences()
        {
            _view ??= SceneComponentResolver.FindInSceneRoot<RunBattleScreenView>(transform);
            _spinLeverView ??= SceneComponentResolver.FindInSceneRoot<SlotLeverView>(transform);
            _slotMachineFrameView ??= SceneComponentResolver.FindInSceneRoot<SlotMachineFrameView>(transform);
            _slotMachineFrameView ??= CreateRuntimeSlotMachineFrameView();
            _slotPresentationManager ??= SceneComponentResolver.FindInSceneRoot<SlotPresentationManager>(transform);
        }

        private SlotMachineFrameView CreateRuntimeSlotMachineFrameView()
        {
            Transform slotMachinePanel = ResolveTransformInSceneRoot("Slot Machine Panel");
            if (slotMachinePanel == null)
            {
                return null;
            }

            SlotMachineFrameView frameView =
                slotMachinePanel.GetComponent<SlotMachineFrameView>() ??
                slotMachinePanel.gameObject.AddComponent<SlotMachineFrameView>();
            frameView.SetIdleImmediate();
            return frameView;
        }

        private Transform ResolveTransformInSceneRoot(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            Transform found = SceneComponentResolver.FindDeepChild(transform.root ?? transform, objectName);
            if (found != null)
            {
                return found;
            }

            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                found = SceneComponentResolver.FindDeepChild(roots[index].transform, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void HandleScreenStateChanged(RunBattleScreenState state)
        {
            _view.Render(state);
        }

        private void StartBattle()
        {
            var player = new CombatParticipant(GameFlowSession.PlayerMaxHp, GameFlowSession.PlayerCurrentHp);
            _encounterRoster = BuildEncounterRoster();

            _battle.StartBattle(player, _encounterRoster.Enemies, _encounterRoster.Schedules);
            _selectedEnemyId = ResolveSelectedEnemyId();
            _combatViewModel.SyncFrom(_battle);
            BindEnemySlots();
            _eventLogger.LogEventsSince(_battle, eventCursor: 0);
        }

        private void InitializePresentationStack()
        {
            _presentationCts = new CancellationTokenSource();
            Transform floatingTextRoot = _view.FloatingTextRoot ?? _view.transform;
            RectTransform playerDamageAnchor = _view.PlayerDamageAnchor
                ?? ResolveDamageAnchor(floatingTextRoot, "player-damage-anchor", new Vector2(0f, -120f));
            RectTransform monsterDamageAnchor = _view.GetEnemyDamageAnchor(1);
            if (monsterDamageAnchor == null)
            {
                monsterDamageAnchor = ResolveDamageAnchor(
                    floatingTextRoot, "monster-damage-anchor", new Vector2(0f, 140f));
                Debug.LogWarning(
                    "[RunBattleCompositionRoot] Center monster damage anchor missing. " +
                    "Using a temporary overlay anchor for this play session.");
            }

            _presentationHost = new CombatPresentationHost(
                gameObject,
                _view.StatusText,
                floatingTextRoot,
                _floatingDamageTextPrefab,
                playerDamageAnchor,
                monsterDamageAnchor,
                GetDefaultFont(),
                RefreshStatusText);
            CombatPresentationPipeline pipeline = CombatPresentationPipeline.CreateDefault(_presentationHost);
            _flowController = new BattleFlowController(pipeline, _combatViewModel);
        }

        private static RectTransform ResolveDamageAnchor(
            Transform overlayRoot,
            string anchorName,
            Vector2 defaultPosition)
        {
            if (overlayRoot == null)
            {
                return null;
            }

            Transform existing = overlayRoot.Find(anchorName);
            if (existing is RectTransform existingRect)
            {
                return existingRect;
            }

            var anchorObject = new GameObject(anchorName, typeof(RectTransform));
            RectTransform anchor = anchorObject.GetComponent<RectTransform>();
            anchor.SetParent(overlayRoot, false);
            anchor.anchorMin = new Vector2(0.5f, 0.5f);
            anchor.anchorMax = new Vector2(0.5f, 0.5f);
            anchor.pivot = new Vector2(0.5f, 0.5f);
            anchor.anchoredPosition = defaultPosition;
            anchor.sizeDelta = Vector2.zero;
            return anchor;
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private void HandleSpinClicked()
        {
            HandleSpinClickedAsync().Forget();
        }

        private async UniTaskVoid HandleSpinClickedAsync()
        {
            if (_battleCompleted || _spinRoutineRunning || !_battle.CanApplyPlayerTurn)
            {
                RefreshStatusText();
                return;
            }

            if (_flowController.IsBusy)
            {
                return;
            }

            SetSpinInteractable(false);
            _spinRoutineRunning = true;
            _spinSequence.Reset();

            try
            {
                await _spinSequence.PlayDownAsync(_presentationCts.Token);
                _spinSequence.StartSpin();

                _slotViewModel.Spin();
                ArtifactDefinitionSO artifact = StarterArtifactCatalog.GetById(GameFlowSession.SelectedArtifactId);
                _lastRequestResult = _requestResolver.Resolve(
                    _slotViewModel.CurrentPatternResult,
                    _slotViewModel.CurrentCombatRequest,
                    artifact,
                    GameFlowSession.DamageBonus,
                    GameFlowSession.DefenseBonus);

                _stateUpdater.UpdateSlotCells(_slotViewModel.CurrentSpinResult);
                RefreshSlotResultText();

                SlotCombatRequest request = _lastRequestResult.FinalRequest;
                CombatParticipantId selectedTargetId = ResolveSelectedEnemyId();
                CombatEffect[] playerEffects = _converter.Convert(
                    request,
                    selectedTargetId,
                    _lastRequestResult.StatusEffectToApply);
                var context = new PresentationContext(request.IsCritical, request.PatternName);
                int eventCursor = _eventLogger.CaptureEventCursor(_battle);

                if (_slotPresentationManager != null)
                {
                    SlotPresentationResult presentationResult = BuildSlotPresentationResult();
                    bool presentationDone = false;
                    bool slotSpinDone = false;
                    void HandleSlotSpinCompleted()
                    {
                        slotSpinDone = true;
                    }

                    _slotPresentationManager.SlotSpinCompleted += HandleSlotSpinCompleted;
                    try
                    {
                        _slotPresentationManager.Play(presentationResult, _ => presentationDone = true);

                        await UniTask.WaitUntil(
                            () => slotSpinDone || presentationDone,
                            cancellationToken: _presentationCts.Token);
                        await _spinSequence.SettleIfNeededAsync(_presentationCts.Token);
                        await UniTask.WaitUntil(() => presentationDone, cancellationToken: _presentationCts.Token);
                    }
                    finally
                    {
                        _slotPresentationManager.SlotSpinCompleted -= HandleSlotSpinCompleted;
                    }
                }
                else
                {
                    await _spinSequence.SettleIfNeededAsync(_presentationCts.Token);
                }

                BattleApplyResult result = await _flowController.RunTurnAsync(
                    _battle,
                    playerEffects,
                    selectedTargetId,
                    context,
                    _presentationCts.Token,
                    RaiseLeverBeforeTurnTransitionAsync,
                    RaiseLeverAfterPlayerAttackAsync);

                if (result.Accepted)
                {
                    _eventLogger.LogEventsSince(_battle, eventCursor, request);
                }
            }
            finally
            {
                _spinSequence.ResetImmediate();
                _spinRoutineRunning = false;
                RefreshStatusText();
                HandleBattleEndIfNeeded();
                UpdateSpinButtonState();
            }

            async UniTask RaiseLeverBeforeTurnTransitionAsync(
                CombatEvent combatEvent,
                int eventIndex,
                IReadOnlyList<CombatEvent> events)
            {
                if (ShouldRaiseLeverBeforeEvent(combatEvent))
                {
                    await _spinSequence.RaiseLeverIfNeededAsync();
                }
            }

            async UniTask RaiseLeverAfterPlayerAttackAsync(
                CombatEvent combatEvent,
                int eventIndex,
                IReadOnlyList<CombatEvent> events)
            {
                if (IsLastPlayerAttackPresentation(combatEvent, eventIndex, events))
                {
                    await _spinSequence.RaiseLeverIfNeededAsync();
                }
            }
        }

        private static bool ShouldRaiseLeverBeforeEvent(CombatEvent combatEvent)
        {
            if (combatEvent.Kind == CombatEventKind.BattleEnded)
            {
                return true;
            }

            return combatEvent.Kind == CombatEventKind.PhaseChanged &&
                combatEvent.Phase != BattlePhase.Resolving;
        }

        private static bool IsLastPlayerAttackPresentation(
            CombatEvent combatEvent,
            int eventIndex,
            IReadOnlyList<CombatEvent> events)
        {
            if (!IsPlayerAttackPresentation(combatEvent))
            {
                return false;
            }

            for (int index = eventIndex + 1; index < events.Count; index++)
            {
                CombatEvent nextEvent = events[index];
                if (IsPlayerAttackPresentation(nextEvent))
                {
                    return false;
                }

                if (nextEvent.Kind == CombatEventKind.PhaseChanged &&
                    nextEvent.Phase != BattlePhase.Resolving)
                {
                    break;
                }
            }

            return true;
        }

        private static bool IsPlayerAttackPresentation(CombatEvent combatEvent)
        {
            return combatEvent.Kind == CombatEventKind.EffectApplied &&
                combatEvent.Phase == BattlePhase.Resolving &&
                !combatEvent.IsPlayerParticipant;
        }

        private void SetSpinInteractable(bool interactable)
        {
            _screenViewModel.SetSpinInteractable(interactable);
        }

        private void UpdateSpinButtonState()
        {
            if (_battleCompleted)
            {
                return;
            }

            bool canSpin = _battle.CurrentPhase != BattlePhase.NotInBattle
                && _battle.CanApplyPlayerTurn
                && !_flowController.IsBusy
                && !_spinRoutineRunning;

            _screenViewModel.SetActionMode(RunBattleActionMode.Spin, canSpin);
        }

        private void HandleBattleEndIfNeeded()
        {
            if (_battle.CurrentPhase != BattlePhase.Ended || _battleCompleted)
            {
                return;
            }

            _battleCompleted = true;
            _screenViewModel.SetSpinInteractable(false);

            if (_battle.EndReason == BattleEndReason.Victory)
            {
                if (GameFlowSession.IsInfiniteMode)
                {
                    GameFlowSession.CompleteInfiniteVictory(_battle.Player.CurrentHp);
                }
                else
                {
                    GameFlowSession.CompleteBattleVictory(_battle.Player.CurrentHp);
                }

                _screenViewModel.SetActionMode(RunBattleActionMode.Continue, spinInteractable: false);
                RefreshStatusText();
            }
            else
            {
                GameFlowSession.CompleteBattleDefeat();
                _screenViewModel.SetActionMode(RunBattleActionMode.Restart, spinInteractable: false);
            }
        }

        private void RefreshSlotResultText()
        {
            string enemyActionText = RunBattleScreenStateUpdater.FormatUpcomingEnemyAction(_battle);
            _stateUpdater.UpdateSlotResult(
                _lastRequestResult,
                _slotViewModel.CurrentPatternResult,
                enemyActionText);
        }

        private void RefreshStatusText()
        {
            if (_battle.CurrentPhase == BattlePhase.NotInBattle)
            {
                return;
            }

            CombatParticipantId selectedTargetId = ResolveSelectedEnemyId();
            string statusText =
                $"{_battle.CurrentPhase}\n" +
                $"Turn {_battle.UpcomingMonsterTurnIndex}\n" +
                $"Enemies {_battle.Enemies.Count}\n" +
                $"Bonus D+{GameFlowSession.DamageBonus} / S+{GameFlowSession.DefenseBonus}";
            string enemyIntentText =
                $"ENEMY INTENT: {RunBattleScreenStateUpdater.FormatUpcomingEnemyAction(_battle)}\n" +
                $"TARGET: {selectedTargetId}";

            int[] enemySlotIndices = null;
            _screenViewModel.Batch(() =>
            {
                _stateUpdater.UpdatePlayerHud(_battle.Player, _combatViewModel);
                enemySlotIndices = _stateUpdater.UpdateEnemySlots(
                    _battle,
                    _combatViewModel,
                    GetEncounterTitle(),
                    _view.EnemySlotCount,
                    _encounterRoster,
                    selectedTargetId,
                    _flowController.IsBusy,
                    _spinRoutineRunning);
                _stateUpdater.UpdateBattleTextMeta(statusText, enemyIntentText);
            });

            UpdateEnemyPortraits(enemySlotIndices);
            UpdateSpinButtonState();
        }

        private void BindEnemySlots()
        {
            int slotCount = _view.EnemySlotCount;
            for (int index = 0; index < slotCount; index++)
            {
                _view.SetEnemySlotClickHandler(index, null);
                _view.SetEnemyPortrait(index, null);
            }

            int bindCount = Mathf.Min(slotCount, _battle.Enemies.Count);
            var usedFormationSlots = new HashSet<int>();
            for (int rosterIndex = 0; rosterIndex < bindCount; rosterIndex++)
            {
                CombatParticipant enemy = _battle.Enemies[rosterIndex];
                CombatParticipantId enemyId = enemy.Id;
                int slotIndex = RunBattleScreenStateUpdater.ResolveHudSlotIndex(
                    _encounterRoster, rosterIndex, slotCount, usedFormationSlots);
                _view.SetEnemySlotClickHandler(slotIndex, () => HandleEnemySelected(enemyId));
                RectTransform anchor = ResolveEnemyDamageAnchor(slotIndex);
                _presentationHost.SetEnemyDamageAnchor(enemyId, anchor);
            }

            if (_battle.Enemies.Count > slotCount)
            {
                Debug.LogWarning(
                    $"[RunBattleCompositionRoot] Enemy count {_battle.Enemies.Count} exceeds configured slots {slotCount}.");
            }
        }

        private void UpdateEnemyPortraits(int[] slotIndices)
        {
            if (slotIndices == null)
            {
                return;
            }

            for (int rosterIndex = 0; rosterIndex < slotIndices.Length; rosterIndex++)
            {
                _view.SetEnemyPortrait(slotIndices[rosterIndex], ResolveEncounterMonster(rosterIndex)?.portrait);
            }
        }

        private RectTransform ResolveEnemyDamageAnchor(int slotIndex)
        {
            RectTransform anchor = _view.GetEnemyDamageAnchor(slotIndex);
            if (anchor == null)
            {
                Debug.LogError(
                    $"[RunBattleCompositionRoot] Damage anchor missing for formation slot {slotIndex}. " +
                    "Run menu: SlotRogue > Game Flow > Rebuild Scene UI Prefabs.");
            }

            return anchor;
        }

        private MonsterDefinition ResolveEncounterMonster(int rosterIndex)
        {
            if (GameFlowSession.IsInfiniteMode)
            {
                return null;
            }

            RunMapNodeDefinition node = GetEncounterNode();
            RunEncounterDefinition encounter = node?.Encounter;
            if (encounter?.entries == null ||
                rosterIndex < 0 ||
                rosterIndex >= encounter.entries.Length)
            {
                return null;
            }

            return encounter.entries[rosterIndex].monster;
        }

        private void HandleEnemySelected(CombatParticipantId enemyId)
        {
            if (_flowController.IsBusy || _spinRoutineRunning)
            {
                return;
            }

            for (int index = 0; index < _battle.Enemies.Count; index++)
            {
                CombatParticipant enemy = _battle.Enemies[index];
                if (enemy.Id.Value == enemyId.Value && !enemy.IsDead)
                {
                    _selectedEnemyId = enemyId;
                    RefreshStatusText();
                    return;
                }
            }
        }

        private CombatParticipantId ResolveSelectedEnemyId()
        {
            if (_selectedEnemyId.IsValid)
            {
                for (int index = 0; index < _battle.Enemies.Count; index++)
                {
                    CombatParticipant enemy = _battle.Enemies[index];
                    if (enemy.Id.Value == _selectedEnemyId.Value && !enemy.IsDead)
                    {
                        return _selectedEnemyId;
                    }
                }
            }

            for (int index = 0; index < _battle.Enemies.Count; index++)
            {
                CombatParticipant enemy = _battle.Enemies[index];
                if (!enemy.IsDead)
                {
                    _selectedEnemyId = enemy.Id;
                    return _selectedEnemyId;
                }
            }

            _selectedEnemyId = default;
            return default;
        }

        private static RunMapNodeDefinition GetEncounterNode()
        {
            return GameFlowSession.CurrentEncounterNode ?? GameFlowSession.CurrentMapNode;
        }

        // 무한모드는 맵 노드 없이 등급(Tier) 기반으로, 스토리모드는 기존 맵 경로로 적을 구성합니다.
        private RunEncounterRoster BuildEncounterRoster()
        {
            if (GameFlowSession.IsInfiniteMode)
            {
                return RunEncounterRosterBuilder.BuildForTier(
                    GameFlowSession.CurrentTier,
                    GameFlowSession.CurrentBattleNumber,
                    fallback: null);
            }

            RunMapNodeDefinition encounterNode = GetEncounterNode();
            int floor = Mathf.Max(1, encounterNode.Floor);
            return RunEncounterRosterBuilder.Build(encounterNode, floor);
        }

        private static string GetEncounterTitle()
        {
            return GameFlowSession.IsInfiniteMode
                ? GameFlowSession.CurrentEncounterTitle
                : GetEncounterNode().DisplayName;
        }

        private void NavigateToReward()
        {
            BattleVictory?.Invoke();
        }

        private void NavigateToStart()
        {
            BattleDefeat?.Invoke();
        }

        private SlotPresentationResult BuildSlotPresentationResult()
        {
            SlotSpinResult spinResult = _slotViewModel.CurrentSpinResult;
            SlotCombatRequest request = _lastRequestResult?.FinalRequest ?? SlotCombatRequest.Empty;

            IReadOnlyList<SlotPatternMatch> matches =
                _presentationPatternResolver.ResolveAll(spinResult);

            var patternPresentations = new SlotPatternPresentationResult[matches.Count];

            for (int index = 0; index < matches.Count; index++)
            {
                SlotPatternMatch match = matches[index];
                var cellIndices = new int[match.MatchedCells.Count];

                for (int cellIndex = 0; cellIndex < match.MatchedCells.Count; cellIndex++)
                {
                    SlotCell cell = match.MatchedCells[cellIndex];
                    cellIndices[cellIndex] = SlotSpinResult.ToIndex(cell.Col, cell.Row);
                }

                patternPresentations[index] = new SlotPatternPresentationResult(
                    match.PresentationTitle,
                    match.Symbol,
                    match.MatchedCells.Count > 0 ? match.MatchedCells[0].Row : -1,
                    match.MatchedCells.Count > 0 ? match.MatchedCells[0].Col : -1,
                    match.MatchedCells.Count,
                    cellIndices,
                    $"{match.Symbol} x{match.MatchedCells.Count} / x{match.Multiplier:0.0}",
                    $"+{match.CalculatedValue} pts",
                    match.Definition.IsJackpot,
                    index,
                    match.CalculatedValue);
            }

            var finalResult = new SlotFinalPresentationResult(
                request.Damage,
                request.Defense,
                request.AttackCount,
                request.HealAmount,
                $"DMG {request.Damage} / DEF {request.Defense}");

            return new SlotPresentationResult(spinResult, patternPresentations, null, finalResult);
        }
    }
}
