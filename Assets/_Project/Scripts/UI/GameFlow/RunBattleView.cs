using SlotRogue.Slot.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleView : MonoBehaviour
    {
        private static readonly Color PatternHitColor = new Color(1f, 0.82f, 0.23f, 1f);
        private static readonly Color BaseAttackColor = new Color(0.66f, 0.82f, 1f, 1f);
        private static readonly Color EnemySlotColor = new Color(0.11f, 0.14f, 0.2f, 0.96f);
        private static readonly Color SelectedEnemySlotColor = new Color(0.45f, 0.26f, 0.12f, 0.96f);

        [SerializeField] private Text[] _slotCells;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _slotResultText;
        [SerializeField] private Text _resultValueText;
        [SerializeField] private Text _playerHudText;
        [SerializeField] private Text _monsterHudText;
        [SerializeField] private Text _enemyIntentText;
        [SerializeField] private Image _playerHpFill;
        [SerializeField] private Image _playerShieldFill;
        [SerializeField] private Image _monsterHpFill;
        [SerializeField] private EnemyHudSlot[] _enemySlots;
        [SerializeField] private Button _spinButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private RectTransform _floatingTextRoot;
        [SerializeField] private RectTransform _playerDamageAnchor;
        [SerializeField] private RectTransform _monsterDamageAnchor;

        public Text[] SlotCells => _slotCells;

        public Button SpinButton => _spinButton;

        public Button ContinueButton => _continueButton;

        public Button RestartButton => _restartButton;

        public Text StatusText => _statusText;

        public Transform FloatingTextRoot => _floatingTextRoot;

        public RectTransform PlayerDamageAnchor => _playerDamageAnchor;

        public RectTransform MonsterDamageAnchor => _monsterDamageAnchor;

        public int EnemySlotCount => ResolveEnemySlots().Length;

        public void EnsureEnemySlotCapacity(int count)
        {
            EnemyHudSlot[] slots = ResolveEnemySlots();
            if (count <= slots.Length)
            {
                LayoutEnemySlots(slots, count);
                return;
            }

            if (slots.Length == 0)
            {
                return;
            }

            var expanded = new EnemyHudSlot[count];
            int existingCount = slots.Length;
            for (int index = 0; index < slots.Length; index++)
            {
                expanded[index] = slots[index];
            }

            RectTransform templateRoot = slots[0].Root ??
                (_monsterHudText != null ? _monsterHudText.rectTransform.parent as RectTransform : null);
            if (templateRoot == null)
            {
                return;
            }

            if (slots[0].Root == null)
            {
                Button templateButton = templateRoot.GetComponent<Button>();
                if (templateButton == null)
                {
                    templateButton = templateRoot.gameObject.AddComponent<Button>();
                }

                templateButton.targetGraphic = templateRoot.GetComponent<Image>();
                expanded[0] = new EnemyHudSlot(
                    templateRoot,
                    slots[0].HudText,
                    slots[0].HpFill,
                    templateButton,
                    slots[0].DamageAnchor);
            }

            Transform parent = templateRoot.parent;
            float spacing = count <= 2 ? 300f : 270f;
            float startX = -(count - 1) * spacing * 0.5f;

            for (int index = 0; index < count; index++)
            {
                if (index < existingCount && expanded[index] != null && expanded[index].Root != null)
                {
                    expanded[index].Root.anchoredPosition = new Vector2(
                        startX + (index * spacing),
                        expanded[index].Root.anchoredPosition.y);
                }
            }

            for (int index = existingCount; index < count; index++)
            {
                RectTransform clone = Object.Instantiate(templateRoot, parent);
                clone.name = $"Runtime Monster {index + 1} Status HUD";
                clone.anchoredPosition = new Vector2(
                    startX + (index * spacing),
                    templateRoot.anchoredPosition.y);
                Text hud = clone.GetComponentInChildren<Text>(true);
                Image hpFill = ResolveHpFill(clone);
                Button button = clone.GetComponent<Button>();
                if (button == null)
                {
                    button = clone.gameObject.AddComponent<Button>();
                }

                button.targetGraphic = clone.GetComponent<Image>();
                expanded[index] = new EnemyHudSlot(clone, hud, hpFill, button, null);
            }

            _enemySlots = expanded;
            LayoutEnemySlots(_enemySlots, count);
        }

        public void Bind(
            Text[] slotCells,
            Text statusText,
            Text slotResultText,
            Text resultValueText,
            Text playerHudText,
            Text monsterHudText,
            Text enemyIntentText,
            Image playerHpFill,
            Image playerShieldFill,
            Image monsterHpFill,
            Button spinButton,
            Button continueButton,
            Button restartButton,
            RectTransform floatingTextRoot,
            RectTransform playerDamageAnchor,
            RectTransform monsterDamageAnchor)
        {
            _slotCells = slotCells;
            _statusText = statusText;
            _slotResultText = slotResultText;
            _resultValueText = resultValueText;
            _playerHudText = playerHudText;
            _monsterHudText = monsterHudText;
            _enemyIntentText = enemyIntentText;
            _playerHpFill = playerHpFill;
            _playerShieldFill = playerShieldFill;
            _monsterHpFill = monsterHpFill;
            _spinButton = spinButton;
            _continueButton = continueButton;
            _restartButton = restartButton;
            _floatingTextRoot = floatingTextRoot;
            _playerDamageAnchor = playerDamageAnchor;
            _monsterDamageAnchor = monsterDamageAnchor;
            _enemySlots = new[]
            {
                new EnemyHudSlot(null, monsterHudText, monsterHpFill, null, monsterDamageAnchor),
            };
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
            EnemyHudSlot[] enemySlots)
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
            _enemySlots = enemySlots ?? new EnemyHudSlot[0];

            if (_enemySlots.Length > 0)
            {
                _monsterHudText = _enemySlots[0].HudText;
                _monsterHpFill = _enemySlots[0].HpFill;
                _monsterDamageAnchor = _enemySlots[0].DamageAnchor;
            }
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

        public void SetMonsterHud(string value)
        {
            SetText(_monsterHudText, value);
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

        public void SetMonsterHpFill(int current, int max)
        {
            SetBarFill(_monsterHpFill, current, max);
        }

        public void SetEnemySlot(
            int slotIndex,
            string value,
            int currentHp,
            int maxHp,
            bool selected,
            bool interactable)
        {
            EnemyHudSlot[] slots = ResolveEnemySlots();
            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                return;
            }

            EnemyHudSlot slot = slots[slotIndex];
            slot.SetActive(true);
            slot.SetHud(selected ? $"> {value}" : value);
            slot.SetHpFill(currentHp, maxHp);
            slot.SetSelected(selected);
            slot.SetInteractable(interactable);
        }

        public void SetEnemySlotActive(int slotIndex, bool active)
        {
            EnemyHudSlot[] slots = ResolveEnemySlots();
            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                return;
            }

            slots[slotIndex].SetActive(active);
        }

        public void SetEnemySlotClickHandler(int slotIndex, UnityAction action)
        {
            EnemyHudSlot[] slots = ResolveEnemySlots();
            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                return;
            }

            slots[slotIndex].SetClickHandler(action);
        }

        public RectTransform GetEnemyDamageAnchor(int slotIndex)
        {
            EnemyHudSlot[] slots = ResolveEnemySlots();
            if (slotIndex >= 0 &&
                slotIndex < slots.Length &&
                slots[slotIndex].DamageAnchor != null)
            {
                return slots[slotIndex].DamageAnchor;
            }

            return _monsterDamageAnchor;
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
            float maxWidth = parent != null ? Mathf.Max(1f, parent.sizeDelta.x - 8f) : 1f;
            float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            fill.rectTransform.sizeDelta = new Vector2(maxWidth * ratio, fill.rectTransform.sizeDelta.y);
        }

        private EnemyHudSlot[] ResolveEnemySlots()
        {
            if (_enemySlots != null && _enemySlots.Length > 0)
            {
                return _enemySlots;
            }

            if (_monsterHudText == null && _monsterHpFill == null && _monsterDamageAnchor == null)
            {
                return new EnemyHudSlot[0];
            }

            _enemySlots = new[]
            {
                new EnemyHudSlot(null, _monsterHudText, _monsterHpFill, null, _monsterDamageAnchor),
            };
            return _enemySlots;
        }

        private static Image ResolveHpFill(RectTransform root)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int index = 0; index < images.Length; index++)
            {
                if (images[index].name.Contains("Fill"))
                {
                    return images[index];
                }
            }

            return images.Length > 0 ? images[images.Length - 1] : null;
        }

        private static void LayoutEnemySlots(EnemyHudSlot[] slots, int visibleCount)
        {
            if (slots == null || visibleCount <= 0)
            {
                return;
            }

            float spacing = visibleCount <= 2 ? 300f : 270f;
            float startX = -(visibleCount - 1) * spacing * 0.5f;
            int layoutCount = Mathf.Min(visibleCount, slots.Length);

            for (int index = 0; index < layoutCount; index++)
            {
                if (slots[index] == null || slots[index].Root == null)
                {
                    continue;
                }

                slots[index].Root.anchoredPosition = new Vector2(
                    startX + (index * spacing),
                    slots[index].Root.anchoredPosition.y);

                if (slots[index].Button != null &&
                    slots[index].Button.transform is RectTransform buttonTransform &&
                    buttonTransform != slots[index].Root)
                {
                    buttonTransform.anchoredPosition = new Vector2(
                        startX + (index * spacing),
                        buttonTransform.anchoredPosition.y);
                }
            }
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

        [System.Serializable]
        public sealed class EnemyHudSlot
        {
            [SerializeField] private RectTransform _root;
            [SerializeField] private Text _hudText;
            [SerializeField] private Image _hpFill;
            [SerializeField] private Button _button;
            [SerializeField] private RectTransform _damageAnchor;

            public EnemyHudSlot(
                RectTransform root,
                Text hudText,
                Image hpFill,
                Button button,
                RectTransform damageAnchor)
            {
                _root = root;
                _hudText = hudText;
                _hpFill = hpFill;
                _button = button;
                _damageAnchor = damageAnchor;
            }

            public Text HudText => _hudText;

            public Image HpFill => _hpFill;

            public Button Button => _button;

            public RectTransform Root => _root;

            public RectTransform DamageAnchor => _damageAnchor;

            public void SetActive(bool active)
            {
                if (_root != null)
                {
                    _root.gameObject.SetActive(active);
                }

                SetGameObjectActive(_hudText, active);
                SetGameObjectActive(_hpFill, active);
                SetGameObjectActive(_button, active);
            }

            public void SetHud(string value)
            {
                SetText(_hudText, value);
            }

            public void SetHpFill(int current, int max)
            {
                SetBarFill(_hpFill, current, max);
            }

            public void SetSelected(bool selected)
            {
                if (_root == null)
                {
                    return;
                }

                Image image = _root.GetComponent<Image>();
                if (image != null)
                {
                    image.color = selected ? SelectedEnemySlotColor : EnemySlotColor;
                }
            }

            public void SetInteractable(bool interactable)
            {
                if (_button != null)
                {
                    _button.interactable = interactable;
                }
            }

            public void SetClickHandler(UnityAction action)
            {
                if (_button == null)
                {
                    return;
                }

                _button.onClick.RemoveAllListeners();
                if (action != null)
                {
                    _button.onClick.AddListener(action);
                }
            }

            private static void SetGameObjectActive(Component component, bool active)
            {
                if (component != null)
                {
                    component.gameObject.SetActive(active);
                }
            }
        }
    }
}
