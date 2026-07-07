// DEV ONLY: This component is for editor/manual combat checks and is not used by the actual in-game flow.
using SlotRogue.Core.Combat;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleStatusEffectDebugButton : MonoBehaviour
    {
        private static readonly DebugStatusButtonDefinition[] EnemyStatusButtons =
        {
            new(StatusEffectKind.Burn, amount: 3, "DEV 화상 3"),
            new(StatusEffectKind.Infection, amount: 3, "DEV 감염 3"),
            new(StatusEffectKind.Vulnerable, amount: 2, "DEV 취약 2"),
            new(StatusEffectKind.Weaken, amount: 2, "DEV 약화 2"),
        };

        private static bool _isCreatingButtonPanel;

        [FormerlySerializedAs("_compositionRoot")]
        [FormerlySerializedAs("_battleFlowController")]
        [SerializeField] private BattleSceneHost _battleSceneCompositionRoot;
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;

        [Header("DEV Status Effect")]
        [SerializeField] private bool _createEnemyStatusButtons = true;
        [SerializeField] private StatusEffectKind _statusEffectKind = StatusEffectKind.Burn;
        [SerializeField] private int _amount = 3;

        private void Awake()
        {
            if (_createEnemyStatusButtons && !_isCreatingButtonPanel)
            {
                CreateEnemyStatusButtonPanel();
            }

            EnsureLabel();
            UpdateLabel();
            if (_button != null)
            {
                _button.onClick.AddListener(ApplyRelicStatusTurn);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(ApplyRelicStatusTurn);
            }
        }

        public void ApplyRelicStatusTurn()
        {
            BattleSceneHost battleSceneCompositionRoot = ResolveBattleSceneHost();
            if (battleSceneCompositionRoot == null)
            {
                Debug.LogError("[RunBattleStatusEffectDebugButton] BattleSceneHost is missing.");
                return;
            }

            battleSceneCompositionRoot.DevApplyRelicStatusTurn(
                _statusEffectKind,
                Mathf.Max(1, _amount),
                CombatTargetMode.SelectedEnemy);
        }

        private void Reset()
        {
            _battleSceneCompositionRoot = GetComponentInParent<BattleSceneHost>();
        }

        private void OnValidate()
        {
            _amount = Mathf.Max(1, _amount);
            UpdateLabel();
        }

        private BattleSceneHost ResolveBattleSceneHost()
        {
            if (_battleSceneCompositionRoot != null)
            {
                return _battleSceneCompositionRoot;
            }

            _battleSceneCompositionRoot = GetComponentInParent<BattleSceneHost>();
            return _battleSceneCompositionRoot;
        }

        private void CreateEnemyStatusButtonPanel()
        {
            _isCreatingButtonPanel = true;
            try
            {
                Configure(EnemyStatusButtons[0], buttonIndex: 0);
                for (int index = 1; index < EnemyStatusButtons.Length; index++)
                {
                    GameObject clone = Instantiate(gameObject, transform.parent);
                    clone.name = $"DevButtonAttribute_{EnemyStatusButtons[index].Kind}";
                    RunBattleStatusEffectDebugButton debugButton =
                        clone.GetComponent<RunBattleStatusEffectDebugButton>();
                    debugButton.Configure(EnemyStatusButtons[index], index);
                }
            }
            finally
            {
                _isCreatingButtonPanel = false;
            }
        }

        private void Configure(DebugStatusButtonDefinition definition, int buttonIndex)
        {
            _createEnemyStatusButtons = false;
            _statusEffectKind = definition.Kind;
            _amount = definition.Amount;
            EnsureLabel();
            _label.text = definition.Label;

            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            int column = buttonIndex % 2;
            int row = buttonIndex / 2;
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(16f + (column * 156f), -16f - (row * 52f));
            rectTransform.sizeDelta = new Vector2(148f, 44f);
        }

        private void EnsureLabel()
        {
            if (_label != null)
            {
                return;
            }

            GameObject labelObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(transform, worldPositionStays: false);
            RectTransform labelRectTransform = (RectTransform)labelObject.transform;
            labelRectTransform.anchorMin = Vector2.zero;
            labelRectTransform.anchorMax = Vector2.one;
            labelRectTransform.offsetMin = Vector2.zero;
            labelRectTransform.offsetMax = Vector2.zero;

            _label = labelObject.GetComponent<Text>();
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = Color.black;
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 18;
            _label.raycastTarget = false;
        }

        private void UpdateLabel()
        {
            if (_label != null)
            {
                _label.text = GetLabel(_statusEffectKind, _amount);
            }
        }

        private static string GetLabel(StatusEffectKind kind, int amount)
        {
            switch (kind)
            {
                case StatusEffectKind.Burn:
                    return $"DEV 화상 {amount}";
                case StatusEffectKind.Infection:
                    return $"DEV 감염 {amount}";
                case StatusEffectKind.Vulnerable:
                    return $"DEV 취약 {amount}";
                case StatusEffectKind.Weaken:
                    return $"DEV 약화 {amount}";
                default:
                    return $"DEV {kind} {amount}";
            }
        }

        private readonly struct DebugStatusButtonDefinition
        {
            public DebugStatusButtonDefinition(
                StatusEffectKind kind,
                int amount,
                string label)
            {
                Kind = kind;
                Amount = amount;
                Label = label;
            }

            public StatusEffectKind Kind { get; }

            public int Amount { get; }

            public string Label { get; }
        }
    }
}
