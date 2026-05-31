using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using SlotRogue.Data.Combat;
using SlotRogue.Slot.Data;
using SlotRogue.UI.Combat.Presentation;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlotRogue.UI.Combat
{
    public sealed class BattleDevHarness : MonoBehaviour
    {
        private const string InputSystemUiInputModuleTypeName =
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";

        [Header("Player")]
        [SerializeField] private int _playerMaxHp = 30;
        [SerializeField] private int _playerCurrentHp = -1;

        [Header("Monster")]
        [SerializeField] private MonsterDefinition _monsterDefinition;
        [SerializeField] private int _monsterCurrentHp = -1;

        [Header("SlotCombatRequest (test)")]
        [SerializeField] private int _requestDamage = 5;
        [SerializeField] private int _requestDefense;
        [SerializeField] private int _requestAttackCount = 1;
        [SerializeField] private int _requestHealAmount;
        [SerializeField] private bool _requestIsCritical;
        [SerializeField] private string _requestPatternName = "Dev Attack";

        private readonly BattleSystem _battle = new();
        private readonly SlotCombatRequestToCombatEffectsConverter _converter = new();
        private readonly CombatEventConsoleLogger _eventLogger = new();
        private readonly CombatViewModel _combatViewModel = new();
        private BattleFlowController _flowController = null!;
        private CombatPresentationHost _presentationHost = null!;
        private CancellationTokenSource _presentationCts = null!;
        private Transform _floatingTextRoot = null!;
        private Text _statusText = null!;

        public BattleSystem Battle => _battle;

        private void Awake()
        {
            _presentationCts = new CancellationTokenSource();
            CreateEventSystemIfNeeded();
            CreateUi();
            _presentationHost = new CombatPresentationHost(
                gameObject,
                _statusText,
                _floatingTextRoot,
                GetDefaultFont(),
                RefreshStatusText);
            CombatPresentationPipeline pipeline = CombatPresentationPipeline.CreateDefault(_presentationHost);
            _flowController = new BattleFlowController(pipeline, _combatViewModel);
            RefreshStatusText();
        }

        private void OnDisable()
        {
#if DOTWEEN
            gameObject.DOKill(true);
#endif
        }

        private void OnDestroy()
        {
            _presentationCts?.Cancel();
            _presentationCts?.Dispose();
            _presentationCts = null!;
        }

        public void StartBattle()
        {
            if (_monsterDefinition == null)
            {
                Debug.LogError("[BattleDevHarness] MonsterDefinition SO is not assigned.");
                RefreshStatusText();
                return;
            }

            if (_monsterDefinition.turnPattern == null)
            {
                Debug.LogError(
                    $"[BattleDevHarness] MonsterDefinition '{_monsterDefinition.name}' has no turnPattern assigned.");
                RefreshStatusText();
                return;
            }

            var player = new CombatParticipant(_playerMaxHp, _playerCurrentHp);
            var monster = new CombatParticipant(_monsterDefinition.maxHp, _monsterCurrentHp);
            MonsterTurnSchedule schedule =
                MonsterTurnScheduleFactory.FromPattern(_monsterDefinition.turnPattern);

            _battle.StartBattle(player, monster, schedule);
            _combatViewModel.SyncFrom(_battle);
            _eventLogger.LogEventsSince(_battle, eventCursor: 0);
            RefreshStatusText();
        }

        public void ApplyTurn()
        {
            var request = new SlotCombatRequest(
                _requestDamage,
                _requestDefense,
                _requestAttackCount,
                _requestHealAmount,
                _requestIsCritical,
                _requestPatternName);

            CombatEffect[] playerEffects = _converter.Convert(request);
            int eventCursor = _eventLogger.CaptureEventCursor(_battle);
            BattleApplyResult result = _battle.ApplyPlayerTurn(playerEffects);

            if (!result.Accepted)
            {
                Debug.Log(
                    $"[BattleDevHarness] ApplyTurn rejected. Phase={result.Phase}, EndReason={result.EndReason}");
                RefreshStatusText();
                return;
            }

            _eventLogger.LogEventsSince(_battle, eventCursor, request);
            RefreshStatusText();
        }

        public void ApplyTurnWithPresentation()
        {
            ApplyTurnWithPresentationAsync().Forget();
        }

        private async UniTaskVoid ApplyTurnWithPresentationAsync()
        {
            if (_battle.CurrentPhase == BattlePhase.NotInBattle)
            {
                Debug.LogWarning("[BattleDevHarness] ApplyTurnWithPresentation ignored — battle not started.");
                RefreshStatusText();
                return;
            }

            if (_flowController.IsBusy)
            {
                Debug.Log("[BattleDevHarness] ApplyTurnWithPresentation rejected — presentation busy.");
                return;
            }

            var request = new SlotCombatRequest(
                _requestDamage,
                _requestDefense,
                _requestAttackCount,
                _requestHealAmount,
                _requestIsCritical,
                _requestPatternName);

            CombatEffect[] playerEffects = _converter.Convert(request);
            var context = new PresentationContext(request.IsCritical, request.PatternName);
            int eventCursor = _eventLogger.CaptureEventCursor(_battle);

            BattleApplyResult result = await _flowController.RunTurnAsync(
                _battle,
                playerEffects,
                context,
                _presentationCts.Token);

            if (!result.Accepted)
            {
                Debug.Log(
                    $"[BattleDevHarness] ApplyTurnWithPresentation rejected. Phase={result.Phase}, " +
                    $"EndReason={result.EndReason}");
                RefreshStatusText();
                return;
            }

            _eventLogger.LogEventsSince(_battle, eventCursor, request);
            RefreshStatusText();
        }

        private void RefreshStatusText()
        {
            if (_statusText == null)
            {
                return;
            }

            if (_battle.CurrentPhase == BattlePhase.NotInBattle)
            {
                _statusText.text =
                    "Phase: NotInBattle\n" +
                    $"Monster: {GetMonsterSourceLabel()}\n" +
                    "Press Start Battle to begin.\n" +
                    $"Next request: dmg={_requestDamage}, def={_requestDefense}, " +
                    $"hits={_requestAttackCount}, heal={_requestHealAmount}, " +
                    $"crit={_requestIsCritical}, pattern={_requestPatternName}";
                return;
            }

            CombatParticipant player = _battle.Player;
            CombatParticipant monster = _battle.Monster;
            IReadOnlyList<CombatEffect> upcoming = _battle.UpcomingEnemyActions;
            string upcomingSummary = upcoming.Count == 0
                ? "none"
                : $"{upcoming[0].Kind} {upcoming[0].Amount}";

            _statusText.text =
                $"Phase: {_battle.CurrentPhase}\n" +
                $"EndReason: {_battle.EndReason}\n" +
                $"Monster: {GetMonsterSourceLabel()}\n" +
                $"Upcoming monster turn #{_battle.UpcomingMonsterTurnIndex}: {upcomingSummary}\n" +
                $"Player: HP {_combatViewModel.PlayerHp}/{player.MaxHp}, " +
                $"Shield {_combatViewModel.PlayerShield}\n" +
                $"Monster: HP {_combatViewModel.MonsterHp}/{monster.MaxHp}, " +
                $"Shield {_combatViewModel.MonsterShield}\n" +
                $"Request: dmg={_requestDamage}, def={_requestDefense}, hits={_requestAttackCount}, " +
                $"heal={_requestHealAmount}, crit={_requestIsCritical}, pattern={_requestPatternName}";
        }

        private string GetMonsterSourceLabel()
        {
            if (_monsterDefinition == null)
            {
                return "(not assigned — assign MonsterDefinition SO)";
            }

            if (_monsterDefinition.turnPattern == null)
            {
                return $"{_monsterDefinition.name} (turnPattern missing)";
            }

            return _monsterDefinition.name;
        }

        private void CreateUi()
        {
            Font font = GetDefaultFont();

            var canvasObject = new GameObject("Battle Dev Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform root = CreateRectTransform("Battle Dev Root", canvasObject.transform);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(900f, 1200f);
            root.anchoredPosition = Vector2.zero;

            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            Text titleText = CreateText(root, font, "Dev Battle", 42, 70f, TextAnchor.MiddleCenter);
            titleText.text = "Dev Battle";

            Button startButton = CreateButton(root, font, "Start Battle", 82f);
            startButton.onClick.AddListener(StartBattle);

            Button applyButton = CreateButton(root, font, "Apply Turn", 82f);
            applyButton.onClick.AddListener(ApplyTurn);

            Button applyPresentationButton = CreateButton(root, font, "Apply Turn (Presentation)", 82f);
            applyPresentationButton.onClick.AddListener(ApplyTurnWithPresentation);

            _statusText = CreateText(root, font, "Status", 24, 760f, TextAnchor.UpperLeft);
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _statusText.verticalOverflow = VerticalWrapMode.Overflow;
            _floatingTextRoot = root;
        }

        private static void CreateEventSystemIfNeeded()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            Type inputModuleType = Type.GetType(InputSystemUiInputModuleTypeName);

            if (inputModuleType != null)
            {
                eventSystemObject.AddComponent(inputModuleType);
                return;
            }
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private static Button CreateButton(Transform parent, Font font, string label, float height)
        {
            RectTransform buttonTransform = CreateRectTransform(label, parent);
            var image = buttonTransform.gameObject.AddComponent<Image>();
            image.color = new Color32(222, 176, 77, 255);

            var button = buttonTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText(buttonTransform, font, label, 30, height, TextAnchor.MiddleCenter);
            RectTransform textTransform = text.rectTransform;
            textTransform.anchorMin = Vector2.zero;
            textTransform.anchorMax = Vector2.one;
            textTransform.offsetMin = Vector2.zero;
            textTransform.offsetMax = Vector2.zero;

            return button;
        }

        private static Text CreateText(
            Transform parent,
            Font font,
            string name,
            int fontSize,
            float height,
            TextAnchor alignment)
        {
            RectTransform transform = CreateRectTransform(name, parent);
            var text = transform.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = name;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            AddLayoutElement(text.gameObject, height);

            return text;
        }

        private static RectTransform CreateRectTransform(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.localScale = Vector3.one;

            return rectTransform;
        }

        private static void AddLayoutElement(GameObject gameObject, float preferredHeight)
        {
            var layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = preferredHeight;
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
    }
}
