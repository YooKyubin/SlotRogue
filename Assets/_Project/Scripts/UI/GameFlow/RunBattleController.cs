using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
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
        private RectTransform _playerDamageAnchor = null!;
        private RectTransform _monsterDamageAnchor = null!;

        [SerializeField] private RunBattleView _view;
        [SerializeField] private FloatingDamageTextView _floatingDamageTextPrefab;
        [SerializeField] private MonsterDefinition _monsterDefinition;

        private RunCombatRequestResult _lastRequestResult;
        private bool _battleCompleted;

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
            var monster = new CombatParticipant(ResolveMonsterMaxHp(encounterNode));
            MonsterTurnSchedule schedule = ResolveMonsterTurnSchedule(encounterNode, floor);

            _battle.StartBattle(player, monster, schedule);
            _combatViewModel.SyncFrom(_battle);
            _eventLogger.LogEventsSince(_battle, eventCursor: 0);
        }

        private void InitializePresentationStack()
        {
            _presentationCts = new CancellationTokenSource();
            Transform floatingTextRoot = ResolveFloatingTextRoot();
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

        private static int GetMonsterMaxHp(RunMapNodeDefinition encounterNode)
        {
            int floor = Mathf.Max(1, encounterNode.Floor);

            switch (encounterNode.NodeType)
            {
                case RunMapNodeType.Elite:
                    return 32 + (floor * 8);
                case RunMapNodeType.Boss:
                    return 46 + (floor * 10);
                default:
                    return 22 + (floor * 6);
            }
        }

        private int ResolveMonsterMaxHp(RunMapNodeDefinition encounterNode)
        {
            if (_monsterDefinition != null)
            {
                return Mathf.Max(1, _monsterDefinition.maxHp);
            }

            return GetMonsterMaxHp(encounterNode);
        }

        private static MonsterTurnSchedule CreateMonsterTurnSchedule(
            RunMapNodeDefinition encounterNode,
            int floor)
        {
            if (encounterNode.NodeType == RunMapNodeType.Boss)
            {
                return new MonsterTurnSchedule(
                    new[] { new CombatEffect(CombatEffectKind.Damage, 6 + floor, CombatEffectTarget.Enemy) },
                    new[] { new CombatEffect(CombatEffectKind.Shield, 5 + floor, CombatEffectTarget.Self) },
                    new[] { new CombatEffect(CombatEffectKind.Damage, 9 + floor, CombatEffectTarget.Enemy) });
            }

            if (encounterNode.NodeType == RunMapNodeType.Elite)
            {
                return new MonsterTurnSchedule(
                    new[] { new CombatEffect(CombatEffectKind.Damage, 5 + floor, CombatEffectTarget.Enemy) },
                    new[] { new CombatEffect(CombatEffectKind.Shield, 4 + floor, CombatEffectTarget.Self) },
                    new[] { new CombatEffect(CombatEffectKind.Damage, 7 + floor, CombatEffectTarget.Enemy) });
            }

            return new MonsterTurnSchedule(
                new[] { new CombatEffect(CombatEffectKind.Damage, 3 + floor, CombatEffectTarget.Enemy) },
                new[] { new CombatEffect(CombatEffectKind.Shield, 2 + floor, CombatEffectTarget.Self) },
                new[] { new CombatEffect(CombatEffectKind.Damage, 5 + floor, CombatEffectTarget.Enemy) });
        }

        private MonsterTurnSchedule ResolveMonsterTurnSchedule(
            RunMapNodeDefinition encounterNode,
            int floor)
        {
            if (_monsterDefinition != null && _monsterDefinition.turnPattern != null)
            {
                return MonsterTurnScheduleFactory.FromPattern(_monsterDefinition.turnPattern);
            }

            return CreateMonsterTurnSchedule(encounterNode, floor);
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
            CombatEffect[] playerEffects = _converter.Convert(request);
            var context = new PresentationContext(request.IsCritical, request.PatternName);
            int eventCursor = _eventLogger.CaptureEventCursor(_battle);

            SetSpinInteractable(false);

            try
            {
                BattleApplyResult result = await _flowController.RunTurnAsync(
                    _battle,
                    playerEffects,
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
            CombatParticipant monster = _battle.Monster;

            _view.SetPlayerHpFill(_combatViewModel.PlayerHp, player.MaxHp);
            _view.SetPlayerShieldFill(_combatViewModel.PlayerShield, Mathf.Max(1, player.MaxHp));
            _view.SetMonsterHpFill(_combatViewModel.MonsterHp, monster.MaxHp);
            _view.SetPlayerHud(
                $"{_combatViewModel.PlayerHp}/{player.MaxHp}\n" +
                $"{_combatViewModel.PlayerShield}");
            _view.SetMonsterHud(
                $"{GetEncounterNode().DisplayName}\n" +
                $"{_combatViewModel.MonsterHp}/{monster.MaxHp}  SH {_combatViewModel.MonsterShield}");
            _view.SetEnemyIntent($"ENEMY INTENT: {FormatUpcomingEnemyAction()}");
            _view.SetStatus(
                $"{_battle.CurrentPhase}\n" +
                $"Turn {_battle.UpcomingMonsterTurnIndex}\n" +
                $"Bonus D+{GameFlowSession.DamageBonus} / S+{GameFlowSession.DefenseBonus}");

            UpdateSpinButtonState();
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
