using SlotRogue.UI.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.App
{
    public sealed class GameStartSceneRoot : MonoBehaviour
    {
        [SerializeField] private GameStartSceneController _view;

        private GameStartViewModel _viewModel;

        private void Awake()
        {
            _viewModel = new GameStartViewModel();

            if (_view == null)
            {
                _view = GetComponent<GameStartSceneController>();
            }

            if (_view != null)
            {
                _view.StartRequested += _viewModel.RequestStartGame;
                _view.QuitRequested += _viewModel.RequestQuitGame;
            }

            _viewModel.StartGameRequested += StartGame;
            _viewModel.QuitGameRequested += QuitGame;
        }

        private void OnDestroy()
        {
            if (_view != null)
            {
                _view.StartRequested -= _viewModel.RequestStartGame;
                _view.QuitRequested -= _viewModel.RequestQuitGame;
            }

            if (_viewModel != null)
            {
                _viewModel.StartGameRequested -= StartGame;
                _viewModel.QuitGameRequested -= QuitGame;
            }
        }

        private static void StartGame()
        {
            GameFlowSession.StartNewRun();
            GameSceneLoader.LoadRunGame();
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
