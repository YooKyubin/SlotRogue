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
            var artifacts = StarterArtifactCatalog.All;
            int visibleCount = Mathf.Min(optionViews.Length, artifacts.Count);

            for (int index = 0; index < optionViews.Length; index++)
            {
                GameFlowOptionView optionView = optionViews[index];
                optionView.gameObject.SetActive(index < visibleCount);

                if (index >= visibleCount)
                {
                    continue;
                }

                ArtifactDefinitionSO artifact = artifacts[index];
                optionView.SetText(artifact.DisplayName, artifact.Description);
                optionView.Button.onClick.RemoveAllListeners();
                optionView.Button.onClick.AddListener(() => SelectArtifact(artifact.ArtifactId));
            }
        }

        private static void SelectArtifact(string artifactId)
        {
            GameFlowSession.SelectArtifact(artifactId);
            SceneManager.LoadScene(GameFlowSceneNames.RunMap);
        }
    }
}
