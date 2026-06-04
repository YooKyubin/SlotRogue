using System;
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

        private static readonly Color PatternHitColor = new Color(1f, 0.82f, 0.23f, 1f);
        private static readonly Color BaseAttackColor = new Color(0.66f, 0.82f, 1f, 1f);
        private static readonly Color EnemySlotColor = new Color(0.11f, 0.14f, 0.2f, 0.96f);
        private static readonly Color SelectedEnemySlotColor = new Color(0.45f, 0.26f, 0.12f, 0.96f);

        [SerializeField] private Text[] _slotCells;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _slotResultText;
        [SerializeField] private Text _resultValueText;
        [SerializeField] private Text _playerHudText;
        [SerializeField] private Text _enemyIntentText;
        [SerializeField] private Image _playerHpFill;
        [SerializeField] private Image _playerShieldFill;
        [SerializeField] private EnemyHudSlot[] _enemySlots = Array.Empty<EnemyHudSlot>();
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

        public int EnemySlotCount => _enemySlots != null ? _enemySlots.Length : 0;

        private void Awake()
        {
            TryEnsureFormationHudSlots();
        }

        public void EnsureEnemySlotCapacity(int enemyCount)
        {
            TryEnsureFormationHudSlots();

            if (_enemySlots == null || _enemySlots.Length < FormationHudSlotCount)
            {
                Debug.LogError(
                    "[RunBattleView] Enemy HUD requires 3 formation slots. " +
                    "Run menu: SlotRogue > Game Flow > Rebuild Scene UI Prefabs.");
                return;
            }

            LayoutFormationHudSlots(_enemySlots);
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
            _enemySlots = enemySlots ?? Array.Empty<EnemyHudSlot>();
            TryEnsureFormationHudSlots();
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

        public void SetEnemySlot(
            int slotIndex,
            string value,
            int currentHp,
            int maxHp,
            bool selected,
            bool interactable)
        {
            if (!TryGetEnemySlot(slotIndex, out EnemyHudSlot slot))
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
            if (TryGetEnemySlot(slotIndex, out EnemyHudSlot slot))
            {
                slot.SetActive(active);
            }
        }

        public void SetEnemySlotClickHandler(int slotIndex, UnityAction action)
        {
            if (TryGetEnemySlot(slotIndex, out EnemyHudSlot slot))
            {
                slot.SetClickHandler(action);
            }
        }

        public RectTransform GetEnemyDamageAnchor(int slotIndex)
        {
            if (TryGetEnemySlot(slotIndex, out EnemyHudSlot slot) && slot.DamageAnchor != null)
            {
                return slot.DamageAnchor;
            }

            if (TryGetEnemySlot(slotIndex, out EnemyHudSlot fallbackSlot) && fallbackSlot.Root != null)
            {
                return fallbackSlot.Root;
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

        private void TryEnsureFormationHudSlots()
        {
            if (HasValidFormationSlots())
            {
                LayoutFormationHudSlots(_enemySlots);
                return;
            }

            if (TryCollectFormationSlotsFromHierarchy(out EnemyHudSlot[] hierarchySlots))
            {
                _enemySlots = hierarchySlots;
                LayoutFormationHudSlots(_enemySlots);
                return;
            }

            EnemyHudSlot seed = FindSeedEnemyHudSlot();
            if (seed.Root == null && seed.HudText == null)
            {
                Debug.LogError(
                    "[RunBattleView] No enemy HUD slots found. " +
                    "Run menu: SlotRogue > Game Flow > Rebuild Scene UI Prefabs.");
                _enemySlots = Array.Empty<EnemyHudSlot>();
                return;
            }

            _enemySlots = BuildFormationSlotsFromSeed(seed);
            LayoutFormationHudSlots(_enemySlots);
        }

        private bool HasValidFormationSlots()
        {
            if (_enemySlots == null || _enemySlots.Length < FormationHudSlotCount)
            {
                return false;
            }

            for (int index = 0; index < FormationHudSlotCount; index++)
            {
                if (_enemySlots[index] == null || _enemySlots[index].Root == null)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryCollectFormationSlotsFromHierarchy(out EnemyHudSlot[] slots)
        {
            var discovered = new EnemyHudSlot[FormationHudSlotCount];
            int foundCount = 0;
            Transform[] candidates = GetComponentsInChildren<Transform>(true);

            for (int index = 0; index < candidates.Length; index++)
            {
                Transform candidate = candidates[index];
                if (!candidate.name.Contains("Monster", StringComparison.Ordinal) ||
                    !candidate.name.Contains("Status HUD", StringComparison.Ordinal))
                {
                    continue;
                }

                int slotIndex = ParseMonsterStatusHudIndex(candidate.name);
                if (slotIndex < 0 || slotIndex >= FormationHudSlotCount)
                {
                    continue;
                }

                if (candidate is not RectTransform root)
                {
                    continue;
                }

                if (discovered[slotIndex] != null)
                {
                    continue;
                }

                Text hud = root.GetComponentInChildren<Text>(true);
                Image hpFill = ResolveHpFill(root);
                Button button = root.GetComponent<Button>();
                if (button == null)
                {
                    button = root.gameObject.AddComponent<Button>();
                    button.targetGraphic = root.GetComponent<Image>();
                }

                discovered[slotIndex] = new EnemyHudSlot(root, hud, hpFill, button, null);
                foundCount++;
            }

            if (foundCount < FormationHudSlotCount)
            {
                slots = Array.Empty<EnemyHudSlot>();
                return false;
            }

            slots = discovered;
            return true;
        }

        private static int ParseMonsterStatusHudIndex(string objectName)
        {
            if (objectName.Contains("Monster 1", StringComparison.Ordinal))
            {
                return 0;
            }

            if (objectName.Contains("Monster 2", StringComparison.Ordinal))
            {
                return 1;
            }

            if (objectName.Contains("Monster 3", StringComparison.Ordinal))
            {
                return 2;
            }

            if (objectName.Equals("Monster Status HUD", StringComparison.Ordinal))
            {
                return 0;
            }

            return -1;
        }

        private EnemyHudSlot FindSeedEnemyHudSlot()
        {
            if (_enemySlots != null)
            {
                for (int index = 0; index < _enemySlots.Length; index++)
                {
                    if (_enemySlots[index] != null &&
                        (_enemySlots[index].Root != null || _enemySlots[index].HudText != null))
                    {
                        return NormalizeSeedSlot(_enemySlots[index]);
                    }
                }
            }

            Transform[] candidates = GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < candidates.Length; index++)
            {
                Transform candidate = candidates[index];
                if (!candidate.name.Contains("Monster", StringComparison.Ordinal) ||
                    !candidate.name.Contains("Status HUD", StringComparison.Ordinal))
                {
                    continue;
                }

                if (candidate is not RectTransform root)
                {
                    continue;
                }

                Text hud = root.GetComponentInChildren<Text>(true);
                Image hpFill = ResolveHpFill(root);
                Button button = root.GetComponent<Button>() ?? root.gameObject.AddComponent<Button>();
                button.targetGraphic = root.GetComponent<Image>();
                return new EnemyHudSlot(root, hud, hpFill, button, null);
            }

            return new EnemyHudSlot(null, null, null, null, null);
        }

        private static EnemyHudSlot NormalizeSeedSlot(EnemyHudSlot seed)
        {
            if (seed.Root != null)
            {
                return seed;
            }

            if (seed.HudText == null)
            {
                return seed;
            }

            RectTransform root = seed.HudText.transform.parent as RectTransform;
            Button button = root != null ? root.GetComponent<Button>() : null;
            if (root != null && button == null)
            {
                button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = root.GetComponent<Image>();
            }

            return new EnemyHudSlot(root, seed.HudText, seed.HpFill, button, seed.DamageAnchor);
        }

        private static EnemyHudSlot[] BuildFormationSlotsFromSeed(EnemyHudSlot seed)
        {
            var slots = new EnemyHudSlot[FormationHudSlotCount];
            RectTransform templateRoot = seed.Root ?? seed.HudText.rectTransform.parent as RectTransform;
            Transform parent = templateRoot.parent;

            for (int index = 0; index < FormationHudSlotCount; index++)
            {
                if (index == 0)
                {
                    templateRoot.name = "Monster 1 Status HUD";
                    slots[index] = new EnemyHudSlot(
                        templateRoot,
                        seed.HudText,
                        seed.HpFill,
                        seed.Button,
                        seed.DamageAnchor);
                    continue;
                }

                RectTransform clone = Instantiate(templateRoot, parent);
                clone.name = $"Monster {index + 1} Status HUD";
                Text hud = clone.GetComponentInChildren<Text>(true);
                Image hpFill = ResolveHpFill(clone);
                Button button = clone.GetComponent<Button>();
                if (button == null)
                {
                    button = clone.gameObject.AddComponent<Button>();
                }

                button.targetGraphic = clone.GetComponent<Image>();
                slots[index] = new EnemyHudSlot(clone, hud, hpFill, button, null);
            }

            return slots;
        }

        private static void LayoutFormationHudSlots(EnemyHudSlot[] slots)
        {
            if (slots == null)
            {
                return;
            }

            float spacing = 300f;
            float startX = -(FormationHudSlotCount - 1) * spacing * 0.5f;

            for (int index = 0; index < FormationHudSlotCount && index < slots.Length; index++)
            {
                if (slots[index] == null || slots[index].Root == null)
                {
                    continue;
                }

                Vector2 position = new Vector2(startX + (index * spacing), slots[index].Root.anchoredPosition.y);
                slots[index].Root.anchoredPosition = position;

                if (slots[index].Button != null &&
                    slots[index].Button.transform is RectTransform buttonTransform &&
                    buttonTransform != slots[index].Root)
                {
                    buttonTransform.anchoredPosition = position;
                }
            }
        }

        private bool TryGetEnemySlot(int slotIndex, out EnemyHudSlot slot)
        {
            slot = null!;
            if (_enemySlots == null || slotIndex < 0 || slotIndex >= _enemySlots.Length)
            {
                return false;
            }

            slot = _enemySlots[slotIndex];
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
            float maxWidth = parent != null ? Mathf.Max(1f, parent.sizeDelta.x - 8f) : 1f;
            float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            fill.rectTransform.sizeDelta = new Vector2(maxWidth * ratio, fill.rectTransform.sizeDelta.y);
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

        [Serializable]
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
