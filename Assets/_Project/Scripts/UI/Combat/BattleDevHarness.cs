using System;
using System.Collections.Generic;
using SlotRogue.Core.Combat;
using SlotRogue.Slot.Data;
using UnityEngine;
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
        [SerializeField] private int _monsterMaxHp = 10;
        [SerializeField] private int _monsterCurrentHp = -1;

        [Header("Monster Turn Schedule")]
        [SerializeField] private MonsterTurnDefinition[] _monsterTurnSchedule = Array.Empty<MonsterTurnDefinition>();

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
        private Text _statusText = null!;

        public BattleSystem Battle => _battle;

        private void Awake()
        {
            CreateEventSystemIfNeeded();
            CreateUi();
            RefreshStatusText();
        }

        private void Reset()
        {
            _monsterTurnSchedule = new[]
            {
                new MonsterTurnDefinition
                {
                    actions = new[]
                    {
                        new CombatEffectDefinition
                        {
                            kind = CombatEffectKind.Damage,
                            amount = 2,
                            target = CombatEffectTarget.Enemy,
                        },
                    },
                },
                new MonsterTurnDefinition
                {
                    actions = new[]
                    {
                        new CombatEffectDefinition
                        {
                            kind = CombatEffectKind.Damage,
                            amount = 4,
                            target = CombatEffectTarget.Enemy,
                        },
                    },
                },
                new MonsterTurnDefinition
                {
                    actions = new[]
                    {
                        new CombatEffectDefinition
                        {
                            kind = CombatEffectKind.Damage,
                            amount = 6,
                            target = CombatEffectTarget.Enemy,
                        },
                    },
                },
            };
        }

        public void StartBattle()
        {
            var player = new CombatParticipant(_playerMaxHp, _playerCurrentHp);
            var monster = new CombatParticipant(_monsterMaxHp, _monsterCurrentHp);
            MonsterTurnSchedule schedule = BuildMonsterTurnSchedule();

            _battle.StartBattle(player, monster, schedule);
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

        private MonsterTurnSchedule BuildMonsterTurnSchedule()
        {
            if (_monsterTurnSchedule == null || _monsterTurnSchedule.Length == 0)
            {
                return new MonsterTurnSchedule(
                    new[] { new CombatEffect(CombatEffectKind.Damage, 2, CombatEffectTarget.Enemy) });
            }

            var turnSets = new CombatEffect[_monsterTurnSchedule.Length][];
            for (int turnIndex = 0; turnIndex < _monsterTurnSchedule.Length; turnIndex++)
            {
                turnSets[turnIndex] = BuildTurnActions(_monsterTurnSchedule[turnIndex].actions);
            }

            return new MonsterTurnSchedule(turnSets);
        }

        private static CombatEffect[] BuildTurnActions(CombatEffectDefinition[] actions)
        {
            if (actions == null || actions.Length == 0)
            {
                return Array.Empty<CombatEffect>();
            }

            var effects = new CombatEffect[actions.Length];
            for (int index = 0; index < actions.Length; index++)
            {
                effects[index] = actions[index].ToCombatEffect();
            }

            return effects;
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
                $"Upcoming monster turn #{_battle.UpcomingMonsterTurnIndex}: {upcomingSummary}\n" +
                $"Player: HP {player.CurrentHp}/{player.MaxHp}, Shield {player.Shield}\n" +
                $"Monster: HP {monster.CurrentHp}/{monster.MaxHp}, Shield {monster.Shield}\n" +
                $"Request: dmg={_requestDamage}, def={_requestDefense}, hits={_requestAttackCount}, " +
                $"heal={_requestHealAmount}, crit={_requestIsCritical}, pattern={_requestPatternName}";
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

            _statusText = CreateText(root, font, "Status", 24, 760f, TextAnchor.UpperLeft);
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _statusText.verticalOverflow = VerticalWrapMode.Overflow;
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
