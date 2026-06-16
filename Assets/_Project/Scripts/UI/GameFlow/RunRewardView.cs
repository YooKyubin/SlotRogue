using System;
using SlotRogue.UI.RunGame;
using SlotRogue.UI.RunGame.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunRewardView : MonoBehaviour, IRunRewardView
    {
        [SerializeField] private Text _summaryText;
        [SerializeField] private GameFlowOptionView[] _rewardOptions;

        [Header("Ads")]
        [Tooltip("Rerolls all reward options after a rewarded ad.")]
        [SerializeField] private Button _rerollButton;
        [SerializeField] private TMP_Text _rerollButtonText;
        [Tooltip("Adds one reward for elite or boss rewards.")]
        [SerializeField] private Button _addRewardButton;
        [SerializeField] private TMP_Text _addRewardButtonText;
        [Tooltip("Doubles the selected reward once.")]
        [SerializeField] private Button _doubleRewardButton;
        [SerializeField] private TMP_Text _doubleRewardButtonText;

        private RectTransform _rewardedActionRow;

        public GameFlowOptionView[] RewardOptions => _rewardOptions;

        public event Action Entered;

        public event Action<int> RewardSelectionRequested;

        public event Action RerollRequested;

        public event Action ExtraRewardRequested;

        public event Action RewardDoubleRequested;

        public void Bind(Text summaryText, GameFlowOptionView[] rewardOptions)
        {
            _summaryText   = summaryText;
            _rewardOptions = rewardOptions;
        }

        private void Awake()
        {
            ResolveAdButtonReferences();
            WireAdButtons();
        }

        private void OnEnable()
        {
            ResolveAdButtonReferences();
            WireAdButtons();
        }

        private void OnDestroy()
        {
            UnwireAdButtons();
        }

        public void OnEnter()
        {
            gameObject.SetActive(true);
            Entered?.Invoke();
        }

        public void OnExit()
        {
            gameObject.SetActive(false);
        }

        public void Render(RunRewardViewState state)
        {
            if (state == null)
            {
                return;
            }

            if (_summaryText != null)
            {
                _summaryText.text = state.Summary;
            }

            RenderOptions(state.Options);
            if (_rerollButton != null)
            {
                _rerollButton.interactable = state.CanReroll;
            }

            if (_rerollButtonText != null)
            {
                _rerollButtonText.text = state.RerollLabel;
            }

            if (_addRewardButton != null)
            {
                _addRewardButton.interactable = state.CanAddReward;
            }

            if (_addRewardButtonText != null)
            {
                _addRewardButtonText.text = state.AddRewardLabel;
            }

            if (_doubleRewardButton != null)
            {
                _doubleRewardButton.interactable = state.CanDoubleReward;
            }

            if (_doubleRewardButtonText != null)
            {
                _doubleRewardButtonText.text = state.DoubleRewardLabel;
            }

            UpdateAdButtonVisibility(state);
        }

        private void ResolveAdButtonReferences()
        {
            _rerollButton ??= FindChildComponent<Button>("Reroll Button");
            _rerollButtonText ??=
                FindChildComponent<TMP_Text>("Reroll Text") ??
                _rerollButton?.GetComponentInChildren<TMP_Text>(includeInactive: true);
            _addRewardButton ??= FindChildComponent<Button>("Add Reward Button");
            _doubleRewardButton ??= FindChildComponent<Button>("Double Reward Button");

            if (_addRewardButton == null)
            {
                _addRewardButton = CreateButtonFromReroll("Add Reward Button");
            }

            if (_doubleRewardButton == null)
            {
                _doubleRewardButton = CreateButtonFromReroll("Double Reward Button");
            }

            EnsureRewardedActionLayout();

            _addRewardButtonText ??=
                FindChildComponent<TMP_Text>("Add Reward Text") ??
                _addRewardButton?.GetComponentInChildren<TMP_Text>(includeInactive: true);
            _doubleRewardButtonText ??=
                FindChildComponent<TMP_Text>("Double Reward Text") ??
                _doubleRewardButton?.GetComponentInChildren<TMP_Text>(includeInactive: true);
        }

        private void WireAdButtons()
        {
            if (_rerollButton != null)
            {
                _rerollButton.onClick.RemoveListener(OnRerollClicked);
                _rerollButton.onClick.AddListener(OnRerollClicked);
            }

            if (_addRewardButton != null)
            {
                _addRewardButton.onClick.RemoveListener(OnAddRewardClicked);
                _addRewardButton.onClick.AddListener(OnAddRewardClicked);
            }

            if (_doubleRewardButton != null)
            {
                _doubleRewardButton.onClick.RemoveListener(OnDoubleRewardClicked);
                _doubleRewardButton.onClick.AddListener(OnDoubleRewardClicked);
            }
        }

        private void UnwireAdButtons()
        {
            _rerollButton?.onClick.RemoveListener(OnRerollClicked);
            _addRewardButton?.onClick.RemoveListener(OnAddRewardClicked);
            _doubleRewardButton?.onClick.RemoveListener(OnDoubleRewardClicked);
        }

        private void OnRerollClicked()
        {
            Debug.Log("[RunRewardView] Rewarded reroll clicked.");
            RerollRequested?.Invoke();
        }

        private void OnAddRewardClicked()
        {
            ExtraRewardRequested?.Invoke();
        }

        private void OnDoubleRewardClicked()
        {
            RewardDoubleRequested?.Invoke();
        }

        private void UpdateAdButtonVisibility(RunRewardViewState state)
        {
            if (_addRewardButton != null)
            {
                _addRewardButton.gameObject.SetActive(state.IsBigReward);
            }

            if (_doubleRewardButton != null)
            {
                _doubleRewardButton.gameObject.SetActive(true);
            }
        }

        private Button CreateButtonFromReroll(string objectName)
        {
            if (_rerollButton == null || _rerollButton.transform.parent == null)
            {
                return null;
            }

            Button button = Instantiate(
                _rerollButton,
                _rerollButton.transform.parent);
            button.name = objectName;
            button.onClick.RemoveAllListeners();
            return button;
        }

        private void EnsureRewardedActionLayout()
        {
            if (_rerollButton == null || _rerollButton.transform.parent == null)
            {
                return;
            }

            Transform currentParent = _rerollButton.transform.parent;
            _rewardedActionRow ??=
                FindDeepChild(transform, "Rewarded Action Row") as RectTransform;

            if (_rewardedActionRow == null)
            {
                var rowObject = new GameObject(
                    "Rewarded Action Row",
                    typeof(RectTransform),
                    typeof(HorizontalLayoutGroup));
                _rewardedActionRow = (RectTransform)rowObject.transform;
                _rewardedActionRow.SetParent(currentParent, false);
                _rewardedActionRow.anchorMin = new Vector2(0f, 0f);
                _rewardedActionRow.anchorMax = new Vector2(1f, 0f);
                _rewardedActionRow.pivot = new Vector2(0.5f, 0.5f);
                _rewardedActionRow.anchoredPosition = new Vector2(0f, 31.4f);
                _rewardedActionRow.sizeDelta = new Vector2(0f, 33f);

                HorizontalLayoutGroup layout =
                    rowObject.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 3f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
            }

            MoveButtonToActionRow(_rerollButton);
            MoveButtonToActionRow(_addRewardButton);
            MoveButtonToActionRow(_doubleRewardButton);
        }

        private void MoveButtonToActionRow(Button button)
        {
            if (button == null ||
                _rewardedActionRow == null ||
                button.transform.parent == _rewardedActionRow)
            {
                return;
            }

            button.transform.SetParent(_rewardedActionRow, false);
            if (button.transform is RectTransform rectTransform)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(0f, 33f);
            }
        }

        private T FindChildComponent<T>(string objectName) where T : Component
        {
            Transform child = FindDeepChild(transform, objectName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static Transform FindDeepChild(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == objectName)
            {
                return parent;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform found = FindDeepChild(parent.GetChild(index), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void RenderOptions(
            System.Collections.Generic.IReadOnlyList<RunRewardOptionViewState> options)
        {
            if (_rewardOptions == null)
            {
                return;
            }

            int optionCount = options != null ? options.Count : 0;
            EnsureRewardOptionCapacity(optionCount);
            int visibleCount = Mathf.Min(_rewardOptions.Length, optionCount);

            for (int index = 0; index < _rewardOptions.Length; index++)
            {
                GameFlowOptionView optionView = _rewardOptions[index];
                if (optionView == null)
                {
                    continue;
                }

                optionView.gameObject.SetActive(index < visibleCount);
                if (index >= visibleCount)
                {
                    continue;
                }

                RunRewardOptionViewState option = options[index];
                optionView.SetText(option.Title, option.Description);
                optionView.Button.onClick.RemoveAllListeners();

                int optionIndex = option.Index;
                optionView.Button.onClick.AddListener(
                    () => RewardSelectionRequested?.Invoke(optionIndex));
            }
        }

        private void EnsureRewardOptionCapacity(int requiredCount)
        {
            if (_rewardOptions == null ||
                requiredCount <= _rewardOptions.Length ||
                _rewardOptions.Length == 0)
            {
                return;
            }

            GameFlowOptionView template =
                _rewardOptions[_rewardOptions.Length - 1];
            if (template == null || template.transform.parent == null)
            {
                return;
            }

            var expanded = new GameFlowOptionView[requiredCount];
            Array.Copy(_rewardOptions, expanded, _rewardOptions.Length);

            for (int index = _rewardOptions.Length; index < requiredCount; index++)
            {
                GameFlowOptionView optionView = Instantiate(
                    template,
                    template.transform.parent);
                optionView.name = $"Reward Option {index + 1}";
                expanded[index] = optionView;
            }

            _rewardOptions = expanded;
        }
    }
}
