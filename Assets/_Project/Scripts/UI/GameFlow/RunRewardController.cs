using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunRewardController : MonoBehaviour
    {
        [SerializeField] private RunRewardView _view;

        private void Awake()
        {
            GameFlowSession.EnsureRunStarted();

            if (_view == null)
            {
                _view = GetComponent<RunRewardView>();
            }

            if (_view == null)
            {
                return;
            }

            _view.SetSummary(GameFlowSession.BuildSummary());
            BindOptions();
        }

        private void BindOptions()
        {
            GameFlowOptionView[] optionViews = _view.RewardOptions;
            var rewards = RunRewardCatalog.All;
            int visibleCount = Mathf.Min(optionViews.Length, rewards.Count);

            for (int index = 0; index < optionViews.Length; index++)
            {
                GameFlowOptionView optionView = optionViews[index];
                optionView.gameObject.SetActive(index < visibleCount);

                if (index >= visibleCount)
                {
                    continue;
                }

                RunRewardDefinition reward = rewards[index];
                optionView.SetText(reward.DisplayName, reward.Description);
                optionView.Button.onClick.RemoveAllListeners();
                optionView.Button.onClick.AddListener(() => ClaimReward(reward.Type));
            }
        }

        private static void ClaimReward(RunRewardType rewardType)
        {
            GameFlowSession.ApplyReward(rewardType);
            SceneManager.LoadScene(GameFlowSceneNames.RunMap);
        }
    }
}
