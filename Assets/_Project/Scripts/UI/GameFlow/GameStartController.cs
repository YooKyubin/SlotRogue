using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlotRogue.UI.GameFlow
{
    public sealed class GameStartController : MonoBehaviour
    {
        [SerializeField] private GameStartView _view;

        private void Awake()
        {
            if (_view == null)
            {
                _view = GetComponent<GameStartView>();
            }

            if (_view == null)
            {
                return;
            }

            _view.SetSummary(
                "Start a new run. Choose a starter artifact, enter the map, fight with slot spins, claim a reward, and return to the map.");
            _view.StartButton.onClick.RemoveListener(StartNewRun);
            _view.StartButton.onClick.AddListener(StartNewRun);
        }

        private static void StartNewRun()
        {
            GameFlowSession.StartNewRun();
            SceneManager.LoadScene(GameFlowSceneNames.StartArtifactSelection);
        }
    }
}
