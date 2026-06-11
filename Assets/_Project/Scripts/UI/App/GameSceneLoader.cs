using UnityEngine.SceneManagement;

namespace SlotRogue.UI.App
{
    /// <summary>
    /// 씬(Scene) 단위 전환을 한 곳으로 모은 공통 로더입니다.
    /// SceneManager.LoadScene 직접 호출을 여러 곳에 흩뿌리지 않기 위한 진입점입니다.
    ///
    /// 사용 범위(씬 전환에만 사용):
    ///  - BootScene → GameStart
    ///  - GameStart → RunGame
    ///  - RunGame → GameStart
    ///  - RunGame 재시작
    ///
    /// RunGame 내부 화면 전환(StartRelicSelect / Battle / Reward 등)은 Scene이 아니라
    /// View이므로 여기서 처리하지 않고 RunGameNavigator.GoTo()를 사용합니다.
    /// </summary>
    public static class GameSceneLoader
    {
        public static void LoadBoot()
        {
            SceneManager.LoadScene(SceneNames.Boot);
        }

        public static void LoadGameStart()
        {
            SceneManager.LoadScene(SceneNames.GameStart);
        }

        public static void LoadRunGame()
        {
            SceneManager.LoadScene(SceneNames.RunGame);
        }

        /// <summary>현재 RunGame 씬을 다시 로드해 런을 재시작합니다.</summary>
        public static void ReloadRunGame()
        {
            SceneManager.LoadScene(SceneNames.RunGame);
        }
    }
}
