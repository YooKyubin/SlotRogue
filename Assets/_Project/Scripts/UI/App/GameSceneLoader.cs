using System;
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

        public static UniTask LoadGameStartAsync(IProgress<float> progress = null)
        {
            return LoadSceneAsync(SceneNames.Lobby, progress);
        }

        public static void LoadRunGame()
        {
            SceneManager.LoadScene(SceneNames.RunGame);
        }

        public static UniTask LoadRunGameAsync(IProgress<float> progress = null)
        {
            return LoadSceneAsync(SceneNames.RunGame, progress);
        }

        public static void ReloadRunGame()
        {
            SceneManager.LoadScene(SceneNames.RunGame);
        }

        public static UniTask ReloadRunGameAsync(IProgress<float> progress = null)
        {
            return LoadSceneAsync(SceneNames.RunGame, progress);
        }

        private static async UniTask LoadSceneAsync(
            string sceneName,
            IProgress<float> progress = null)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                SceneManager.LoadScene(sceneName);
                progress?.Report(1f);
                return;
            }

            while (!operation.isDone)
            {
                progress?.Report(Mathf.Clamp01(operation.progress / 0.9f));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            progress?.Report(1f);
        }
    }
}
