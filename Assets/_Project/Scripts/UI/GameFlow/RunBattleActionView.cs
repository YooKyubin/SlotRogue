using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleActionView : MonoBehaviour
    {
        private static readonly Color PatternHitColor = new(1f, 0.82f, 0.23f, 1f);
        private static readonly Color BaseAttackColor = new(0.66f, 0.82f, 1f, 1f);

        [SerializeField] private Text _slotResultText;
        [SerializeField] private Text _attackResultText;
        [SerializeField] private Button _spinButton;

        private Color _slotResultDefaultColor = Color.white;
        private Color _attackResultDefaultColor = Color.white;
        private bool _hasCachedDefaults;

        public event Action SpinRequested;
        public bool HasRequiredControls => _spinButton != null;

        private void Awake()
        {
            EnsureReferences();
            SubscribeButtons();
        }

        private void OnDestroy()
        {
            UnsubscribeButtons();
        }

        public void Bind(
            Text slotResultText,
            Text attackResultText,
            Button spinButton)
        {
            _slotResultText = slotResultText;
            _attackResultText = attackResultText;
            _spinButton = spinButton;
            SubscribeButtons();
        }

        public void Render(RunBattleScreenState state)
        {
            EnsureReferences();
            CacheDefaultsIfNeeded();
            SetText(_slotResultText, state.SlotResultText);
            SetText(_attackResultText, state.AttackResultText);
            RenderEmphasis(state.SlotOutcome.HasPattern);
            RenderButtons(state.ActionMode, state.SpinInteractable);
        }

        private void SubscribeButtons()
        {
            UnsubscribeButtons();

            if (_spinButton != null)
            {
                _spinButton.onClick.AddListener(HandleSpinClicked);
            }
        }

        private void EnsureReferences()
        {
            _slotResultText ??= ResolveText("Next Attack Text");
            _attackResultText ??=
                ResolveText("Attack Power Text") ??
                ResolveText("Attack Result Value");
        }

        private Text ResolveText(string objectName)
        {
            Transform searchRoot = transform.root != null ? transform.root : transform;
            Transform found = SceneComponentResolver.FindDeepChild(searchRoot, objectName);
            return found != null ? found.GetComponent<Text>() : null;
        }

        private void UnsubscribeButtons()
        {
            if (_spinButton != null)
            {
                _spinButton.onClick.RemoveListener(HandleSpinClicked);
            }

        }

        private void RenderButtons(RunBattleActionMode mode, bool spinInteractable)
        {
            SetActive(_spinButton, mode == RunBattleActionMode.Spin);

            if (_spinButton != null)
            {
                _spinButton.interactable = spinInteractable;
            }
        }

        private void RenderEmphasis(bool hasPattern)
        {
            Color emphasisColor = hasPattern ? PatternHitColor : BaseAttackColor;
            if (_slotResultText != null)
            {
                _slotResultText.color = string.IsNullOrEmpty(_slotResultText.text)
                    ? _slotResultDefaultColor
                    : emphasisColor;
            }

            if (_attackResultText != null)
            {
                _attackResultText.color = string.IsNullOrEmpty(_attackResultText.text)
                    ? _attackResultDefaultColor
                    : emphasisColor;
            }
        }

        private void CacheDefaultsIfNeeded()
        {
            if (_hasCachedDefaults)
            {
                return;
            }

            _slotResultDefaultColor = _slotResultText != null ? _slotResultText.color : Color.white;
            _attackResultDefaultColor = _attackResultText != null ? _attackResultText.color : Color.white;
            _hasCachedDefaults = true;
        }

        private void HandleSpinClicked()
        {
            SpinRequested?.Invoke();
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetActive(Button button, bool active)
        {
            if (button != null)
            {
                button.gameObject.SetActive(active);
            }
        }
    }
}
