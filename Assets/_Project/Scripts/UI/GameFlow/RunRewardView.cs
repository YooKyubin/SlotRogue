using SlotRogue.UI.RunGame;
using SlotRogue.UI.RunGame.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunRewardView : MonoBehaviour, IRunRewardView
    {
        [SerializeField] private Text _summaryText;
        [SerializeField] private GameFlowOptionView[] _rewardOptions;

        [Header("광고 (스텁)")]
        [Tooltip("광고로 보상 3개 싹 바꾸기 — 모든 보상 화면에 표시")]
        [SerializeField] private Button _rerollButton;
        [Tooltip("광고로 보상 1개 더 — 엘리트/보스(큰 보상)에서만 표시")]
        [SerializeField] private Button _addRewardButton;

        public GameFlowOptionView[] RewardOptions => _rewardOptions;

        private RunRewardViewModel _viewModel;

        // ── 기존 Editor 빌더용 Bind (유지) ──────────────────────────────

        public void Bind(Text summaryText, GameFlowOptionView[] rewardOptions)
        {
            _summaryText   = summaryText;
            _rewardOptions = rewardOptions;
        }

        // ── IRunRewardView (MVVM) ────────────────────────────────────────

        public void Bind(RunRewardViewModel viewModel)
        {
            if (_viewModel != null) _viewModel.Changed -= OnViewModelChanged;
            _viewModel = viewModel;
            if (_viewModel != null) _viewModel.Changed += OnViewModelChanged;

            WireAdButtons();
        }

        public void OnEnter()
        {
            gameObject.SetActive(true);
            if (_viewModel == null) return;

            _viewModel.Refresh();
            SetSummary(_viewModel.Summary);
            RebindOptions();
            UpdateAdButtonVisibility();
        }

        public void OnExit()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_viewModel != null) _viewModel.Changed -= OnViewModelChanged;
        }

        // ── 광고 버튼 (스텁) ─────────────────────────────────────────────

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
        }

        private void OnRerollClicked() => _viewModel?.RerollRewards();

        private void OnAddRewardClicked() => _viewModel?.AddExtraReward();

        private void OnViewModelChanged()
        {
            if (_viewModel == null) return;
            SetSummary(_viewModel.Summary);
            RebindOptions();
            UpdateAdButtonVisibility();
        }

        private void UpdateAdButtonVisibility()
        {
            // 리롤 버튼은 모든 보상 화면에 표시, 추가 버튼은 큰 보상(엘리트/보스)에서만 표시.
            if (_addRewardButton != null)
                _addRewardButton.gameObject.SetActive(_viewModel != null && _viewModel.IsBigReward);
        }

        // ── 내부 ────────────────────────────────────────────────────────

        public void SetSummary(string value)
        {
            if (_summaryText != null)
                _summaryText.text = value;
        }

        private void RebindOptions()
        {
            if (_viewModel == null || _rewardOptions == null) return;

            var rewards      = _viewModel.Rewards;
            int visibleCount = Mathf.Min(_rewardOptions.Length, rewards.Count);

            for (int i = 0; i < _rewardOptions.Length; i++)
            {
                GameFlowOptionView option = _rewardOptions[i];
                if (option == null) continue; // 인스펙터 미연결 슬롯은 건너뜀
                option.gameObject.SetActive(i < visibleCount);
                if (i >= visibleCount) continue;

                RunRewardDefinition reward = rewards[i];
                option.SetText(reward.DisplayName, reward.Description);
                option.Button.onClick.RemoveAllListeners();

                option.Button.onClick.AddListener(() => _viewModel.ClaimReward(reward));
            }
        }
    }
}
