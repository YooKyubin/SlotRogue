using SlotRogue.Slot.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleView : MonoBehaviour
    {
        private static readonly Color PatternHitColor = new Color(1f, 0.82f, 0.23f, 1f);
        private static readonly Color BaseAttackColor = new Color(0.66f, 0.82f, 1f, 1f);

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
        [SerializeField] private Button _spinButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _restartButton;

        public Text[] SlotCells => _slotCells;

        public Button SpinButton => _spinButton;

        public Button ContinueButton => _continueButton;

        public Button RestartButton => _restartButton;

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
            Button restartButton)
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
