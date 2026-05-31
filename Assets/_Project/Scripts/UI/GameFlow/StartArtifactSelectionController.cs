using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlotRogue.UI.GameFlow
{
    public sealed class StartArtifactSelectionController : MonoBehaviour
    {
        [SerializeField] private StartArtifactSelectionView _view;

        private void Awake()
        {
            GameFlowSession.EnsureRunStarted();

            if (_view == null)
            {
                _view = GetComponent<StartArtifactSelectionView>();
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
            GameFlowOptionView[] optionViews = _view.ArtifactOptions;
            int visibleCount = Mathf.Min(optionViews.Length, StarterArtifactCatalog.All.Count);

            for (int index = 0; index < optionViews.Length; index++)
            {
                GameFlowOptionView optionView = optionViews[index];
                optionView.gameObject.SetActive(index < visibleCount);

                if (index >= visibleCount)
                {
                    continue;
                }

                StarterArtifactDefinition artifact = StarterArtifactCatalog.All[index];
                optionView.SetText(artifact.DisplayName, artifact.Description);
                optionView.Button.onClick.RemoveAllListeners();
                optionView.Button.onClick.AddListener(() => SelectArtifact(artifact.Id));
            }
        }

        private static void SelectArtifact(StarterArtifactId artifactId)
        {
            GameFlowSession.SelectStarterArtifact(artifactId);
            SceneManager.LoadScene(GameFlowSceneNames.RunMap);
        }
    }
}
