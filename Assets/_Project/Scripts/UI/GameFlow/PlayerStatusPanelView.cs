using System;
using System.Collections.Generic;
using DG.Tweening;
using SlotRogue.Core.Combat;
using SlotRogue.UI.Combat.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class PlayerStatusPanelView : IDisposable
    {
        private readonly Dictionary<StatusEffectKind, StatusSlot> _slotsByKind = new();
        private readonly List<StatusSlot> _slots = new();
        private readonly List<StatusEffectKind> _buffOrder = new();
        private readonly List<StatusEffectKind> _debuffOrder = new();
        private readonly RectTransform _root;
        private readonly StatusEffectIconSet _iconSet;

        private StatusSlot _slotTemplate;
        private bool _initialized;
        private bool _reportedMissingReferences;

        public PlayerStatusPanelView(
            RectTransform root,
            StatusEffectIconSet iconSet)
        {
            _root = root;
            _iconSet = iconSet;
            EnsureInitialized();
        }

        public void Dispose()
        {
            for (int index = 0; index < _slots.Count; index++)
            {
                StatusSlot slot = _slots[index];
                if (slot?.Root != null)
                {
                    slot.Root.DOKill(complete: true);
                }
            }
        }

        public void Render(IReadOnlyList<StatusEffectViewData> statuses)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            var visibleKinds = new HashSet<StatusEffectKind>();
            if (statuses != null)
            {
                for (int index = 0; index < statuses.Count; index++)
                {
                    StatusEffectViewData status = statuses[index];
                    if (status.Kind == StatusEffectKind.None)
                    {
                        continue;
                    }

                    visibleKinds.Add(status.Kind);
                    AddOrUpdateStatus(status);
                }
            }

            RemoveMissingStatuses(visibleKinds);
            ApplyLayout();
        }

        private bool EnsureInitialized()
        {
            if (_initialized)
            {
                return _iconSet != null && _slotTemplate != null;
            }

            _initialized = true;
            VerticalLayoutGroup legacyLayoutGroup = _root != null
                ? _root.GetComponent<VerticalLayoutGroup>()
                : null;
            if (legacyLayoutGroup != null)
            {
                legacyLayoutGroup.enabled = false;
            }

            int childCount = _root != null ? _root.childCount : 0;
            for (int index = 0; index < childCount; index++)
            {
                Transform child = _root.GetChild(index);
                if (TryCreateSlot(child.gameObject, out StatusSlot slot))
                {
                    slot.Root.gameObject.SetActive(false);
                    _slots.Add(slot);
                    _slotTemplate ??= slot;
                }
            }

            if (_iconSet == null || _slotTemplate == null)
            {
                ReportMissingReferences();
                return false;
            }

            return true;
        }

        private void AddOrUpdateStatus(StatusEffectViewData status)
        {
            if (!_slotsByKind.TryGetValue(status.Kind, out StatusSlot slot))
            {
                slot = AcquireSlot();
                if (slot == null)
                {
                    return;
                }

                _slotsByKind.Add(status.Kind, slot);
                GetOrder(status.Kind).Add(status.Kind);
                slot.Root.gameObject.SetActive(true);
            }

            slot.Icon.sprite = _iconSet.GetIcon(status.Kind);
            slot.ValueText.text = status.ShowValue
                ? status.DisplayValue.ToString()
                : string.Empty;
        }

        private StatusSlot AcquireSlot()
        {
            for (int index = 0; index < _slots.Count; index++)
            {
                StatusSlot slot = _slots[index];
                if (!_slotsByKind.ContainsValue(slot))
                {
                    return slot;
                }
            }

            GameObject clone = UnityEngine.Object.Instantiate(
                _slotTemplate.Root.gameObject,
                _root);
            clone.name = "Player Status";
            if (!TryCreateSlot(clone, out StatusSlot created))
            {
                UnityEngine.Object.Destroy(clone);
                return null;
            }

            created.Root.gameObject.SetActive(false);
            _slots.Add(created);
            return created;
        }

        private void RemoveMissingStatuses(HashSet<StatusEffectKind> visibleKinds)
        {
            var removedKinds = new List<StatusEffectKind>();
            foreach (KeyValuePair<StatusEffectKind, StatusSlot> pair in _slotsByKind)
            {
                if (!visibleKinds.Contains(pair.Key))
                {
                    removedKinds.Add(pair.Key);
                }
            }

            for (int index = 0; index < removedKinds.Count; index++)
            {
                StatusEffectKind kind = removedKinds[index];
                StatusSlot slot = _slotsByKind[kind];
                slot.Root.DOKill(complete: true);
                slot.Root.gameObject.SetActive(false);
                _slotsByKind.Remove(kind);
                _buffOrder.Remove(kind);
                _debuffOrder.Remove(kind);
            }
        }

        private void ApplyLayout()
        {
            for (int index = 0; index < _buffOrder.Count; index++)
            {
                StatusSlot slot = _slotsByKind[_buffOrder[index]];
                PositionSlot(slot, anchorY: 1f, pivotY: 1f, anchoredY: -GetSlotHeight(slot) * index);
            }

            for (int index = 0; index < _debuffOrder.Count; index++)
            {
                StatusSlot slot = _slotsByKind[_debuffOrder[index]];
                PositionSlot(slot, anchorY: 0f, pivotY: 0f, anchoredY: GetSlotHeight(slot) * index);
            }
        }

        private static void PositionSlot(
            StatusSlot slot,
            float anchorY,
            float pivotY,
            float anchoredY)
        {
            RectTransform root = slot.Root;
            root.anchorMin = new Vector2(0.5f, anchorY);
            root.anchorMax = new Vector2(0.5f, anchorY);
            root.pivot = new Vector2(0.5f, pivotY);
            root.anchoredPosition = new Vector2(0f, anchoredY);
        }

        private static float GetSlotHeight(StatusSlot slot)
        {
            float height = slot.Root.sizeDelta.y;
            return height > 0f ? height : 16f;
        }

        private List<StatusEffectKind> GetOrder(StatusEffectKind kind)
        {
            return StatusEffectPresentationMapper.GetDisplayGroup(kind) ==
                StatusEffectDisplayGroup.Buff
                    ? _buffOrder
                    : _debuffOrder;
        }

        private static bool TryCreateSlot(GameObject root, out StatusSlot slot)
        {
            slot = null;
            if (root == null || root.transform is not RectTransform rectTransform)
            {
                return false;
            }

            Image icon = root.GetComponentInChildren<Image>(includeInactive: true);
            TMP_Text valueText = root.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (icon == null || valueText == null)
            {
                return false;
            }

            slot = new StatusSlot(rectTransform, icon, valueText);
            return true;
        }

        private void ReportMissingReferences()
        {
            if (_reportedMissingReferences)
            {
                return;
            }

            _reportedMissingReferences = true;
            Debug.LogError(
                "[PlayerStatusPanelView] Status icon set and at least one status slot are required.",
                _root);
        }

        private sealed class StatusSlot
        {
            public StatusSlot(RectTransform root, Image icon, TMP_Text valueText)
            {
                Root = root;
                Icon = icon;
                ValueText = valueText;
            }

            public RectTransform Root { get; }

            public Image Icon { get; }

            public TMP_Text ValueText { get; }
        }
    }
}
