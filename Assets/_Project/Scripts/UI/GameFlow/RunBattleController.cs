using System;
using System.Text;
using System.Collections.Generic;
using SlotRogue.Core.Combat;
using SlotRogue.Slot.Core;
using SlotRogue.Slot.Data;
using SlotRogue.Slot.ViewModels;
using SlotRogue.UI.Combat;
using SlotRogue.UI.SlotPresentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleController : MonoBehaviour
    {
        private readonly BattleSystem _battle = new();
        private readonly SlotMachineViewModel _slotViewModel = new();
        private readonly SlotPatternResolver _presentationPatternResolver = new();
        private readonly SlotCombatRequestToCombatEffectsConverter _converter = new();
        private readonly CombatEventConsoleLogger _eventLogger = new();
        private readonly RunCombatRequestResolver _requestResolver = new();

        [SerializeField] private RunBattleView _view;
        [SerializeField] private SlotPresentationManager _presentationManager;
        [SerializeField] private SlotLeverView _spinLeverView;

        private RunCombatRequestResult _lastRequestResult;
        private bool _battleCompleted;
        private bool _isPresentingSpin;
        private bool _isPresentationEventBound;
        private int _presentedAttackValue;

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

            if (_presentationManager == null)
            {
                _presentationManager = GetComponentInChildren<SlotPresentationManager>(true);
            }

            EnsureResponsiveLayout();
            BindPresentationEvents();
            EnsureSpinLeverView();

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

        private void EnsureResponsiveLayout()
        {
            RunBattleResponsiveLayout responsiveLayout = GetComponent<RunBattleResponsiveLayout>();

            if (responsiveLayout == null)
            {
                responsiveLayout = gameObject.AddComponent<RunBattleResponsiveLayout>();
            }

            responsiveLayout.ApplyNow();
        }

        private void OnDestroy()
        {
            if (_presentationManager != null && _isPresentationEventBound)
            {
                _presentationManager.PatternStepStarted -= HandlePatternStepStarted;
            }
        }

        private void BindPresentationEvents()
        {
            if (_presentationManager == null || _isPresentationEventBound)
            {
                return;
            }

            _presentationManager.PatternStepStarted += HandlePatternStepStarted;
            _isPresentationEventBound = true;
        }

        private void StartBattle()
        {
            RunMapNodeDefinition encounterNode = GetEncounterNode();
            int floor = Mathf.Max(1, encounterNode.Floor);
            var player = new CombatParticipant(GameFlowSession.PlayerMaxHp, GameFlowSession.PlayerCurrentHp);
            var monster = new CombatParticipant(GetMonsterMaxHp(encounterNode));
            MonsterTurnSchedule schedule = CreateMonsterTurnSchedule(encounterNode, floor);

            _battle.StartBattle(player, monster, schedule);
            _eventLogger.LogEventsSince(_battle, eventCursor: 0);
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

        private void HandleSpinClicked()
        {
            if (_battleCompleted || _isPresentingSpin || !_battle.CanApplyPlayerTurn)
            {
                RefreshStatusText();
                return;
            }

            _slotViewModel.Spin();
            ArtifactDefinitionSO artifact = StarterArtifactCatalog.GetById(GameFlowSession.SelectedArtifactId);
            _lastRequestResult = _requestResolver.Resolve(
                _slotViewModel.CurrentPatternResult,
                _slotViewModel.CurrentCombatRequest,
                artifact,
                GameFlowSession.DamageBonus,
                GameFlowSession.DefenseBonus);

            RefreshSlotCells(_slotViewModel.CurrentSpinResult);
            _view.SetAttackResult("ATK -");
            _view.SetSlotResult("RESOLVING SPIN...");
            _spinLeverView?.PlayDown();

            IReadOnlyList<SlotPatternMatch> presentationMatches =
                _presentationPatternResolver.ResolveAll(_slotViewModel.CurrentSpinResult);
            SlotPresentationResult presentationResult = BuildPresentationResult(artifact, presentationMatches);

            if (_presentationManager != null)
            {
                _presentedAttackValue = presentationMatches.Count > 0 ? 0 : SlotCombatRequest.BaseAttackDamage;
                _view.SetAttackResult($"ATK {_presentedAttackValue}");
                _isPresentingSpin = true;
                _view.SpinButton.interactable = false;
                _presentationManager.Play(presentationResult, _ => ApplyPresentedSpinResult());
                return;
            }

            ApplyPresentedSpinResult();
        }

        private void ApplyPresentedSpinResult()
        {
            if (_lastRequestResult == null)
            {
                _isPresentingSpin = false;
                _spinLeverView?.PlayUp();
                RefreshStatusText();
                return;
            }

            CombatEffect[] playerEffects = _converter.Convert(_lastRequestResult.FinalRequest);
            int eventCursor = _eventLogger.CaptureEventCursor(_battle);
            BattleApplyResult result = _battle.ApplyPlayerTurn(playerEffects);

            if (result.Accepted)
            {
                _eventLogger.LogEventsSince(_battle, eventCursor, _lastRequestResult.FinalRequest);
            }

            _isPresentingSpin = false;
            _spinLeverView?.PlayUp();
            RefreshSlotResultText();
            RefreshStatusText();
            HandleBattleEndIfNeeded();

            if (!_battleCompleted && _view.SpinButton != null)
            {
                _view.SpinButton.interactable = _battle.CanApplyPlayerTurn;
            }
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

        private void EnsureSpinLeverView()
        {
            if (_spinLeverView != null)
            {
                _spinLeverView.SetUpImmediate();
                return;
            }

            if (_view == null || _view.SpinButton == null)
            {
                return;
            }

            _spinLeverView = _view.GetComponentInChildren<SlotLeverView>(true);

            _spinLeverView?.SetUpImmediate();
        }

        private void RefreshSlotResultText()
        {
            if (_lastRequestResult == null)
            {
                _view.SetAttackResult("ATK -");
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
            _view.SetAttackResult($"ATK {attackResult}");

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

            _view.SetPlayerHpFill(player.CurrentHp, player.MaxHp);
            _view.SetPlayerShieldFill(player.Shield, Mathf.Max(1, player.MaxHp));
            _view.SetMonsterHpFill(monster.CurrentHp, monster.MaxHp);
            _view.SetPlayerHud(
                $"{player.CurrentHp}/{player.MaxHp}\n" +
                $"{player.Shield}");
            _view.SetMonsterHud(
                $"{GetEncounterNode().DisplayName}\n" +
                $"{monster.CurrentHp}/{monster.MaxHp}  SH {monster.Shield}");
            _view.SetEnemyIntent($"ENEMY INTENT: {FormatUpcomingEnemyAction()}");
            _view.SetStatus(
                $"{_battle.CurrentPhase}\n" +
                $"Turn {_battle.UpcomingMonsterTurnIndex}\n" +
                $"Bonus D+{GameFlowSession.DamageBonus} / S+{GameFlowSession.DefenseBonus}");
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

        private SlotPresentationResult BuildPresentationResult(
            ArtifactDefinitionSO artifact,
            IReadOnlyList<SlotPatternMatch> patternMatches)
        {
            return new SlotPresentationResult(
                _slotViewModel.CurrentSpinResult,
                BuildPatternPresentations(patternMatches),
                BuildRelicPresentations(artifact),
                BuildFinalPresentation());
        }

        private static SlotPatternPresentationResult[] BuildPatternPresentations(
            IReadOnlyList<SlotPatternMatch> matches)
        {
            if (matches == null || matches.Count == 0)
            {
                return new SlotPatternPresentationResult[0];
            }

            var results = new SlotPatternPresentationResult[matches.Count];

            for (int index = 0; index < matches.Count; index++)
            {
                SlotPatternMatch match = matches[index];
                int[] highlightedIndices = BuildHighlightedCellIndices(match);

                results[index] = new SlotPatternPresentationResult(
                    match.PresentationTitle,
                    match.Symbol,
                    match.MatchedCells.Count > 0 ? match.MatchedCells[0].Row : -1,
                    match.MatchedCells.Count > 0 ? match.MatchedCells[0].Col : -1,
                    match.MatchedCells.Count,
                    highlightedIndices,
                    BuildPatternDescription(match),
                    BuildPatternBonusText(match),
                    match.Definition.IsJackpot,
                    index,
                    match.CalculatedValue);
            }

            return results;
        }

        private void HandlePatternStepStarted(SlotPatternPresentationResult pattern)
        {
            if (pattern == null || !_isPresentingSpin)
            {
                return;
            }

            _presentedAttackValue += Mathf.Max(0, pattern.BonusValue);
            _view.SetAttackResult($"ATK {_presentedAttackValue}");
        }

        private SlotRelicTriggerPresentationResult[] BuildRelicPresentations(ArtifactDefinitionSO artifact)
        {
            var relics = new List<SlotRelicTriggerPresentationResult>();

            if (_lastRequestResult != null && _lastRequestResult.StarterArtifactActivation.Activated)
            {
                relics.Add(new SlotRelicTriggerPresentationResult(
                    artifact != null ? artifact.ArtifactId : _lastRequestResult.StarterArtifactActivation.ArtifactName,
                    _lastRequestResult.StarterArtifactActivation.ArtifactName,
                    null,
                    _lastRequestResult.StarterArtifactActivation.Description,
                    BuildArtifactValueText(artifact)));
            }

            if (_lastRequestResult != null && !string.IsNullOrEmpty(_lastRequestResult.RunBonusSummary))
            {
                relics.Add(new SlotRelicTriggerPresentationResult(
                    "RunBonus",
                    "Run Bonus",
                    null,
                    "Previously selected rewards modify this spin.",
                    _lastRequestResult.RunBonusSummary));
            }

            return relics.ToArray();
        }

        private SlotFinalPresentationResult BuildFinalPresentation()
        {
            SlotCombatRequest request = _lastRequestResult != null
                ? _lastRequestResult.FinalRequest
                : SlotCombatRequest.Empty;

            return new SlotFinalPresentationResult(
                request.Damage,
                request.Defense,
                request.AttackCount,
                request.HealAmount,
                FormatRequest(request));
        }

        private static int[] BuildHighlightedCellIndices(SlotPatternMatch match)
        {
            if (match == null || match.MatchedCells == null || match.MatchedCells.Count == 0)
            {
                return new int[0];
            }

            var indices = new int[match.MatchedCells.Count];

            for (int index = 0; index < match.MatchedCells.Count; index++)
            {
                SlotCell cell = match.MatchedCells[index];
                indices[index] = SlotSpinResult.ToIndex(cell.Col, cell.Row);
            }

            return indices;
        }

        private static string BuildPatternDescription(SlotPatternMatch match)
        {
            return $"{match.Symbol} x{match.MatchedCells.Count} / Multiplier x{match.Multiplier:0.0}";
        }

        private static string BuildPatternBonusText(SlotPatternMatch match)
        {
            return $"+{match.CalculatedValue} pts";
        }

        private static string BuildArtifactValueText(ArtifactDefinitionSO artifact)
        {
            if (artifact == null)
            {
                return string.Empty;
            }

            switch (artifact.EffectKind)
            {
                case ArtifactEffectKind.BonusDamage:
                    return $"+{artifact.BonusAmount} damage";
                case ArtifactEffectKind.BonusDefense:
                    return $"+{artifact.BonusAmount} defense";
                case ArtifactEffectKind.BonusHeal:
                    return $"+{artifact.BonusAmount} heal";
                case ArtifactEffectKind.ApplyBurn:
                    return $"Burn {artifact.StatusDuration} turns / {artifact.StatusMagnitude} dmg";
                case ArtifactEffectKind.ApplyFreeze:
                    return $"Freeze {artifact.StatusDuration} turn(s)";
                case ArtifactEffectKind.ApplyPoison:
                    return $"+{artifact.StatusMagnitude} Poison stack(s)";
                default:
                    return artifact.Description;
            }
        }

        private static string FormatSlotSymbol(SlotSymbolType symbol)
        {
            switch (symbol)
            {
                case SlotSymbolType.Cherry:
                    return "CHERRY";
                case SlotSymbolType.Seven:
                    return "SEVEN";
                case SlotSymbolType.Grape:
                    return "GRAPE";
                case SlotSymbolType.Bell:
                    return "BELL";
                case SlotSymbolType.Clover:
                    return "CLOVER";
                case SlotSymbolType.Lemon:
                    return "LEMON";
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

    [DisallowMultipleComponent]
    public sealed class RunBattleResponsiveLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform _layoutRoot;
        [SerializeField] private Vector2 _referenceResolution = new(1080f, 1920f);
        [SerializeField, Range(0f, 1f)] private float _canvasMatchWidthOrHeight = 0.5f;
        [SerializeField] private bool _applySafeArea = true;

        public void ApplyNow()
        {
            EnsureCaptured();
            ApplyLayout();
        }

        private void Awake()
        {
            ConfigureCanvasScaler();
            EnsureCaptured();
        }

        private void Start()
        {
            ApplyLayout();
        }

        private void OnEnable()
        {
            _lastScreenWidth = -1;
            _lastScreenHeight = -1;
            _lastSafeArea = Rect.zero;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
            {
                ApplyLayout();
            }
        }

        private void Update()
        {
            if (Screen.width == _lastScreenWidth &&
                Screen.height == _lastScreenHeight &&
                Screen.safeArea == _lastSafeArea)
            {
                return;
            }

            ApplyLayout();
        }

        private void ConfigureCanvasScaler()
        {
            CanvasScaler scaler = GetComponentInParent<CanvasScaler>();

            if (scaler == null)
            {
                return;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = _referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = _canvasMatchWidthOrHeight;
        }

        private void EnsureCaptured()
        {
            if (_captured)
            {
                return;
            }

            _layoutRoot = ResolveLayoutRoot();

            if (_layoutRoot == null)
            {
                return;
            }

            EnsureSpinButtonText();
            NormalizeSlotIconRects();

            _targets.Clear();
            CaptureTarget("Scene Background Image", AnchorMode.FullCanvas);
            CaptureTarget("Inside Texture Backdrop", AnchorMode.FullCanvas);
            CaptureTarget("Slot Presentation Layer", AnchorMode.FullCanvas);
            CaptureTarget("Currency HUD", AnchorMode.TopLeft);
            CaptureTarget("Pause Button", AnchorMode.TopRight);
            CaptureTarget("Slot Machine Panel", AnchorMode.BottomCenter);
            CaptureTarget("Player HP Gauge", AnchorMode.BottomCenter);
            CaptureTarget("Attack Power HUD", AnchorMode.BottomCenter);
            CaptureTarget("Spin Button", AnchorMode.BottomCenter);
            CaptureTarget("Spin Lever", AnchorMode.BottomCenter);
            CaptureTarget("Claim Reward Button", AnchorMode.BottomCenter);
            CaptureTarget("Return To Start Button", AnchorMode.BottomCenter);
            CaptureTarget("Relic Inventory Origin", AnchorMode.BottomLeft);
            CaptureTarget("Potion Slot 1", AnchorMode.BottomRight);
            CaptureTarget("Potion Slot 2", AnchorMode.BottomRight);
            CaptureTarget("Pattern Presentation Panel", AnchorMode.Center);
            CaptureTarget("Final Result Presentation Panel", AnchorMode.Center);
            CaptureTarget("Relic Presentation Panel", AnchorMode.BottomLeft);

            _captured = true;
        }

        private void EnsureSpinButtonText()
        {
            RectTransform spinButton = FindTarget("Spin Button");

            if (spinButton == null || FindChild(spinButton, "Spin Button Text") != null)
            {
                return;
            }

            var textObject = new GameObject("Spin Button Text", typeof(RectTransform), typeof(Text));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(spinButton, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(24f, 30f);
            rect.offsetMax = new Vector2(-24f, -24f);
            rect.localScale = Vector3.one;

            Text text = textObject.GetComponent<Text>();
            text.font = ResolveDefaultFont();
            text.text = "SPIN";
            text.fontSize = 56;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color32(238, 248, 255, 255);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void NormalizeSlotIconRects()
        {
            RectTransform[] rects = _layoutRoot.GetComponentsInChildren<RectTransform>(true);

            for (int index = 0; index < rects.Length; index++)
            {
                if (!rects[index].gameObject.name.StartsWith("Slot Cell Icon ", StringComparison.Ordinal))
                {
                    continue;
                }

                rects[index].anchorMin = new Vector2(0.5f, 0.5f);
                rects[index].anchorMax = new Vector2(0.5f, 0.5f);
                rects[index].anchoredPosition = Vector2.zero;
                rects[index].sizeDelta = new Vector2(32f, 32f);
                rects[index].localScale = Vector3.one;
            }
        }

        private static RectTransform FindChild(RectTransform parent, string childName)
        {
            for (int index = 0; index < parent.childCount; index++)
            {
                RectTransform child = parent.GetChild(index) as RectTransform;

                if (child != null && child.gameObject.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static Font ResolveDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private RectTransform ResolveLayoutRoot()
        {
            if (_layoutRoot != null)
            {
                return _layoutRoot;
            }

            RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);

            for (int index = 0; index < rects.Length; index++)
            {
                if (rects[index].gameObject.name == "Run Battle Root")
                {
                    return rects[index];
                }
            }

            return transform as RectTransform;
        }

        private void CaptureTarget(string targetName, AnchorMode mode)
        {
            RectTransform rect = FindTarget(targetName);

            if (rect == null)
            {
                return;
            }

            RectTransform parent = rect.parent as RectTransform;

            if (parent == null)
            {
                return;
            }

            Rect parentRect = parent.rect;
            Vector2 pivotPosition = parent.InverseTransformPoint(rect.position);
            Vector2 size = rect.rect.size;

            _targets.Add(new CapturedTarget(
                rect,
                mode,
                size,
                pivotPosition.x - parentRect.xMin,
                parentRect.xMax - pivotPosition.x,
                pivotPosition.y - parentRect.yMin,
                parentRect.yMax - pivotPosition.y,
                pivotPosition - parentRect.center));
        }

        private RectTransform FindTarget(string targetName)
        {
            RectTransform[] rects = _layoutRoot.GetComponentsInChildren<RectTransform>(true);

            for (int index = 0; index < rects.Length; index++)
            {
                if (rects[index].gameObject.name == targetName)
                {
                    return rects[index];
                }
            }

            return null;
        }

        private void ApplyLayout()
        {
            if (!_captured)
            {
                EnsureCaptured();
            }

            if (!_captured)
            {
                return;
            }

            ConfigureCanvasScaler();

            for (int index = 0; index < _targets.Count; index++)
            {
                ApplyTarget(_targets[index]);
            }

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastSafeArea = Screen.safeArea;
        }

        private void ApplyTarget(CapturedTarget target)
        {
            if (target.Rect == null)
            {
                return;
            }

            RectTransform parent = target.Rect.parent as RectTransform;

            if (parent == null)
            {
                return;
            }

            if (target.Mode == AnchorMode.FullCanvas)
            {
                target.Rect.anchorMin = Vector2.zero;
                target.Rect.anchorMax = Vector2.one;
                target.Rect.offsetMin = Vector2.zero;
                target.Rect.offsetMax = Vector2.zero;
                return;
            }

            Rect safeRect = ResolveSafeRect(parent);
            Vector2 pivotPosition = ResolvePivotPosition(target, safeRect);
            Rect parentRect = parent.rect;

            target.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            target.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            target.Rect.sizeDelta = target.Size;
            target.Rect.anchoredPosition = pivotPosition - parentRect.center;
        }

        private Vector2 ResolvePivotPosition(CapturedTarget target, Rect safeRect)
        {
            switch (target.Mode)
            {
                case AnchorMode.TopLeft:
                    return new Vector2(safeRect.xMin + target.LeftOffset, safeRect.yMax - target.TopOffset);
                case AnchorMode.TopRight:
                    return new Vector2(safeRect.xMax - target.RightOffset, safeRect.yMax - target.TopOffset);
                case AnchorMode.BottomLeft:
                    return new Vector2(safeRect.xMin + target.LeftOffset, safeRect.yMin + target.BottomOffset);
                case AnchorMode.BottomRight:
                    return new Vector2(safeRect.xMax - target.RightOffset, safeRect.yMin + target.BottomOffset);
                case AnchorMode.BottomCenter:
                    return new Vector2(safeRect.center.x + target.CenterOffset.x, safeRect.yMin + target.BottomOffset);
                default:
                    return safeRect.center + target.CenterOffset;
            }
        }

        private Rect ResolveSafeRect(RectTransform parent)
        {
            Rect parentRect = parent.rect;

            if (!_applySafeArea || Screen.width <= 0 || Screen.height <= 0)
            {
                return parentRect;
            }

            Rect safeArea = Screen.safeArea;

            if (safeArea.width <= 0f || safeArea.height <= 0f)
            {
                return parentRect;
            }

            float xMin = parentRect.xMin + (safeArea.xMin / Screen.width * parentRect.width);
            float xMax = parentRect.xMin + (safeArea.xMax / Screen.width * parentRect.width);
            float yMin = parentRect.yMin + (safeArea.yMin / Screen.height * parentRect.height);
            float yMax = parentRect.yMin + (safeArea.yMax / Screen.height * parentRect.height);

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private enum AnchorMode
        {
            FullCanvas,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            BottomCenter,
            Center
        }

        private sealed class CapturedTarget
        {
            public CapturedTarget(
                RectTransform rect,
                AnchorMode mode,
                Vector2 size,
                float leftOffset,
                float rightOffset,
                float bottomOffset,
                float topOffset,
                Vector2 centerOffset)
            {
                Rect = rect;
                Mode = mode;
                Size = size;
                LeftOffset = leftOffset;
                RightOffset = rightOffset;
                BottomOffset = bottomOffset;
                TopOffset = topOffset;
                CenterOffset = centerOffset;
            }

            public RectTransform Rect { get; }

            public AnchorMode Mode { get; }

            public Vector2 Size { get; }

            public float LeftOffset { get; }

            public float RightOffset { get; }

            public float BottomOffset { get; }

            public float TopOffset { get; }

            public Vector2 CenterOffset { get; }
        }

        private readonly List<CapturedTarget> _targets = new();
        private bool _captured;
        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;
        private Rect _lastSafeArea;
    }
}
