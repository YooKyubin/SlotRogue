using System;
using System.Collections.Generic;
using SlotRogue.Core.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class CombatPresentationHost
    {
        private readonly Dictionary<int, RectTransform> _enemyDamageAnchors = new();

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

        public void SetEnemyDamageAnchor(
            CombatParticipantId participantId,
            RectTransform anchor)
        {
            if (!participantId.IsValid || anchor == null)
            {
                return;
            }

            _enemyDamageAnchors[participantId.Value] = anchor;
        }

        public RectTransform ResolveDamageAnchor(
            CombatParticipantId participantId,
            bool isPlayerParticipant)
        {
            if (isPlayerParticipant)
            {
                return PlayerDamageAnchor;
            }

            if (participantId.IsValid &&
                _enemyDamageAnchors.TryGetValue(participantId.Value, out RectTransform anchor))
            {
                return anchor;
            }

            return MonsterDamageAnchor;
        }
    }
}
