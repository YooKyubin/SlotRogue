using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
#if DOTWEEN
using DG.Tweening;
#endif

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyFormationSlotView : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color EnemySlotColor = new Color(0.11f, 0.14f, 0.2f, 0.96f);
        private static readonly Color SelectedEnemySlotColor = new Color(0.45f, 0.26f, 0.12f, 0.96f);

        [Header("Root")]
        [SerializeField] private Transform _root;
        [SerializeField] private Transform _shakeGroup;

        [Header("Portrait")]
        [SerializeField] private SpriteRenderer _portrait;
        [SerializeField] private Text _placeholderText;

        [Header("HUD")]
        [SerializeField] private Canvas _hudRoot;
        [SerializeField] private Text _hudText;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _statusBackground;
        [SerializeField] private ShieldGaugeView _shieldGauge;

        [Header("Combat Anchors")]
        [SerializeField] private RectTransform _damageAnchor;
        [SerializeField] private Collider2D _clickCollider;

        [Header("Status Effects")]
        [SerializeField] private RectTransform _statusEffectRoot;
        [SerializeField] private GameObject _statusEffectIconPrefab;

        [Header("Intent")]
        [SerializeField] private Transform _intentRoot;
        [SerializeField] private EnemyIntentIconView _intentIconPrefab;

        private readonly List<EnemyStatusEffectIconView> _statusEffectIcons = new();
        private readonly List<EnemyIntentIconView> _intentIcons = new();
        private UnityAction _clickHandler;
        private bool _interactable = true;
        private bool _statusEffectMissingReferenceWarningLogged;
        private bool _intentMissingReferenceWarningLogged;
        private float _hpFillMaxWidth;
        private bool _hpFillLayoutInitialized;
        private bool _hpFillRendered;
        private GameObject _combatVisualPrefab;
#if DOTWEEN
        private Tween _hpFillTween;
