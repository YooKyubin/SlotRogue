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

        // щ’ 곕퀎 媛蹂 щ낵 (GameFlowSession.SlotPool)먯꽌 媛쒖닔 鍮꾨濡戮묐뒗
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
        private RunBattleEnemySelectionBinder _enemySelectionBinder;

        [SerializeField] private RunBattleScreenView _view;
        [SerializeField] private FloatingDamageTextView _floatingDamageTextPrefab;
        [SerializeField] private SlotLeverView _spinLeverView;
        [SerializeField] private SlotMachineFrameView _slotMachineFrameView;
        [SerializeField] private SlotPresentationManager _slotPresentationManager;

        private RunCombatRequestResult _lastRequestResult;
        private bool _battleCompleted;
        private bool _spinRoutineRunning;
        private RunEncounterRoster _encounterRoster;

        /// <summary>꾪닾 밸━ BattleView媛 援щ룆⑸땲</summary>
        public event System.Action BattleVictory;

        /// <summary>꾪닾 ⑤같 BattleView媛 援щ룆⑸땲</summary>
        public event System.Action BattleDefeat;

        private void Awake()
        {
            // ⑥씪 援ъ“먯꽌BeginBattle()Navigator섑빐 紐낆떆곸쑝濡몄텧⑸땲
            // Awake먯꽌덊띁곗뒪留뺣낫⑸땲
            ResolveSceneReferences();
        }

        /// <summary>
        /// BattleView(OnEnter)媛 꾪닾 붾㈃吏꾩엯몄텧⑸땲
        /// 留꾪닾留덈떎 곹깭瑜珥덇린뷀븯怨꾪닾瑜쒖옉⑸땲
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

            // 以묐났 援щ룆 諛⑹: ъ쭊湲곗〈 援щ룆 쒓굅
            _screenViewModel.Changed -= HandleScreenStateChanged;
            _view.SpinRequested -= HandleSpinClicked;
            _view.ContinueRequested -= NavigateToReward;
            _view.RestartRequested -= NavigateToStart;

            // 댁쟾 꾪닾 痍⑥냼좏겙 뺣━
            _presentationCts?.Cancel();
            _presentationCts?.Dispose();
            _presentationCts = null;

            // 곹깭 由ъ뀑
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
            RunMapNodeDefinition encounterNode = GameFlowSession.IsInfiniteMode ? null : GetEncounterNode();
            CombatParticipant player = RunCombatParticipantFactory.CreatePlayer(
                GameFlowSession.PlayerMaxHp,
                GameFlowSession.PlayerCurrentHp);
            _encounterRoster = BuildEncounterRoster();

            _battle.StartBattle(player, _encounterRoster.Enemies, _encounterRoster.Schedules);
            _enemySelectionBinder = new RunBattleEnemySelectionBinder(
                _battle,
                _view,
                _encounterRoster,
                _presentationHost,
                encounterNode,
                () => _flowController.IsBusy,
                () => _spinRoutineRunning,
                RefreshStatusText);
            _enemySelectionBinder.ResolveSelectedEnemyId();
            _combatViewModel.SyncFrom(_battle);
            _enemySelectionBinder.Bind();
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

        public void DevApplyStatusTurn(StatusEffectKind statusEffectKind, int duration, int magnitude,
            StatusStackMode stackMode, bool includeDamage, int damage, int attackCount)
        {
            DevApplyStatusTurnAsync(
                statusEffectKind,
                duration,
                magnitude,
                stackMode,
                includeDamage,
                damage,
                attackCount).Forget();
        }

        private async UniTaskVoid DevApplyStatusTurnAsync(
            StatusEffectKind statusEffectKind,
            int duration,
            int magnitude,
            StatusStackMode stackMode,
            bool includeDamage,
            int damage,
            int attackCount)
        {
            if (statusEffectKind == StatusEffectKind.None)
            {
                Debug.LogWarning("[RunBattleCompositionRoot] Dev status turn ignored because status kind is None.");
                return;
            }

            if (_battleCompleted || _spinRoutineRunning || !_battle.CanApplyPlayerTurn || _flowController.IsBusy)
            {
                RefreshStatusText();
                return;
            }

            CombatParticipantId selectedTargetId = _enemySelectionBinder != null
                ? _enemySelectionBinder.ResolveSelectedEnemyId()
                : default;
            if (!selectedTargetId.IsValid)
            {
                Debug.LogWarning("[RunBattleCompositionRoot] Dev status turn ignored because no selected enemy target was found.");
                return;
            }

            SetSpinInteractable(false);
            _spinRoutineRunning = true;

            try
            {
                var statusEffect = new StatusEffectSpec(
                    statusEffectKind,
                    Mathf.Max(0, duration),
                    Mathf.Max(0, magnitude),
                    stackMode);
                var request = new SlotCombatRequest(
                    includeDamage ? Mathf.Max(0, damage) : 0,
                    0,
                    Mathf.Max(1, attackCount),
                    0,
                    false,
                    $"DEV {statusEffectKind}");
                _lastRequestResult = new RunCombatRequestResult(
                    request,
                    request,
                    StarterArtifactActivation.None,
                    $"DEV {statusEffectKind}",
                    statusEffect);
                RefreshSlotResultText();

                CombatEffect[] playerEffects = _converter.Convert(request, selectedTargetId, statusEffect);
                var context = new PresentationContext(isCritical: false, request.PatternName);
                int eventCursor = _eventLogger.CaptureEventCursor(_battle);

                BattleApplyResult result = await _flowController.RunTurnAsync(
                    _battle,
                    playerEffects,
                    selectedTargetId,
                    context,
                    _presentationCts != null ? _presentationCts.Token : CancellationToken.None);

                if (result.Accepted)
                {
                    _eventLogger.LogEventsSince(_battle, eventCursor, request);
                }
            }
            finally
            {
                _spinRoutineRunning = false;
                RefreshStatusText();
                HandleBattleEndIfNeeded();
                UpdateSpinButtonState();
            }
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
                CombatParticipantId selectedTargetId = _enemySelectionBinder != null
                    ? _enemySelectionBinder.ResolveSelectedEnemyId()
                    : default;
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

            CombatParticipantId selectedTargetId = _enemySelectionBinder != null
                ? _enemySelectionBinder.ResolveSelectedEnemyId()
                : default;
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

            _enemySelectionBinder?.UpdateEnemyPortraits(enemySlotIndices);
            UpdateSpinButtonState();
        }

        private static RunMapNodeDefinition GetEncounterNode()
        {
            return GameFlowSession.CurrentEncounterNode ?? GameFlowSession.CurrentMapNode;
        }

        // 臾댄븳紐⑤뱶留몃뱶 놁씠 깃툒(Tier) 湲곕컲쇰줈, ㅽ넗由щえ쒕뒗 湲곗〈 留寃쎈줈濡곸쓣 援ъ꽦⑸땲
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
