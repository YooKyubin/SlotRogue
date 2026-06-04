using SlotRogue.Slot.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    [DefaultExecutionOrder(-50)]
    public sealed class RunBattleView : MonoBehaviour
    {
        public const int FormationHudSlotCount = 3;
        private const float FormationHudSpacing = 300f;

        private static readonly Color PatternHitColor = new Color(1f, 0.82f, 0.23f, 1f);
        private static readonly Color BaseAttackColor = new Color(0.66f, 0.82f, 1f, 1f);

        [SerializeField] private Text[] _slotCells;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _slotResultText;
        [SerializeField] private Text _resultValueText;
        [SerializeField] private Text _playerHudText;
        [SerializeField] private Text _enemyIntentText;
        [SerializeField] private Image _playerHpFill;
        [SerializeField] private Image _playerShieldFill;
        [SerializeField] private EnemyFormationSlotView[] _formationSlots = System.Array.Empty<EnemyFormationSlotView>();
        [SerializeField] private Button _spinButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private RectTransform _floatingTextRoot;
        [SerializeField] private RectTransform _playerDamageAnchor;

        public Text[] SlotCells => _slotCells;

        public Button SpinButton => _spinButton;

        public Button ContinueButton => _continueButton;

        public Button RestartButton => _restartButton;

        public Text StatusText => _statusText;

        public Transform FloatingTextRoot => _floatingTextRoot;

        public RectTransform PlayerDamageAnchor => _playerDamageAnchor;

        public RectTransform MonsterDamageAnchor => GetEnemyDamageAnchor(1);

        public int EnemySlotCount => _formationSlots != null ? _formationSlots.Length : 0;

        private void Awake()
        {
            ValidateFormationSlots();
        }

        public void EnsureEnemySlotCapacity(int enemyCount)
        {
            if (!ValidateFormationSlots())
            {
                return;
            }

            LayoutFormationHudSlots();
        }

        public void Bind(
            Text[] slotCells,
            Text statusText,
            Text slotResultText,
            Text resultValueText,
            Text playerHudText,
            Text enemyIntentText,
            Image playerHpFill,
            Image playerShieldFill,
            Button spinButton,
            Button continueButton,
            Button restartButton,
            RectTransform floatingTextRoot,
            RectTransform playerDamageAnchor,
            EnemyFormationSlotView[] formationSlots)
        {
            _slotCells = slotCells;
            _statusText = statusText;
            _slotResultText = slotResultText;
            _resultValueText = resultValueText;
            _playerHudText = playerHudText;
            _enemyIntentText = enemyIntentText;
            _playerHpFill = playerHpFill;
            _playerShieldFill = playerShieldFill;
            _spinButton = spinButton;
            _continueButton = continueButton;
            _restartButton = restartButton;
            _floatingTextRoot = floatingTextRoot;
            _playerDamageAnchor = playerDamageAnchor;
            _formationSlots = formationSlots ?? System.Array.Empty<EnemyFormationSlotView>();
            ValidateFormationSlots();
        }

        public void SetAttackResult(string value)
        {
            SetText(_resultValueText, value);
        }

        public void SetSlotResult(string value)
        {
            SetText(_slotResultText, value);
        }

        public void SetSlotOutcomePresentation(
            bool hasPattern,
            int row,
            int startColumn,
            int matchLength)
        {
            CacheOutcomeColorsIfNeeded();
            ResetSlotCellColors();

            Color emphasisColor = hasPattern ? PatternHitColor : BaseAttackColor;
            SetTextColor(_slotResultText, emphasisColor);
            SetTextColor(_resultValueText, emphasisColor);

            if (!hasPattern ||
                row < 0 ||
                row >= SlotSpinResult.Rows ||
                matchLength <= 0 ||
                _slotCells == null)
            {
                return;
            }

            int firstColumn = Mathf.Clamp(startColumn, 0, SlotSpinResult.Columns - 1);
            int endColumn = Mathf.Min(SlotSpinResult.Columns, firstColumn + matchLength);

            for (int column = firstColumn; column < endColumn; column++)
            {
                int index = SlotSpinResult.ToIndex(column, row);

                if (index >= 0 && index < _slotCells.Length && _slotCells[index] != null)
                {
                    _slotCells[index].color = PatternHitColor;
                }
            }
        }

        public void ResetSlotOutcomePresentation()
        {
            CacheOutcomeColorsIfNeeded();
            SetTextColor(_slotResultText, _slotResultDefaultColor);
            SetTextColor(_resultValueText, _resultValueDefaultColor);
            ResetSlotCellColors();
        }

        public void SetStatus(string value)
        {
            SetText(_statusText, value);
        }

        public void SetPlayerHud(string value)
        {
            SetText(_playerHudText, value);
        }

        public void SetEnemyIntent(string value)
        {
            SetText(_enemyIntentText, value);
        }

        public void SetPlayerHpFill(int current, int max)
        {
            SetBarFill(_playerHpFill, current, max);
        }

        public void SetPlayerShieldFill(int current, int max)
        {
            SetBarFill(_playerShieldFill, current, max);
        }

        public void SetEnemyPortrait(int slotIndex, Sprite portrait)
        {
            if (TryGetFormationSlot(slotIndex, out EnemyFormationSlotView slot))
            {
                slot.SetPortrait(portrait);
            }
        }

        public void SetEnemySlot(
            int slotIndex,
            string value,
            int currentHp,
            int maxHp,
            bool selected,
            bool interactable)
        {
            if (!TryGetFormationSlot(slotIndex, out EnemyFormationSlotView slot))
            {
                return;
            }

            slot.SetActive(true);
            slot.SetHud(selected ? $"> {value}" : value);
            slot.SetHpFill(currentHp, maxHp);
            slot.SetSelected(selected);
            slot.SetInteractable(interactable);
        }

        public void SetEnemySlotActive(int slotIndex, bool active)
        {
            if (TryGetFormationSlot(slotIndex, out EnemyFormationSlotView slot))
            {
                slot.SetActive(active);
            }
        }

        public void SetEnemySlotClickHandler(int slotIndex, UnityAction action)
        {
            if (TryGetFormationSlot(slotIndex, out EnemyFormationSlotView slot))
            {
                slot.SetClickHandler(action);
            }
        }

        public RectTransform GetEnemyDamageAnchor(int slotIndex)
        {
            if (TryGetFormationSlot(slotIndex, out EnemyFormationSlotView slot))
            {
                return slot.DamageAnchor;
            }

            return null;
        }

        public void ShowSpinButton()
        {
            SetActive(_spinButton, true);
            SetActive(_continueButton, false);
            SetActive(_restartButton, false);
        }

        public void ShowContinueButton()
        {
            SetActive(_spinButton, false);
            SetActive(_continueButton, true);
            SetActive(_restartButton, false);
        }

        public void ShowRestartButton()
        {
            SetActive(_spinButton, false);
            SetActive(_continueButton, false);
            SetActive(_restartButton, true);
        }

        private bool ValidateFormationSlots()
        {
            if (HasValidFormationSlots())
            {
                LayoutFormationHudSlots();
                return true;
            }

            Debug.LogError(
                "[RunBattleView] Enemy formation requires 3 configured slots. " +
                "Run menu: SlotRogue > Game Flow > Rebuild Scene UI Prefabs.");
            return false;
        }

        private bool HasValidFormationSlots()
        {
            if (_formationSlots == null || _formationSlots.Length < FormationHudSlotCount)
            {
                return false;
            }

            for (int index = 0; index < FormationHudSlotCount; index++)
            {
                if (_formationSlots[index] == null || _formationSlots[index].Root == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void LayoutFormationHudSlots()
        {
            if (_formationSlots == null)
            {
                return;
            }

            float startX = -(FormationHudSlotCount - 1) * FormationHudSpacing * 0.5f;

            for (int index = 0; index < FormationHudSlotCount && index < _formationSlots.Length; index++)
            {
                EnemyFormationSlotView slot = _formationSlots[index];
                if (slot == null || slot.Root == null)
                {
                    continue;
                }

                RectTransform root = slot.Root;
                Vector2 position = new Vector2(startX + (index * FormationHudSpacing), root.anchoredPosition.y);
                root.anchoredPosition = position;
            }
        }

        private bool TryGetFormationSlot(int slotIndex, out EnemyFormationSlotView slot)
        {
            slot = null!;
            if (_formationSlots == null || slotIndex < 0 || slotIndex >= _formationSlots.Length)
            {
                return false;
            }

            slot = _formationSlots[slotIndex];
            return slot != null;
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetTextColor(Text text, Color color)
        {
            if (text != null)
            {
                text.color = color;
            }
        }

        private static void SetActive(Button button, bool active)
        {
            if (button != null)
            {
                button.gameObject.SetActive(active);
            }
        }

        private static void SetBarFill(Image fill, int current, int max)
        {
            if (fill == null)
            {
                return;
            }

            RectTransform parent = fill.rectTransform.parent as RectTransform;
            float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);

            if (parent != null && parent.sizeDelta.y > parent.sizeDelta.x * 1.4f)
            {
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Vertical;
                fill.fillOrigin = (int)Image.OriginVertical.Bottom;
                fill.fillAmount = ratio;
                fill.preserveAspect = false;
                return;
            }

            fill.type = Image.Type.Simple;
            float maxWidth = parent != null ? Mathf.Max(1f, parent.sizeDelta.x - 8f) : 1f;
            fill.rectTransform.sizeDelta = new Vector2(maxWidth * ratio, fill.rectTransform.sizeDelta.y);
        }

        private void CacheOutcomeColorsIfNeeded()
        {
            if (_hasCachedOutcomeColors)
            {
                return;
            }

            _slotResultDefaultColor = _slotResultText != null ? _slotResultText.color : Color.white;
            _resultValueDefaultColor = _resultValueText != null ? _resultValueText.color : Color.white;

            if (_slotCells == null)
            {
                _slotCellDefaultColors = new Color[0];
            }
            else
            {
                _slotCellDefaultColors = new Color[_slotCells.Length];

                for (int index = 0; index < _slotCells.Length; index++)
                {
                    _slotCellDefaultColors[index] = _slotCells[index] != null
                        ? _slotCells[index].color
                        : Color.white;
                }
            }

            _hasCachedOutcomeColors = true;
        }

        private void ResetSlotCellColors()
        {
            if (_slotCells == null)
            {
                return;
            }

            for (int index = 0; index < _slotCells.Length; index++)
            {
                if (_slotCells[index] == null)
                {
                    continue;
                }

                Color defaultColor = _slotCellDefaultColors != null && index < _slotCellDefaultColors.Length
                    ? _slotCellDefaultColors[index]
                    : Color.white;
                _slotCells[index].color = defaultColor;
            }
        }

        private Color[] _slotCellDefaultColors;
        private Color _slotResultDefaultColor = Color.white;
        private Color _resultValueDefaultColor = Color.white;
        private bool _hasCachedOutcomeColors;
    }
}
