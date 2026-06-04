using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using SlotRogue.Data.GameFlow;
using SlotRogue.Slot.Data;
using SlotRogue.Slot.ViewModels;
using SlotRogue.UI.Combat;
using SlotRogue.UI.Combat.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleController : MonoBehaviour
    {
        private readonly BattleSystem _battle = new();
        private readonly SlotMachineViewModel _slotViewModel = new();
        private readonly SlotCombatRequestToCombatEffectsConverter _converter = new();
        private readonly CombatEventConsoleLogger _eventLogger = new();
        private readonly RunCombatRequestResolver _requestResolver = new();
        private readonly CombatViewModel _combatViewModel = new();

        private BattleFlowController _flowController = null!;
        private CombatPresentationHost _presentationHost = null!;
        private CancellationTokenSource _presentationCts = null!;
        private Transform _floatingTextRoot = null!;
        private RectTransform _playerDamageAnchor = null!;
        private RectTransform _monsterDamageAnchor = null!;

        [SerializeField] private RunBattleView _view;
        [SerializeField] private FloatingDamageTextView _floatingDamageTextPrefab;
        [SerializeField] private MonsterDefinition _monsterDefinition;

        private RunCombatRequestResult _lastRequestResult;
        private bool _battleCompleted;
        private CombatParticipantId _selectedEnemyId;
        private RunEncounterRoster _encounterRoster = null!;

        private void Awake()
        {
            GameFlowSession.EnsureRunStarted();

            if (!GameFlowSession.HasStarterArtifact)
            {
                SceneManager.LoadScene(GameFlowSceneNames.StartArtifactSelection);
                return;
            }

            if (_view == null)
            {
                _view = GetComponent<RunBattleView>();
            }

            if (_view == null)
            {
                return;
            }

            InitializePresentationStack();

            _view.SpinButton.onClick.RemoveAllListeners();
            _view.ContinueButton.onClick.RemoveAllListeners();
            _view.RestartButton.onClick.RemoveAllListeners();
            _view.SpinButton.onClick.AddListener(HandleSpinClicked);
            _view.ContinueButton.onClick.AddListener(LoadRewardScene);
            _view.RestartButton.onClick.AddListener(ReturnToStart);
            _view.ShowSpinButton();

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
            _presentationCts?.Cancel();
            _presentationCts?.Dispose();
            _presentationCts = null!;
        }

        private void StartBattle()
        {
            RunMapNodeDefinition encounterNode = GetEncounterNode();
            int floor = Mathf.Max(1, encounterNode.Floor);
            var player = new CombatParticipant(GameFlowSession.PlayerMaxHp, GameFlowSession.PlayerCurrentHp);
            _encounterRoster = RunEncounterRosterBuilder.Build(encounterNode, floor, _monsterDefinition);

            _battle.StartBattle(player, _encounterRoster.Enemies, _encounterRoster.Schedules);
            _selectedEnemyId = ResolveSelectedEnemyId();
            _combatViewModel.SyncFrom(_battle);
            _view.EnsureEnemySlotCapacity(_battle.Enemies.Count);
            BindEnemySlots();
            _eventLogger.LogEventsSince(_battle, eventCursor: 0);
        }

        private void InitializePresentationStack()
        {
            _presentationCts = new CancellationTokenSource();
            Transform floatingTextRoot = ResolveFloatingTextRoot();
            _floatingTextRoot = floatingTextRoot;
            _playerDamageAnchor = _view.PlayerDamageAnchor != null
                ? _view.PlayerDamageAnchor
                : ResolveDamageAnchor(floatingTextRoot, "player-damage-anchor", new Vector2(0f, -120f));
            _monsterDamageAnchor = _view.MonsterDamageAnchor != null
                ? _view.MonsterDamageAnchor
                : ResolveDamageAnchor(floatingTextRoot, "monster-damage-anchor", new Vector2(0f, 40f));

            _presentationHost = new CombatPresentationHost(
                gameObject,
                _view.StatusText,
                floatingTextRoot,
                _floatingDamageTextPrefab,
                _playerDamageAnchor,
                _monsterDamageAnchor,
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

        private Transform ResolveFloatingTextRoot()
        {
            if (_view.FloatingTextRoot != null)
            {
                return _view.FloatingTextRoot;
            }

            Canvas canvas = _view.GetComponent<Canvas>();

            if (canvas == null)
            {
                return _view.transform;
            }

            var overlayObject = new GameObject("Presentation Overlay", typeof(RectTransform), typeof(CanvasGroup));
            RectTransform overlay = overlayObject.GetComponent<RectTransform>();
            overlay.SetParent(canvas.transform, false);
            overlay.SetAsLastSibling();
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;

            CanvasGroup canvasGroup = overlayObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            return overlay;
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private void HandleSpinClicked()
        {
            HandleSpinClickedAsync().Forget();
        }

        private async UniTaskVoid HandleSpinClickedAsync()
        {
            if (_battleCompleted || !_battle.CanApplyPlayerTurn)
            {
                RefreshStatusText();
                return;
            }

            if (_flowController.IsBusy)
            {
                return;
            }

            _slotViewModel.Spin();
            StarterArtifactDefinition artifact = StarterArtifactCatalog.Get(GameFlowSession.SelectedStarterArtifactId);
            _lastRequestResult = _requestResolver.Resolve(
                _slotViewModel.CurrentPatternResult,
                _slotViewModel.CurrentCombatRequest,
                artifact,
                GameFlowSession.DamageBonus,
                GameFlowSession.DefenseBonus);

            RefreshSlotCells(_slotViewModel.CurrentSpinResult);
            RefreshSlotResultText();

            SlotCombatRequest request = _lastRequestResult.FinalRequest;
            CombatParticipantId selectedTargetId = ResolveSelectedEnemyId();
            CombatEffect[] playerEffects = _converter.Convert(request, selectedTargetId);
            var context = new PresentationContext(request.IsCritical, request.PatternName);
            int eventCursor = _eventLogger.CaptureEventCursor(_battle);

            SetSpinInteractable(false);

            try
            {
                BattleApplyResult result = await _flowController.RunTurnAsync(
                    _battle,
                    playerEffects,
                    selectedTargetId,
                    context,
                    _presentationCts.Token);

                if (result.Accepted)
                {
                    _eventLogger.LogEventsSince(_battle, eventCursor, request);
                }
            }
            finally
            {
                RefreshStatusText();
                HandleBattleEndIfNeeded();
            }
        }

        private void SetSpinInteractable(bool interactable)
        {
            if (_view.SpinButton != null)
            {
                _view.SpinButton.interactable = interactable;
            }
        }

        private void UpdateSpinButtonState()
        {
            if (_view.SpinButton == null || _battleCompleted)
            {
                return;
            }

            bool canSpin = _battle.CurrentPhase != BattlePhase.NotInBattle
                && _battle.CanApplyPlayerTurn
                && !_flowController.IsBusy;

            _view.SpinButton.interactable = canSpin;
        }

        private void HandleBattleEndIfNeeded()
        {
            if (_battle.CurrentPhase != BattlePhase.Ended || _battleCompleted)
            {
                return;
            }

            _battleCompleted = true;
            _view.SpinButton.interactable = false;

            if (_battle.EndReason == BattleEndReason.Victory)
            {
                GameFlowSession.CompleteBattleVictory(_battle.Player.CurrentHp);
                _view.ShowContinueButton();
                RefreshStatusText();
            }
            else
            {
                GameFlowSession.CompleteBattleDefeat();
                _view.ShowRestartButton();
            }
        }

        private void RefreshSlotCells(SlotSpinResult spinResult)
        {
            if (spinResult == null)
            {
                return;
            }

            for (int index = 0; index < _view.SlotCells.Length; index++)
            {
                _view.SlotCells[index].text = FormatSlotSymbol(spinResult.Symbols[index]);
            }
        }

        private void RefreshSlotResultText()
        {
            if (_lastRequestResult == null)
            {
                _view.SetAttackResult("-");
                _view.SetSlotResult(
                    "NEXT ATTACK\n" +
                    FormatUpcomingEnemyAction());
                _view.ResetSlotOutcomePresentation();
                return;
            }

            SlotCombatRequest request = _lastRequestResult.FinalRequest;
            SlotPatternResult patternResult = _slotViewModel.CurrentPatternResult;
            bool hasPattern = patternResult != null && patternResult.HasMatch;
            int attackResult = Mathf.Max(request.Damage, request.Defense);
            _view.SetAttackResult(attackResult.ToString());

            var builder = new StringBuilder();
            if (hasPattern)
            {
                builder.AppendLine("PATTERN HIT!");
                builder.AppendLine(patternResult.PatternName);
            }
            else
            {
                builder.AppendLine(SlotCombatRequest.BaseAttackName.ToUpperInvariant());
                builder.AppendLine("No pattern matched");
            }

            builder.AppendLine(FormatRequest(request));

            if (_lastRequestResult.StarterArtifactActivation.Activated)
            {
                builder.AppendLine(_lastRequestResult.StarterArtifactActivation.ArtifactName);
            }

            if (!string.IsNullOrEmpty(_lastRequestResult.RunBonusSummary))
            {
                builder.AppendLine(_lastRequestResult.RunBonusSummary);
            }

            _view.SetSlotResult(builder.ToString());
            _view.SetSlotOutcomePresentation(
                hasPattern,
                patternResult != null ? patternResult.Row : -1,
                patternResult != null ? patternResult.StartColumn : -1,
                patternResult != null ? patternResult.MatchLength : 0);
        }

        private void RefreshStatusText()
        {
            if (_battle.CurrentPhase == BattlePhase.NotInBattle)
            {
                return;
            }

            CombatParticipant player = _battle.Player;

            _view.SetPlayerHpFill(_combatViewModel.PlayerHp, player.MaxHp);
            _view.SetPlayerShieldFill(_combatViewModel.PlayerShield, Mathf.Max(1, player.MaxHp));
            _view.SetPlayerHud(
                $"{_combatViewModel.PlayerHp}/{player.MaxHp}\n" +
                $"{_combatViewModel.PlayerShield}");
            CombatParticipantId selectedTargetId = ResolveSelectedEnemyId();
            RefreshEnemySlots();
            _view.SetEnemyIntent(
                $"ENEMY INTENT: {FormatUpcomingEnemyAction()}\n" +
                $"TARGET: {selectedTargetId}");
            _view.SetStatus(
                $"{_battle.CurrentPhase}\n" +
                $"Turn {_battle.UpcomingMonsterTurnIndex}\n" +
                $"Enemies {_battle.Enemies.Count}\n" +
                $"Bonus D+{GameFlowSession.DamageBonus} / S+{GameFlowSession.DefenseBonus}");

            UpdateSpinButtonState();
        }

        private void BindEnemySlots()
        {
            int slotCount = _view.EnemySlotCount;
            for (int index = 0; index < slotCount; index++)
            {
                _view.SetEnemySlotClickHandler(index, null);
                _view.SetEnemySlotActive(index, false);
            }

            int bindCount = Mathf.Min(slotCount, _battle.Enemies.Count);
            var usedFormationSlots = new HashSet<int>();
            for (int rosterIndex = 0; rosterIndex < bindCount; rosterIndex++)
            {
                CombatParticipant enemy = _battle.Enemies[rosterIndex];
                CombatParticipantId enemyId = enemy.Id;
                int slotIndex = ResolveHudSlotIndex(rosterIndex, slotCount, usedFormationSlots);
                _view.SetEnemySlotClickHandler(slotIndex, () => HandleEnemySelected(enemyId));
                RectTransform anchor = ResolveEnemyDamageAnchor(slotIndex);
                _presentationHost.SetEnemyDamageAnchor(enemyId, anchor);
            }

            if (_battle.Enemies.Count > slotCount)
            {
                Debug.LogWarning(
                    $"[RunBattleController] Enemy count {_battle.Enemies.Count} exceeds configured HUD slots {slotCount}.");
            }
        }

        private void RefreshEnemySlots()
        {
            int slotCount = _view.EnemySlotCount;
            int enemyCount = _battle.Enemies.Count;

            for (int index = 0; index < slotCount; index++)
            {
                _view.SetEnemySlotActive(index, false);
            }

            var usedFormationSlots = new HashSet<int>();
            for (int rosterIndex = 0; rosterIndex < enemyCount; rosterIndex++)
            {
                CombatParticipant enemy = _battle.Enemies[rosterIndex];
                int slotIndex = ResolveHudSlotIndex(rosterIndex, slotCount, usedFormationSlots);
                CombatParticipantSnapshot snapshot = _combatViewModel.TryGetParticipantSnapshot(
                    enemy.Id,
                    out CombatParticipantSnapshot participantSnapshot)
                    ? participantSnapshot
                    : new CombatParticipantSnapshot(enemy.CurrentHp, enemy.Shield);
                bool selected = _selectedEnemyId.IsValid && _selectedEnemyId.Value == enemy.Id.Value;
                string deadSuffix = enemy.IsDead ? " [DOWN]" : string.Empty;
                _view.SetEnemySlot(
                    slotIndex,
                    $"{GetEncounterNode().DisplayName} #{rosterIndex + 1}{deadSuffix}\n" +
                    $"{snapshot.Hp}/{enemy.MaxHp}  SH {snapshot.Shield}",
                    snapshot.Hp,
                    enemy.MaxHp,
                    selected,
                    !enemy.IsDead && !_flowController.IsBusy);
            }
        }

        private int ResolveHudSlotIndex(int rosterIndex, int slotCount, HashSet<int> usedFormationSlots)
        {
            int slotIndex = RunEncounterRosterBuilder.ResolveFormationSlot(
                _encounterRoster,
                rosterIndex,
                slotCount);

            if (!usedFormationSlots.Add(slotIndex))
            {
                Debug.LogWarning(
                    $"[RunBattleController] Duplicate formation slot {slotIndex} for roster index {rosterIndex}; using roster index.");
                slotIndex = Mathf.Clamp(rosterIndex, 0, slotCount - 1);
                usedFormationSlots.Add(slotIndex);
            }

            return slotIndex;
        }

        private RectTransform ResolveEnemyDamageAnchor(int slotIndex)
        {
            RectTransform anchor = _view.GetEnemyDamageAnchor(slotIndex);
            if (anchor != null)
            {
                return anchor;
            }

            return ResolveDamageAnchor(
                _floatingTextRoot,
                $"runtime-monster-{slotIndex}-damage-anchor",
                ResolveEnemyDamageAnchorPosition(slotIndex));
        }

        private static Vector2 ResolveEnemyDamageAnchorPosition(int slotIndex)
        {
            float spacing = 300f;
            float startX = -(RunBattleView.FormationHudSlotCount - 1) * spacing * 0.5f;
            return new Vector2(startX + (slotIndex * spacing), 40f);
        }

        private void HandleEnemySelected(CombatParticipantId enemyId)
        {
            if (_flowController.IsBusy)
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

        private string FormatUpcomingEnemyAction()
        {
            if (_battle.UpcomingEnemyActions.Count == 0)
            {
                return "none";
            }

            CombatEffect effect = _battle.UpcomingEnemyActions[0];
            return $"{effect.Kind} {effect.Amount}";
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

        private static string FormatRequest(SlotCombatRequest request)
        {
            return
                $"DMG {request.Damage} / DEF {request.Defense}\n" +
                $"HIT {request.AttackCount} / HEAL {request.HealAmount}";
        }

        private static string FormatSlotSymbol(SlotSymbolType symbol)
        {
            switch (symbol)
            {
                case SlotSymbolType.Sword:
                    return "SWORD";
                case SlotSymbolType.Shield:
                    return "SHIELD";
                case SlotSymbolType.Heart:
                    return "HEART";
                case SlotSymbolType.Coin:
                    return "COIN";
                case SlotSymbolType.Gem:
                    return "GEM";
                case SlotSymbolType.Skull:
                    return "SKULL";
                default:
                    return symbol.ToString().ToUpperInvariant();
            }
        }

        private static RunMapNodeDefinition GetEncounterNode()
        {
            return GameFlowSession.CurrentEncounterNode ?? GameFlowSession.CurrentMapNode;
        }

        private static void LoadRewardScene()
        {
            SceneManager.LoadScene(GameFlowSceneNames.RunReward);
        }

        private static void ReturnToStart()
        {
            SceneManager.LoadScene(GameFlowSceneNames.GameStart);
        }

    }
}
