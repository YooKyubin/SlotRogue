using System.Diagnostics;

using UnityEngine;

namespace SlotRogue.Slot.Core
{
    public static class SlotDebugLog
    {
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string message)
        {
            UnityEngine.Debug.Log($"[SLOT] {message}");
        }
    }
}
