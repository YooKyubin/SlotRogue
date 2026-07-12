using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using SlotRogue.UI.Combat.Presentation;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyStatusEffectListView : MonoBehaviour
    {
        [SerializeField] private RectTransform _iconRoot;
        [SerializeField] private GameObject _iconPrefab;

        private readonly Dictionary<StatusEffectKind, EnemyStatusEffectIconView> _iconsByKind = new();
        private bool _missingReferenceWarningLogged;

        public void SetStatusEffects(IReadOnlyList<StatusEffectViewData> statuses)
        {
            if (!HasReferences())
            {
                return;
            }

            var activeKinds = new HashSet<StatusEffectKind>();
            int statusCount = statuses != null ? statuses.Count : 0;
            for (int index = 0; index < statusCount; index++)
            {
                StatusEffectViewData status = statuses[index];
                activeKinds.Add(status.Kind);
                if (!_iconsByKind.TryGetValue(status.Kind, out EnemyStatusEffectIconView icon))
                {
                    icon = CreateIcon(status.Kind);
                    if (icon == null)
                    {
                        return;
                    }

                    _iconsByKind.Add(status.Kind, icon);
                }

                icon.gameObject.SetActive(true);
                icon.Set(status);
            }

            var removedKinds = new List<StatusEffectKind>();
            foreach (KeyValuePair<StatusEffectKind, EnemyStatusEffectIconView> pair in _iconsByKind)
            {
                if (!activeKinds.Contains(pair.Key))
                {
                    DestroyIcon(pair.Value);
                    removedKinds.Add(pair.Key);
                }
            }

            for (int index = 0; index < removedKinds.Count; index++)
            {
                _iconsByKind.Remove(removedKinds[index]);
            }
        }

        public async UniTask AddStatusAsync(StatusEffectViewData status, CancellationToken cancellationToken)
        {
            if (!HasReferences())
            {
                return;
            }

            if (!_iconsByKind.TryGetValue(status.Kind, out EnemyStatusEffectIconView icon))
            {
                icon = CreateIcon(status.Kind);
                if (icon == null)
                {
                    return;
                }

                _iconsByKind.Add(status.Kind, icon);
                await icon.ShowAsync(status, cancellationToken);
                return;
            }

            await icon.UpdateValueAsync(status, cancellationToken);
        }

        public UniTask UpdateStatusValueAsync(StatusEffectViewData status, CancellationToken cancellationToken)
        {
            if (!_iconsByKind.TryGetValue(status.Kind, out EnemyStatusEffectIconView icon))
            {
                Debug.LogError($"[EnemyStatusEffectListView] Cannot update missing status icon '{status.Kind}'.", this);
                return UniTask.CompletedTask;
            }

            return icon.UpdateValueAsync(status, cancellationToken);
        }

        public UniTask PlayStatusActivationAsync(StatusEffectKind kind, CancellationToken cancellationToken)
        {
            if (!_iconsByKind.TryGetValue(kind, out EnemyStatusEffectIconView icon))
            {
                Debug.LogError($"[EnemyStatusEffectListView] Cannot animate missing status icon '{kind}'.", this);
                return UniTask.CompletedTask;
            }

            return icon.PlayActivationAsync(cancellationToken);
        }

        public async UniTask RemoveStatusAsync(StatusEffectKind kind, CancellationToken cancellationToken)
        {
            if (!_iconsByKind.TryGetValue(kind, out EnemyStatusEffectIconView icon))
            {
                Debug.LogError($"[EnemyStatusEffectListView] Cannot remove missing status icon '{kind}'.", this);
                return;
            }

            await icon.HideAsync(cancellationToken);
            _iconsByKind.Remove(kind);
            DestroyIcon(icon);
        }

        private bool HasReferences()
        {
            if (_iconRoot == null)
            {
                LogMissingReferenceWarning("Icon Root");
                return false;
            }

            if (_iconPrefab == null)
            {
                LogMissingReferenceWarning("Icon Prefab");
                return false;
            }

            return true;
        }

        private EnemyStatusEffectIconView CreateIcon(StatusEffectKind kind)
        {
            GameObject iconObject = Instantiate(_iconPrefab, _iconRoot);
            iconObject.name = $"Status Effect Icon {kind}";

            EnemyStatusEffectIconView icon = iconObject.GetComponent<EnemyStatusEffectIconView>();
            if (icon == null)
            {
                Destroy(iconObject);
                LogMissingReferenceWarning("EnemyStatusEffectIconView component on Icon Prefab");
                return null;
            }

            iconObject.SetActive(false);
            return icon;
        }

        private static void DestroyIcon(EnemyStatusEffectIconView icon)
        {
            if (Application.isPlaying)
            {
                Destroy(icon.gameObject);
            }
            else
            {
                DestroyImmediate(icon.gameObject);
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
                $"[EnemyStatusEffectListView] {missingReferenceName} is missing. " +
                "Status effect icons will not be shown for this slot.",
                this);
        }
    }
}
