using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Leaderboard
{
    public sealed class LeaderboardLoginView : MonoBehaviour
    {
        private const string NicknameInputName = "Nickname Input";
        private const string ConfirmButtonName = "Confirm Button";
        private const string StatusTextName = "Status Text";

        [SerializeField] private InputField _nicknameInput;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Text _statusText;

        private bool _initialized;
        private bool _subscribed;

        public event Action<string> ProfileSubmitted;

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            ResolveReferences();
            SubscribeButton();
            _initialized = true;
        }

        public void Render(LeaderboardViewState state)
        {
            Initialize();

            bool shouldShow = state?.IsVisible == true &&
                state.IsProfileRequired;
            gameObject.SetActive(shouldShow);

            if (!shouldShow || state == null)
            {
                return;
            }

            transform.SetAsLastSibling();

            if (_nicknameInput != null && !_nicknameInput.isFocused)
            {
                _nicknameInput.text = state.PlayerName;
            }

            if (_statusText != null)
            {
                _statusText.text = state.StatusMessage;
            }

            SetInteractable(_nicknameInput, !state.IsLoading);
            SetInteractable(_confirmButton, !state.IsLoading);
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (_subscribed && _confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            }
        }

        private void ResolveReferences()
        {
            _nicknameInput ??=
                FindDescendant(NicknameInputName)?.GetComponent<InputField>();
            _confirmButton ??=
                FindDescendant(ConfirmButtonName)?.GetComponent<Button>();
            _statusText ??=
                FindDescendant(StatusTextName)?.GetComponent<Text>();
        }

        private void SubscribeButton()
        {
            if (_subscribed || _confirmButton == null)
            {
                return;
            }

            _confirmButton.onClick.AddListener(HandleConfirmClicked);
            _subscribed = true;
        }

        private void HandleConfirmClicked()
        {
            ProfileSubmitted?.Invoke(_nicknameInput?.text ?? string.Empty);
        }

        private Transform FindDescendant(string objectName)
        {
            Transform[] descendants =
                GetComponentsInChildren<Transform>(includeInactive: true);
            for (int index = 0; index < descendants.Length; index++)
            {
                if (descendants[index].name == objectName)
                {
                    return descendants[index];
                }
            }

            return null;
        }

        private static void SetInteractable(
            Selectable selectable,
            bool interactable)
        {
            if (selectable != null)
            {
                selectable.interactable = interactable;
            }
        }
    }
}
