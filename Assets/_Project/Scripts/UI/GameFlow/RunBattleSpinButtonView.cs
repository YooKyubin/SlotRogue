using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 전투의 SPIN/ATTACK 버튼 뷰. 같은 버튼이 페이즈에 따라 SPIN↔ATTACK으로 바뀐다.
    /// 슬롯/공격 결과 텍스트는 FinalResultDirector가 담당하므로 여기서는 버튼만 다룬다.
    /// </summary>
    public sealed class RunBattleSpinButtonView : MonoBehaviour
    {
        [SerializeField] private Button _spinButton;
        [SerializeField] private TMP_Text _spinButtonTmpLabel;

        private Button _subscribedSpinButton;
        private bool _missingReferenceErrorLogged;

        public event Action SpinRequested;

        public bool HasRequiredControls
        {
            get
            {
                EnsureReferences();
                return _spinButton != null;
            }
        }

        private void Awake()
        {
            EnsureReferences();
            SubscribeButtons();
        }

        private void OnDestroy()
        {
            UnsubscribeButtons();
        }

        public void Bind(Button spinButton)
        {
            _spinButton = spinButton;
            SubscribeButtons();
        }

        public void Render(RunBattleScreenState state)
        {
            EnsureReferences();
            RenderButtons(state.ActionMode, state.SpinInteractable);
        }

        private void RenderButtons(RunBattleActionMode mode, bool spinInteractable)
        {
            bool visible = mode == RunBattleActionMode.Spin ||
                mode == RunBattleActionMode.Attack;
            SetActive(_spinButton, visible);

            if (_spinButton == null)
            {
                return;
            }

            _spinButton.interactable = spinInteractable;
            if (_spinButtonTmpLabel != null)
            {
                _spinButtonTmpLabel.text = mode == RunBattleActionMode.Attack ? "ATTACK" : "SPIN";
            }
        }

        private void SubscribeButtons()
        {
            if (_subscribedSpinButton == _spinButton)
            {
                return;
            }

            UnsubscribeButtons();
            if (_spinButton != null)
            {
                _spinButton.onClick.AddListener(HandleSpinClicked);
                _subscribedSpinButton = _spinButton;
            }
        }

        private void UnsubscribeButtons()
        {
            if (_subscribedSpinButton != null)
            {
                _subscribedSpinButton.onClick.RemoveListener(HandleSpinClicked);
            }

            _subscribedSpinButton = null;
        }

        private void HandleSpinClicked()
        {
            SpinRequested?.Invoke();
        }

        private void EnsureReferences()
        {
            if (_missingReferenceErrorLogged ||
                (_spinButton != null && _spinButtonTmpLabel != null))
            {
                return;
            }

            _missingReferenceErrorLogged = true;
            var missing = new System.Collections.Generic.List<string>();
            if (_spinButton == null) missing.Add("Spin Button");
            if (_spinButtonTmpLabel == null) missing.Add("Spin Button Label");
            Debug.LogError(
                "[RunBattleSpinButtonView] Spin button references must be wired in the inspector. " +
                $"Missing: {string.Join(", ", missing)}");
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
