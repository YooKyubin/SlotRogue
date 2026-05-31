using System.Text;
using SlotRogue.Core.Combat;
using SlotRogue.Slot.Data;
using SlotRogue.Slot.ViewModels;
using SlotRogue.UI.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleController : MonoBehaviour
    {
        private readonly BattleSystem _battle = new();
        private readonly SlotMachineViewModel _slotViewModel = new();
        private readonly SlotCombatRequestToCombatEffectsConverter _converter = new();
        private readonly CombatEventConsoleLogger _eventLogger = new();
        private readonly RunCombatRequestResolver _requestResolver = new();

        [SerializeField] private RunBattleView _view;

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
            if (_battleCompleted || !_battle.CanApplyPlayerTurn)
            {
                RefreshStatusText();
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

            CombatEffect[] playerEffects = _converter.Convert(_lastRequestResult.FinalRequest);
            int eventCursor = _eventLogger.CaptureEventCursor(_battle);
            BattleApplyResult result = _battle.ApplyPlayerTurn(playerEffects);

            if (result.Accepted)
            {
                _eventLogger.LogEventsSince(_battle, eventCursor, _lastRequestResult.FinalRequest);
            }

            RefreshSlotCells(_slotViewModel.CurrentSpinResult);
            RefreshSlotResultText();
            RefreshStatusText();
            HandleBattleEndIfNeeded();
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