#endif

        public Transform Root => _root != null ? _root : transform;

        public Transform ShakeGroup => _shakeGroup;

        public Canvas HudRoot => _hudRoot;

        public RectTransform DamageAnchor => _damageAnchor;

        public ShieldGaugeView ShieldGauge => _shieldGauge;

        public GameObject CombatVisualPrefab => _combatVisualPrefab;

        public void Bind(
            Transform root,
            Transform shakeGroup,
            SpriteRenderer portrait,
            Canvas hudRoot,
            Text hudText,
            Image hpFill,
            Image statusBackground,
            ShieldGaugeView shieldGauge,
            RectTransform damageAnchor,
            Text placeholderText,
            Collider2D clickCollider,
            RectTransform statusEffectRoot = null,
            GameObject statusEffectIconPrefab = null,
            Transform intentRoot = null,
            EnemyIntentIconView intentIconPrefab = null)
        {
            _root = root;
            _shakeGroup = shakeGroup;
            _portrait = portrait;
            _hudRoot = hudRoot;
            _hudText = hudText;
            _hpFill = hpFill;
            _hpFillLayoutInitialized = false;
            _hpFillRendered = false;
            _statusBackground = statusBackground;
            _shieldGauge = shieldGauge;
            _damageAnchor = damageAnchor;
            _placeholderText = placeholderText;
            _clickCollider = clickCollider;
            _statusEffectRoot = statusEffectRoot;
            _statusEffectIconPrefab = statusEffectIconPrefab;
            _intentRoot = intentRoot;
            _intentIconPrefab = intentIconPrefab;
        }

        private void OnDisable()
        {
#if DOTWEEN
            _hpFillTween?.Kill();
#endif
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

        public void SetCombatVisualPrefab(GameObject combatVisualPrefab)
        {
            _combatVisualPrefab = combatVisualPrefab;
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

            RectTransform fillRect = _hpFill.rectTransform;
            RectTransform parent = fillRect.parent as RectTransform;
            if (!_hpFillLayoutInitialized)
            {
                float currentWidth = Mathf.Max(0f, fillRect.rect.width);
                _hpFillMaxWidth = currentWidth > 0f
                    ? currentWidth
                    : Mathf.Max(0f, fillRect.sizeDelta.x);

                float leftInset = 0f;
                if (parent != null)
                {
                    float parentWidth = parent.rect.width;
                    float pivotPosition = (parentWidth * fillRect.anchorMin.x) + fillRect.anchoredPosition.x;
                    leftInset = pivotPosition - (_hpFillMaxWidth * fillRect.pivot.x);
                }

                fillRect.anchorMin = new Vector2(0f, 0.5f);
                fillRect.anchorMax = new Vector2(0f, 0.5f);
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.anchoredPosition = new Vector2(leftInset, fillRect.anchoredPosition.y);
                _hpFillLayoutInitialized = true;
            }

            float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            _hpFill.type = Image.Type.Simple;
            _hpFill.preserveAspect = false;
#if DOTWEEN
            float targetWidth = _hpFillMaxWidth * ratio;
            if (!_hpFillRendered)
            {
                fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                _hpFillRendered = true;
                return;
            }

            _hpFillTween?.Kill();
            _hpFillTween = DOTween.To(
                    () => fillRect.rect.width,
                    width => fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width),
                    targetWidth,
                    0.35f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
#else
            fillRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                _hpFillMaxWidth * ratio);
            _hpFillRendered = true;
#endif
        }

        public void SetShield(int shield)
        {
            if (_shieldGauge != null)
            {
                _shieldGauge.Render(shield);
            }
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

        public void SetUpcomingActions(IReadOnlyList<EnemyUpcomingActionViewData> upcomingActions)
        {
            int actionCount = upcomingActions != null ? upcomingActions.Count : 0;
            if (_intentRoot != null)
            {
                _intentRoot.gameObject.SetActive(actionCount > 0);
            }

            if (actionCount == 0)
            {
                HideIntentIcons(startIndex: 0);
                return;
            }

            if (_intentRoot == null)
            {
                LogMissingIntentReferenceWarning("Intent Root");
                return;
            }

            if (_intentIconPrefab == null)
            {
                LogMissingIntentReferenceWarning("Intent Icon Prefab");
                return;
            }

            EnsureIntentIconCount(actionCount);

            for (int index = 0; index < _intentIcons.Count; index++)
            {
                EnemyIntentIconView icon = _intentIcons[index];
                bool active = index < actionCount;
                icon.gameObject.SetActive(active);
                if (active)
                {
                    icon.Set(upcomingActions[index]);
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

        private void EnsureIntentIconCount(int count)
        {
            while (_intentIcons.Count < count)
            {
                EnemyIntentIconView icon = CreateIntentIcon();
                if (icon == null)
                {
                    return;
                }

                _intentIcons.Add(icon);
            }
        }

        private EnemyIntentIconView CreateIntentIcon()
        {
            if (_intentIconPrefab == null || _intentRoot == null)
            {
                return null;
            }

            EnemyIntentIconView icon = Instantiate(_intentIconPrefab, _intentRoot);
            icon.name = $"Intent Icon {_intentIcons.Count}";
            icon.gameObject.SetActive(false);
            return icon;
        }

        private void HideIntentIcons(int startIndex)
        {
            for (int index = startIndex; index < _intentIcons.Count; index++)
            {
                EnemyIntentIconView icon = _intentIcons[index];
                if (icon != null)
                {
                    icon.gameObject.SetActive(false);
                }
            }
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

        private void LogMissingIntentReferenceWarning(string missingReferenceName)
        {
            if (_intentMissingReferenceWarningLogged)
            {
                return;
            }

            _intentMissingReferenceWarningLogged = true;
            Debug.LogWarning(
                $"[EnemyFormationSlotView] {missingReferenceName} is missing. " +
                "Enemy intent icons will not be shown for this slot.");
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
