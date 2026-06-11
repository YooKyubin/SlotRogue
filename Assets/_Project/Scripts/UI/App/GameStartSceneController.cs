using SlotRogue.UI.GameFlow;
using UnityEngine;

namespace SlotRogue.UI.App
{
    /// <summary>
    /// GameStart(타이틀) 씬의 버튼 OnClick에 연결하는 컨트롤러입니다.
    /// View가 아니라 Controller에서 런 시작 / 씬 전환을 처리하도록 MVVM 경계를 지킵니다.
    ///
    /// Inspector에서 Start 버튼 OnClick → OnClickStartGame,
    /// Quit 버튼 OnClick → OnClickQuit 으로 연결합니다.
    /// </summary>
    public sealed class GameStartSceneController : MonoBehaviour
    {
        /// <summary>Start 버튼: 새 런을 시작하고 RunGame 씬으로 이동합니다.</summary>
        public void OnClickStartGame()
        {
            GameFlowSession.StartNewRun();
            GameSceneLoader.LoadRunGame();
        }

        /// <summary>Quit 버튼: 에디터에서는 PlayMode 종료, 빌드에서는 앱 종료.</summary>
        public void OnClickQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
