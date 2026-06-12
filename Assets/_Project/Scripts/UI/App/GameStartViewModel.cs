using System;

namespace SlotRogue.UI.App
{
    public sealed class GameStartViewModel
    {
        public event Action StartGameRequested;

        public event Action QuitGameRequested;

        public void RequestStartGame()
        {
            StartGameRequested?.Invoke();
        }

        public void RequestQuitGame()
        {
            QuitGameRequested?.Invoke();
        }
    }
}
