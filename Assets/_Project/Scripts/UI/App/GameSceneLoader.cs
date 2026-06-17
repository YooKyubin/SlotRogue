using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlotRogue.UI.App
{
    /// <summary>
    /// Central scene transition entry point.
    /// </summary>
    public static class GameSceneLoader
    {
        public static void LoadBoot()
        {
            SceneManager.LoadScene(SceneNames.Title);
        }

        public static void LoadGameStart()
        {
            SceneManager.LoadScene(SceneNames.Lobby);
        }

        public static UniTask LoadGameStartAsync()
        {
            return LoadSceneAsync(SceneNames.Lobby);
        }

        public static void LoadRunGame()
        {
            SceneManager.LoadScene(SceneNames.RunGame);
        }

        public static UniTask LoadRunGameAsync()
        {
            return LoadSceneAsync(SceneNames.RunGame);
        }

        public static void ReloadRunGame()
        {
            SceneManager.LoadScene(SceneNames.RunGame);
        }

        public static UniTask ReloadRunGameAsync()
        {
            return LoadSceneAsync(SceneNames.RunGame);
        }

        private static async UniTask LoadSceneAsync(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            while (!operation.isDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }
    }
}
