using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using SlotRogue.UI.Combat.Presentation;
using DG.Tweening;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyFormationSlotView : MonoBehaviour, IPointerClickHandler
    {
        private const float DeathDuration = 0.35f;
        private const float DeathEndScale = 0.82f;
        private const float DeathDropDistance = 0.18f;

        private static readonly Color EnemySlotColor = new Color(0.11f, 0.14f, 0.2f, 0.96f);
        private static readonly Color SelectedEnemySlotColor = new Color(0.45f, 0.26f, 0.12f, 0.96f);

        [Header("Root")]
        [SerializeField] private Transform _root;
        [SerializeField] private Transform _shakeGroup;

        [Header("Visual")]
        [SerializeField] private Transform _visualRoot;

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

        private readonly Dictionary<StatusEffectKind, EnemyStatusEffectIconView> _statusEffectIconsByKind = new();
        private readonly List<EnemyIntentIconView> _intentIcons = new();
        private UnityAction _clickHandler;
        private bool _interactable = true;
        private bool _statusEffectMissingReferenceWarningLogged;
        private bool _intentMissingReferenceWarningLogged;
        private float _hpFillMaxWidth;
        private bool _hpFillLayoutInitialized;
        private bool _hpFillRendered;
        private Sprite _portraitSprite;
        private GameObject _combatVisualPrefab;
        private GameObject _combatVisualInstance;
        private IEnemyCombatVisual _combatVisual;
        private bool _visualRootMissingWarningLogged;
        private bool _combatVisualMissingWarningLogged;
        private bool _deathPresented;
        private Tween _deathTween;
        private Tween _hpFillTween;

        public Transform Root => _root != null ? _root : transform;

        public Transform ShakeGroup => _shakeGroup;

        public Canvas HudRoot => _hudRoot;

        public RectTransform DamageAnchor => _damageAnchor;

        public ShieldGaugeView ShieldGauge => _shieldGauge;

        public Sprite PortraitSprite => _portraitSprite;

        public void Bind(
            Transform root,
            Transform shakeGroup,
            Canvas hudRoot,
            Text hudText,
            Image hpFill,
            Image statusBackground,
            ShieldGaugeView shieldGauge,
            RectTransform damageAnchor,
            Collider2D clickCollider,
            RectTransform statusEffectRoot = null,
            GameObject statusEffectIconPrefab = null,
            Transform intentRoot = null,
            EnemyIntentIconView intentIconPrefab = null,
            Transform visualRoot = null)
        {
            _root = root;
            _shakeGroup = shakeGroup;
            _visualRoot = visualRoot;
            _hudRoot = hudRoot;
            _hudText = hudText;
            _hpFill = hpFill;
            _hpFillLayoutInitialized = false;
            _hpFillRendered = false;
            _statusBackground = statusBackground;
            _shieldGauge = shieldGauge;
            _damageAnchor = damageAnchor;
            _clickCollider = clickCollider;
            _statusEffectRoot = statusEffectRoot;
            _statusEffectIconPrefab = statusEffectIconPrefab;
            _intentRoot = intentRoot;
            _intentIconPrefab = intentIconPrefab;
        }

        private void OnDisable()
        {
            _deathTween?.Kill();
            _hpFillTween?.Kill();
        }

        private void OnDestroy()
        {
            _deathTween?.Kill();
            DestroyCombatVisualInstance();
        }

        public void SetCombatVisualPrefab(GameObject combatVisualPrefab)
        {
            ClearCombatVisual();

            if (combatVisualPrefab == null)
            {
                Debug.LogError(
                    "[EnemyFormationSlotView] Combat visual prefab is missing. " +
                    "Assign MonsterVisualDefinition.CombatVisualPrefab before binding the enemy slot.",
                    this);
                return;
            }

            if (_visualRoot == null)
            {
                LogMissingVisualRootWarning();
                return;
            }

            ResetDeathPresentation();
            _combatVisualPrefab = combatVisualPrefab;
            _combatVisualInstance = Instantiate(combatVisualPrefab, _visualRoot);
            _combatVisualInstance.transform.localPosition = Vector3.zero;
            _combatVisualInstance.transform.localRotation = Quaternion.identity;
            _combatVisual = _combatVisualInstance.GetComponentInChildren<IEnemyCombatVisual>(includeInactive: true);
            if (_combatVisual == null)
            {
                LogMissingCombatVisualWarning(combatVisualPrefab);
                return;
            }

            PlayCombatVisualIdle();
        }

        public void SetPortraitSprite(Sprite portraitSprite)
        {
            _portraitSprite = portraitSprite;
        }

        public void ClearCombatVisual()
        {
            _deathTween?.Kill();
            _combatVisualPrefab = null;
            _combatVisual = null;
            DestroyCombatVisualInstance();
        }

        public UniTask PlayCombatVisualActionUntilEffectPointAsync(
            string actionName,
            CancellationToken cancellationToken)
        {
            if (_combatVisual == null)
            {
                LogMissingCombatVisualWarning(_combatVisualPrefab);
                return UniTask.CompletedTask;
            }

            return _combatVisual.PlayActionUntilEffectPointAsync(actionName, cancellationToken);
        }

        public UniTask WaitCombatVisualActionCompletedAsync(CancellationToken cancellationToken)
        {
            if (_combatVisual == null)
            {
                LogMissingCombatVisualWarning(_combatVisualPrefab);
                return UniTask.CompletedTask;
            }

            return _combatVisual.WaitForActionCompletedAsync(cancellationToken);
        }

        public async UniTask PlayDeathAsync(CancellationToken cancellationToken)
        {
            if (_deathPresented)
            {
                return;
            }

            _deathPresented = true;
            SetInteractable(false);
            HideIntentIcons(startIndex: 0);
            if (_intentRoot != null)
            {
                _intentRoot.gameObject.SetActive(false);
            }

            if (_combatVisualInstance == null)
            {
                HideDeathPresentation();
                return;
            }

            _deathTween?.Kill();
            Transform visualTransform = _combatVisualInstance.transform;
            Vector3 startScale = visualTransform.localScale;
            Vector3 targetScale = startScale * DeathEndScale;
            Vector3 targetPosition = visualTransform.localPosition + (Vector3.down * DeathDropDistance);
            SpriteRenderer[] spriteRenderers =
                _combatVisualInstance.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

            Sequence sequence = DOTween.Sequence()
                .SetLink(gameObject);
            sequence.Join(
                visualTransform
                    .DOScale(targetScale, DeathDuration)
                    .SetEase(Ease.InBack));
            sequence.Join(
                visualTransform
                    .DOLocalMove(targetPosition, DeathDuration)
                    .SetEase(Ease.InQuad));

            for (int index = 0; index < spriteRenderers.Length; index++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[index];
                if (spriteRenderer == null)
                {
                    continue;
                }

                Color startColor = spriteRenderer.color;
                sequence.Join(
                    DOTween.To(
                            () => spriteRenderer != null ? spriteRenderer.color.a : 0f,
                            alpha =>
                            {
                                if (spriteRenderer == null)
                                {
                                    return;
                                }

                                Color color = spriteRenderer.color;
                                color.a = alpha;
                                spriteRenderer.color = color;
                            },
                            0f,
                            DeathDuration)
                        .SetEase(Ease.InQuad)
                        .OnKill(() =>
                        {
                            if (spriteRenderer == null || _deathPresented)
                            {
                                return;
                            }

                            spriteRenderer.color = startColor;
                        }));
            }

            _deathTween = sequence;
            await CombatPresentationTweens.AwaitTweenAsync(sequence, cancellationToken);

            HideDeathPresentation();
        }

        private void PlayCombatVisualIdle()
        {
            if (_combatVisual == null)
            {
                LogMissingCombatVisualWarning(_combatVisualPrefab);
                return;
            }

            _combatVisual.PlayIdle();
        }

        public void SetActive(bool active)
        {
            if (Root != null)
            {
                Root.gameObject.SetActive(active);
            }

            if (active && _deathPresented)
            {
                HideDeathPresentation();
            }
        }

        public void SetHud(string value)
        {
            if (_deathPresented)
            {
                return;
            }

            if (_hudText != null)
            {
                _hudText.text = value;
            }
        }

        public void SetHpFill(int current, int max)
        {
            if (_deathPresented)
            {
                return;
            }

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
        }

        public UniTask WaitHpFillAsync(CancellationToken cancellationToken)
        {
            return SlotRogue.UI.Combat.Presentation.CombatPresentationTweens.AwaitTweenAsync(
                _hpFillTween,
                cancellationToken);
        }

        public void SetShield(int shield)
        {
            if (_deathPresented)
            {
                return;
            }

            if (_shieldGauge != null)
            {
                _shieldGauge.Render(shield);
            }
        }

        public void SetStatusEffects(IReadOnlyList<StatusEffectViewData> statuses)
        {
            if (_deathPresented)
            {
                HideDeathPresentation();
                return;
            }

            if (!HasStatusEffectReferences())
            {
                return;
            }

            var activeKinds = new HashSet<StatusEffectKind>();
            int statusCount = statuses != null ? statuses.Count : 0;
            for (int index = 0; index < statusCount; index++)
            {
                StatusEffectViewData status = statuses[index];
                activeKinds.Add(status.Kind);
                if (!_statusEffectIconsByKind.TryGetValue(
                        status.Kind,
                        out EnemyStatusEffectIconView icon))
                {
                    icon = CreateStatusEffectIcon(status.Kind);
                    if (icon == null)
                    {
                        return;
                    }

                    _statusEffectIconsByKind.Add(status.Kind, icon);
                }

                icon.gameObject.SetActive(true);
                icon.Set(status);
            }

            var removedKinds = new List<StatusEffectKind>();
            foreach (KeyValuePair<StatusEffectKind, EnemyStatusEffectIconView> pair in _statusEffectIconsByKind)
            {
                if (!activeKinds.Contains(pair.Key))
                {
                    DestroyStatusEffectIcon(pair.Value);
                    removedKinds.Add(pair.Key);
                }
            }

            for (int index = 0; index < removedKinds.Count; index++)
            {
                _statusEffectIconsByKind.Remove(removedKinds[index]);
            }
        }

        public async UniTask AddStatusAsync(
            StatusEffectViewData status,
            CancellationToken cancellationToken)
        {
            if (!HasStatusEffectReferences())
            {
                return;
            }

            if (!_statusEffectIconsByKind.TryGetValue(
                    status.Kind,
                    out EnemyStatusEffectIconView icon))
            {
                icon = CreateStatusEffectIcon(status.Kind);
                if (icon == null)
                {
                    return;
                }

                _statusEffectIconsByKind.Add(status.Kind, icon);
                await icon.ShowAsync(status, cancellationToken);
                return;
            }

            await icon.UpdateValueAsync(status, cancellationToken);
        }

        public UniTask UpdateStatusValueAsync(
            StatusEffectViewData status,
            CancellationToken cancellationToken)
        {
            if (!_statusEffectIconsByKind.TryGetValue(
                    status.Kind,
                    out EnemyStatusEffectIconView icon))
            {
                Debug.LogError(
                    $"[EnemyFormationSlotView] Cannot update missing status icon '{status.Kind}'.",
                    this);
                return UniTask.CompletedTask;
            }

            return icon.UpdateValueAsync(status, cancellationToken);
        }

        public UniTask PlayStatusActivationAsync(
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            if (!_statusEffectIconsByKind.TryGetValue(
                    kind,
                    out EnemyStatusEffectIconView icon))
            {
                Debug.LogError(
                    $"[EnemyFormationSlotView] Cannot animate missing status icon '{kind}'.",
                    this);
                return UniTask.CompletedTask;
            }

            return icon.PlayActivationAsync(cancellationToken);
        }

        public async UniTask RemoveStatusAsync(
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            if (!_statusEffectIconsByKind.TryGetValue(
                    kind,
                    out EnemyStatusEffectIconView icon))
            {
                Debug.LogError(
                    $"[EnemyFormationSlotView] Cannot remove missing status icon '{kind}'.",
                    this);
                return;
            }

            await icon.HideAsync(cancellationToken);
            _statusEffectIconsByKind.Remove(kind);
            DestroyStatusEffectIcon(icon);
        }

        public void SetUpcomingActions(IReadOnlyList<EnemyUpcomingActionViewData> upcomingActions)
        {
            if (_deathPresented)
            {
                HideDeathPresentation();
                return;
            }

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
            if (_deathPresented)
            {
                return;
            }

            if (_statusBackground != null)
            {
                _statusBackground.color = selected ? SelectedEnemySlotColor : EnemySlotColor;
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_deathPresented)
            {
                _interactable = false;
                if (_clickCollider != null)
                {
                    _clickCollider.enabled = false;
                }

                return;
            }

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

        //private void AutoBindStatusEffectRootIfNeeded()
        //{
        //    if (_statusEffectRoot != null)
        //    {
        //        return;
        //    }

        //    Transform rootTransform = FindDeepChild(Root, "Status Effect Root");
        //    _statusEffectRoot = rootTransform as RectTransform;
        //}

        private void DestroyCombatVisualInstance()
        {
            _combatVisual = null;
            if (_combatVisualInstance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_combatVisualInstance);
            }
            else
            {
                DestroyImmediate(_combatVisualInstance);
            }

            _combatVisualInstance = null;
        }

        private void ResetDeathPresentation()
        {
            _deathPresented = false;
            _deathTween?.Kill();
            _deathTween = null;
            if (_combatVisualInstance != null)
            {
                _combatVisualInstance.SetActive(true);
                ResetSpriteRendererAlpha(_combatVisualInstance);
            }

            if (_hudRoot != null)
            {
                _hudRoot.gameObject.SetActive(true);
            }

            if (_statusEffectRoot != null)
            {
                _statusEffectRoot.gameObject.SetActive(true);
            }
        }

        private void HideDeathPresentation()
        {
            if (_combatVisualInstance != null)
            {
                _combatVisualInstance.SetActive(false);
            }

            if (_hudRoot != null)
            {
                _hudRoot.gameObject.SetActive(false);
            }

            if (_statusEffectRoot != null)
            {
                _statusEffectRoot.gameObject.SetActive(false);
            }

            if (_intentRoot != null)
            {
                _intentRoot.gameObject.SetActive(false);
            }
        }

        private static void ResetSpriteRendererAlpha(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            SpriteRenderer[] spriteRenderers =
                root.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            for (int index = 0; index < spriteRenderers.Length; index++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[index];
                if (spriteRenderer == null)
                {
                    continue;
                }

                Color color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }
        }

        private bool HasStatusEffectReferences()
        {
            if (_statusEffectRoot == null)
            {
                LogMissingStatusEffectReferenceWarning("Status Effect Root");
                return false;
            }

            if (_statusEffectIconPrefab == null)
            {
                LogMissingStatusEffectReferenceWarning("Status Effect Icon Prefab");
                return false;
            }

            return true;
        }

        private EnemyStatusEffectIconView CreateStatusEffectIcon(StatusEffectKind kind)
        {
            GameObject iconObject = Instantiate(_statusEffectIconPrefab, _statusEffectRoot);
            iconObject.name = $"Status Effect Icon {kind}";

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

        private static void DestroyStatusEffectIcon(EnemyStatusEffectIconView icon)
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

        private void LogMissingVisualRootWarning()
        {
            if (_visualRootMissingWarningLogged)
            {
                return;
            }

            _visualRootMissingWarningLogged = true;
            Debug.LogError(
                "[EnemyFormationSlotView] Visual Root is missing. " +
                "Combat visual prefabs must be spawned under the slot VisualRoot.",
                this);
        }

        private void LogMissingCombatVisualWarning(GameObject combatVisualPrefab)
        {
            if (_combatVisualMissingWarningLogged)
            {
                return;
            }

            _combatVisualMissingWarningLogged = true;
            string prefabName = combatVisualPrefab != null ? combatVisualPrefab.name : "the bound combat visual prefab";
            Debug.LogError(
                $"[EnemyFormationSlotView] {prefabName} does not provide IEnemyCombatVisual. " +
                "Add a monster combat visual component to the prefab before using animation requests.",
                this);
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
