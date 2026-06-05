using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyFormationSlotView : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color EnemySlotColor = new Color(0.11f, 0.14f, 0.2f, 0.96f);
        private static readonly Color SelectedEnemySlotColor = new Color(0.45f, 0.26f, 0.12f, 0.96f);

        [SerializeField] private Transform _root;
        [SerializeField] private Transform _shakeGroup;
        [SerializeField] private SpriteRenderer _portrait;
        [SerializeField] private Canvas _hudRoot;
        [SerializeField] private Text _hudText;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _statusBackground;
        [SerializeField] private RectTransform _damageAnchor;
        [SerializeField] private Text _placeholderText;
        [SerializeField] private Collider2D _clickCollider;
        [SerializeField] private RectTransform _statusEffectRoot;
        [SerializeField] private GameObject _statusEffectIconPrefab;

        private readonly List<EnemyStatusEffectIconView> _statusEffectIcons = new();
        private UnityAction _clickHandler;
        private bool _interactable = true;
        private bool _statusEffectMissingReferenceWarningLogged;

        public Transform Root => _root != null ? _root : transform;

        public Transform ShakeGroup => _shakeGroup;

        public Canvas HudRoot => _hudRoot;

        public RectTransform DamageAnchor => _damageAnchor;

        public void Bind(
            Transform root,
            Transform shakeGroup,
            SpriteRenderer portrait,
            Canvas hudRoot,
            Text hudText,
            Image hpFill,
            Image statusBackground,
            RectTransform damageAnchor,
            Text placeholderText,
            Collider2D clickCollider,
            RectTransform statusEffectRoot = null,
            GameObject statusEffectIconPrefab = null)
        {
            _root = root;
            _shakeGroup = shakeGroup;
            _portrait = portrait;
            _hudRoot = hudRoot;
            _hudText = hudText;
            _hpFill = hpFill;
            _statusBackground = statusBackground;
            _damageAnchor = damageAnchor;
            _placeholderText = placeholderText;
            _clickCollider = clickCollider;
            _statusEffectRoot = statusEffectRoot;
            _statusEffectIconPrefab = statusEffectIconPrefab;
        }

        public void SetPortrait(Sprite sprite)
        {
            if (_portrait != null)
            {
                _portrait.sprite = sprite;
            }

            if (_placeholderText != null)
            {
                _placeholderText.gameObject.SetActive(sprite == null);
            }
        }

        public void SetActive(bool active)
        {
            if (Root != null)
            {
                Root.gameObject.SetActive(active);
            }
        }

        public void SetHud(string value)
        {
            if (_hudText != null)
            {
                _hudText.text = value;
            }
        }

        public void SetHpFill(int current, int max)
        {
            if (_hpFill == null)
            {
                return;
            }

            RectTransform parent = _hpFill.rectTransform.parent as RectTransform;
            float maxWidth = parent != null ? Mathf.Max(1f, parent.sizeDelta.x - 8f) : 1f;
            float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            _hpFill.rectTransform.sizeDelta = new Vector2(maxWidth * ratio, _hpFill.rectTransform.sizeDelta.y);
        }

        public void SetStatusEffects(IReadOnlyList<StatusEffectViewData> statuses)
        {
            AutoBindStatusEffectRootIfNeeded();
            if (_statusEffectRoot == null)
            {
                LogMissingStatusEffectReferenceWarning("Status Effect Root");
                return;
            }

            if (_statusEffectIconPrefab == null)
            {
                LogMissingStatusEffectReferenceWarning("Status Effect Icon Prefab");
                return;
            }

            int statusCount = statuses != null ? statuses.Count : 0;
            EnsureStatusIconCount(statusCount);

            for (int index = 0; index < _statusEffectIcons.Count; index++)
            {
                EnemyStatusEffectIconView icon = _statusEffectIcons[index];
                bool active = index < statusCount;
                icon.gameObject.SetActive(active);
                if (active)
                {
                    icon.Set(statuses[index]);
                }
            }
        }

        public void SetSelected(bool selected)
        {
            if (_statusBackground != null)
            {
                _statusBackground.color = selected ? SelectedEnemySlotColor : EnemySlotColor;
            }
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            if (_clickCollider != null)
            {
                _clickCollider.enabled = interactable;
            }
        }

        public void SetClickHandler(UnityAction action)
        {
            _clickHandler = action;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable)
            {
                return;
            }

            if (_clickHandler == null || _clickCollider == null || !_clickCollider.enabled)
            {
                return;
            }

            _clickHandler.Invoke();
        }

        private void AutoBindStatusEffectRootIfNeeded()
        {
            if (_statusEffectRoot != null)
            {
                return;
            }

            Transform rootTransform = FindDeepChild(Root, "Status Effect Root");
            _statusEffectRoot = rootTransform as RectTransform;
        }

        private void EnsureStatusIconCount(int count)
        {
            while (_statusEffectIcons.Count < count)
            {
                EnemyStatusEffectIconView icon = CreateStatusEffectIcon();
                if (icon == null)
                {
                    return;
                }

                _statusEffectIcons.Add(icon);
            }
        }

        private EnemyStatusEffectIconView CreateStatusEffectIcon()
        {
            if (_statusEffectIconPrefab == null || _statusEffectRoot == null)
            {
                return null;
            }

            GameObject iconObject = Instantiate(_statusEffectIconPrefab, _statusEffectRoot);
            iconObject.name = $"Status Effect Icon {_statusEffectIcons.Count}";

            EnemyStatusEffectIconView icon = iconObject.GetComponent<EnemyStatusEffectIconView>();
            if (icon == null)
            {
                Destroy(iconObject);
                LogMissingStatusEffectReferenceWarning("EnemyStatusEffectIconView component on Status Effect Icon Prefab");
                return null;
            }

            iconObject.SetActive(false);
            return icon;
        }

        private void LogMissingStatusEffectReferenceWarning(string missingReferenceName)
        {
            if (_statusEffectMissingReferenceWarningLogged)
            {
                return;
            }

            _statusEffectMissingReferenceWarningLogged = true;
            Debug.LogWarning(
                $"[EnemyFormationSlotView] {missingReferenceName} is missing. " +
                "Status effect icons will not be shown for this slot.");
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == childName)
            {
                return parent;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform found = FindDeepChild(parent.GetChild(index), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
