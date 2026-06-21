using UnityEngine;

namespace SlotRogue.UI.App
{
    public static class FirstRunTutorialState
    {
        public const string CompletedKey = "SlotRogue.FirstRunTutorial.Completed";

        public static bool IsCompleted => PlayerPrefs.GetInt(CompletedKey, 0) == 1;

        public static void MarkCompleted()
        {
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
        }

        public static void ResetForDebug()
        {
            PlayerPrefs.DeleteKey(CompletedKey);
            PlayerPrefs.Save();
        }
    }
}
