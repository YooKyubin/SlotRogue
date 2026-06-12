using System;
using UnityEngine;

namespace SlotRogue.UI.App
{
    /// <summary>
    /// Legacy serialized class name for the GameStart View adapter.
    /// It only forwards Unity UI input to the SceneRoot.
    /// </summary>
    public sealed class GameStartSceneController : MonoBehaviour
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
