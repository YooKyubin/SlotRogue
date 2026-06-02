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
            FloatingDamageTextView floatingDamageTextPrefab,
            RectTransform playerDamageAnchor,
            RectTransform monsterDamageAnchor,
            Font defaultFont,
            Action refreshStatusText)
        {
            LinkTarget = linkTarget;
            StatusText = statusText;
            FloatingTextRoot = floatingTextRoot;
            FloatingDamageTextPrefab = floatingDamageTextPrefab;
            PlayerDamageAnchor = playerDamageAnchor;
            MonsterDamageAnchor = monsterDamageAnchor;
            DefaultFont = defaultFont;
            RefreshStatusText = refreshStatusText;
        }

        public GameObject LinkTarget { get; }

        public Text StatusText { get; }

        public Transform FloatingTextRoot { get; }

        public FloatingDamageTextView FloatingDamageTextPrefab { get; }

        public RectTransform PlayerDamageAnchor { get; }

        public RectTransform MonsterDamageAnchor { get; }

        public Font DefaultFont { get; }

        public Action RefreshStatusText { get; }
    }
}
