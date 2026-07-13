using System.Collections.Generic;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyIntentListView : MonoBehaviour
    {
        [SerializeField] private Transform _iconRoot;
        [SerializeField] private EnemyIntentIconView _iconPrefab;

        private readonly List<EnemyIntentIconView> _icons = new();
        private bool _missingReferenceWarningLogged;

        public void Render(IReadOnlyList<EnemyUpcomingActionViewData> upcomingActions)
        {
            int actionCount = upcomingActions != null ? upcomingActions.Count : 0;
            gameObject.SetActive(actionCount > 0);
            if (actionCount == 0)
            {
                HideIcons(startIndex: 0);
                return;
            }

            if (_iconRoot == null)
            {
                LogMissingReferenceWarning("Intent Root");
                return;
            }

            if (_iconPrefab == null)
            {
                LogMissingReferenceWarning("Intent Icon Prefab");
                return;
            }

            EnsureIconCount(actionCount);
            for (int index = 0; index < _icons.Count; index++)
            {
                EnemyIntentIconView icon = _icons[index];
                bool active = index < actionCount;
                icon.gameObject.SetActive(active);
                if (active)
                {
                    icon.Set(upcomingActions[index]);
                }
            }
        }

        private void EnsureIconCount(int count)
        {
            while (_icons.Count < count)
            {
                EnemyIntentIconView icon = CreateIcon();
                if (icon == null)
                {
                    return;
                }

                _icons.Add(icon);
            }
        }

        private EnemyIntentIconView CreateIcon()
        {
            EnemyIntentIconView icon = Instantiate(_iconPrefab, _iconRoot);
            icon.name = $"Intent Icon {_icons.Count}";
            icon.gameObject.SetActive(false);
            return icon;
        }

        private void HideIcons(int startIndex)
        {
            for (int index = startIndex; index < _icons.Count; index++)
            {
                EnemyIntentIconView icon = _icons[index];
                if (icon != null)
                {
                    icon.gameObject.SetActive(false);
                }
            }
        }

        private void LogMissingReferenceWarning(string missingReferenceName)
        {
            if (_missingReferenceWarningLogged)
            {
                return;
            }

            _missingReferenceWarningLogged = true;
            Debug.LogWarning(
                $"[EnemyIntentListView] {missingReferenceName} is missing. " +
                "Enemy intent icons will not be shown for this slot.",
                this);
        }
    }
}
