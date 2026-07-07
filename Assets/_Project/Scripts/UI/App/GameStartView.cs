using System;
using UnityEngine;

namespace SlotRogue.UI.App
{
    /// <summary>
    /// GameStart(로비) View 어댑터. Unity UI 입력을 SceneRoot로 전달하는 역할만 한다(ADR-0020).
    /// </summary>
    public sealed class GameStartView : MonoBehaviour
    {
        public event Action StartRequested;

        public event Action QuitRequested;

        public void OnClickStartGame()
        {
            StartRequested?.Invoke();
        }

        public void OnClickQuit()
        {
            QuitRequested?.Invoke();
        }
    }
}
