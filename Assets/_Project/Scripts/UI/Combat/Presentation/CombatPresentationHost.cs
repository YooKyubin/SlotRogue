using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class CombatPresentationHost
    {
        public CombatPresentationHost(
            GameObject linkTarget,
            Text statusText,
            Transform floatingTextRoot,
            Font defaultFont,
            Action refreshStatusText)
        {
            LinkTarget = linkTarget;
            StatusText = statusText;
            FloatingTextRoot = floatingTextRoot;
            DefaultFont = defaultFont;
            RefreshStatusText = refreshStatusText;
        }

        public GameObject LinkTarget { get; }

        public Text StatusText { get; }

        public Transform FloatingTextRoot { get; }

        public Font DefaultFont { get; }

        public Action RefreshStatusText { get; }
    }
}
