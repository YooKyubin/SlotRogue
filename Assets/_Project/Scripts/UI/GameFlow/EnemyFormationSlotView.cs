using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
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
        private const float ShieldedHpBarOffsetX = 7f;
        private const float ShieldedHpBarMoveDuration = 0.2f;

        [Header("Root")]
        [SerializeField] private Transform _root;
        [SerializeField] private Transform _shakeGroup;

        [Header("Visual")]
        [SerializeField] private Transform _visualRoot;

        [Header("Selection")]
        [SerializeField] private GameObject _selectionIndicator;

        [Header("HUD")]
        [SerializeField] private Canvas _hudRoot;
        [SerializeField] private Text _hudText;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _hpBarFrame;
        [SerializeField] private RectTransform _hpBarRoot;
        [SerializeField] private ShieldGaugeView _shieldGauge;

        [Header("Shielded HP Bar")]
        [SerializeField] private Sprite _normalHpFillSprite;
        [SerializeField] private Sprite _shieldedHpFillSprite;
        [SerializeField] private Sprite _normalHpBarFrameSprite;
        [SerializeField] private Sprite _shieldedHpBarFrameSprite;

        [Header("Combat Anchors")]
        [SerializeField] private RectTransform _damageAnchor;
        [SerializeField] private Collider2D _clickCollider;

        [Header("Damage VFX")]
        [SerializeField] private Transform _damageVFXEffectRoot;
        [SerializeField] private CombatDamageVFXSet[] _damageVFXSets = Array.Empty<CombatDamageVFXSet>();

        [Header("Status Effects")]
        [SerializeField] private EnemyStatusEffectListView _statusEffectListView;

        [Header("Intent")]
        [SerializeField] private EnemyIntentListView _intentListView;

        private readonly CombatDamageVFXRunner _damageVFXRunner = new();
        private UnityAction _clickHandler;
        private bool _interactable = true;
        private float _hpFillMaxWidth;
        private bool _hpFillLayoutInitialized;
        private bool _hpFillRendered;
        private Sprite _portraitSprite;
        private GameObject _combatVisualPrefab;
        private GameObject _combatVisualInstance;
        private IEnemyCombatVisual _combatVisual;
        private bool _combatVisualIdlePlaybackPending;
        private bool _visualRootMissingWarningLogged;
        private bool _combatVisualMissingWarningLogged;
        private bool _damageVFXTargetMissingWarningLogged;
        private bool _damageVFXEffectRootMissingWarningLogged;
        private bool _deathPresented;
        private bool _shieldedHpBarLayoutRendered;
        private Tween _deathTween;
        private Tween _hpFillTween;
        private Tween _hpBarRootTween;

        public Transform Root => _root != null ? _root : transform;

        public Transform ShakeGroup => _shakeGroup;

        public Canvas HudRoot => _hudRoot;

        public RectTransform DamageAnchor => _damageAnchor;

        public ShieldGaugeView ShieldGauge => _shieldGauge;

        public Sprite PortraitSprite => _portraitSprite;

        private void OnDisable()
        {
            _deathTween?.Kill();
            _hpFillTween?.Kill();
            _hpBarRootTween?.Kill();
        }

        private void OnDestroy()
        {
            _deathTween?.Kill();
            _hpBarRootTween?.Kill();
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

            _combatVisualIdlePlaybackPending = true;
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
            _combatVisualIdlePlaybackPending = false;
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

        public UniTask ShowCombatDamageVFXAsync(CombatDamageVFXRequest request, CancellationToken cancellationToken)
        {
            if (_combatVisualInstance == null)
            {
                LogMissingDamageVFXTargetWarning();
                return UniTask.CompletedTask;
            }

            if (_damageVFXEffectRoot == null)
            {
                LogMissingDamageVFXEffectRootWarning();
                return UniTask.CompletedTask;
            }

            return _damageVFXRunner.PlayAsync(
                request,
                _damageVFXSets,
                _combatVisualInstance,
                _damageVFXEffectRoot,
                _damageAnchor,
                cancellationToken);
        }

        public async UniTask PlayDeathAsync(CancellationToken cancellationToken)
        {
            if (_deathPresented)
            {
                return;
            }

            _deathPresented = true;
            SetInteractable(false);
            if (_intentListView != null)
            {
                _intentListView.gameObject.SetActive(false);
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

        /// <summary>
        /// 슬롯의 표시 수명주기를 전환한다.
        /// 새 전투 비주얼의 최초 Idle 재생은 Root가 활성화된 뒤에만 시작한다.
        /// </summary>
        public void SetPresentationActive(bool active)
        {
            if (Root != null)
            {
                Root.gameObject.SetActive(active);
            }

            if (!active)
            {
                SetSelected(false);
            }

            if (active && _deathPresented)
            {
                HideDeathPresentation();
            }

            if (active && _combatVisualIdlePlaybackPending)
            {
                _combatVisualIdlePlaybackPending = false;
                PlayCombatVisualIdle();
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
            return SlotRogue.UI.Combat.Presentation.CombatPresentationTweens.AwaitTweenAsync(_hpFillTween, cancellationToken);
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

            ApplyShieldedHealthBarSprites(shield > 0);
            ApplyShieldedHealthBarLayout(shield > 0);
        }

        public void SetStatusEffects(IReadOnlyList<StatusEffectViewData> statuses)
        {
            if (_deathPresented)
            {
                HideDeathPresentation();
                return;
            }

            _statusEffectListView.SetStatusEffects(statuses);
        }

        public UniTask AddStatusAsync(StatusEffectViewData status, CancellationToken cancellationToken)
        {
            return _statusEffectListView.AddStatusAsync(status, cancellationToken);
        }

        public UniTask UpdateStatusValueAsync(StatusEffectViewData status, CancellationToken cancellationToken)
        {
            return _statusEffectListView.UpdateStatusValueAsync(status, cancellationToken);
        }

        public UniTask PlayStatusActivationAsync(
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            return _statusEffectListView.PlayStatusActivationAsync(kind, cancellationToken);
        }

        public UniTask RemoveStatusAsync(StatusEffectKind kind, CancellationToken cancellationToken)
        {
            return _statusEffectListView.RemoveStatusAsync(kind, cancellationToken);
        }

        public void SetUpcomingActions(IReadOnlyList<EnemyUpcomingActionViewData> upcomingActions)
        {
            if (_deathPresented)
            {
                HideDeathPresentation();
                return;
            }

            _intentListView.Render(upcomingActions);
        }

        public void SetSelected(bool selected)
        {
            if (_selectionIndicator != null)
            {
                _selectionIndicator.SetActive(selected && !_deathPresented);
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

        private void CaptureDefaultHealthBarSprites()
        {
            if (_hpFill != null && _normalHpFillSprite == null)
            {
                _normalHpFillSprite = _hpFill.sprite;
            }

            if (_hpBarFrame != null && _normalHpBarFrameSprite == null)
            {
                _normalHpBarFrameSprite = _hpBarFrame.sprite;
            }
        }

        private void ApplyShieldedHealthBarSprites(bool shielded)
        {
            CaptureDefaultHealthBarSprites();

            if (_hpFill != null)
            {
                Sprite fillSprite = shielded && _shieldedHpFillSprite != null
                    ? _shieldedHpFillSprite
                    : _normalHpFillSprite;
                _hpFill.sprite = fillSprite;
            }

            if (_hpBarFrame != null)
            {
                Sprite frameSprite = shielded && _shieldedHpBarFrameSprite != null
                    ? _shieldedHpBarFrameSprite
                    : _normalHpBarFrameSprite;
                _hpBarFrame.sprite = frameSprite;
            }
        }

        private void ApplyShieldedHealthBarLayout(bool shielded)
        {
            if (_hpBarRoot == null)
            {
                return;
            }

            float targetX = shielded ? ShieldedHpBarOffsetX : 0f;
            Vector2 targetPosition = new(targetX, _hpBarRoot.anchoredPosition.y);
            if (!_shieldedHpBarLayoutRendered)
            {
                _hpBarRoot.anchoredPosition = targetPosition;
                _shieldedHpBarLayoutRendered = true;
                return;
            }

            _hpBarRootTween?.Kill();
            _hpBarRootTween = DOTween.To(
                    () => _hpBarRoot.anchoredPosition,
                    position => _hpBarRoot.anchoredPosition = position,
                    targetPosition,
                    ShieldedHpBarMoveDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
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

            if (_statusEffectListView != null)
            {
                _statusEffectListView.gameObject.SetActive(true);
            }
        }

        private void HideDeathPresentation()
        {
            SetSelected(false);

            if (_combatVisualInstance != null)
            {
                _combatVisualInstance.SetActive(false);
            }

            if (_hudRoot != null)
            {
                _hudRoot.gameObject.SetActive(false);
            }

            if (_statusEffectListView != null)
            {
                _statusEffectListView.gameObject.SetActive(false);
            }

            if (_intentListView != null)
            {
                _intentListView.gameObject.SetActive(false);
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

        private void LogMissingDamageVFXTargetWarning()
        {
            if (_damageVFXTargetMissingWarningLogged)
            {
                return;
            }

            _damageVFXTargetMissingWarningLogged = true;
            Debug.LogError(
                "[EnemyFormationSlotView] Damage VFX target is missing. " +
                "Bind a combat visual prefab before requesting Damage VFX.",
                this);
        }

        private void LogMissingDamageVFXEffectRootWarning()
        {
            if (_damageVFXEffectRootMissingWarningLogged)
            {
                return;
            }

            _damageVFXEffectRootMissingWarningLogged = true;
            Debug.LogError(
                "[EnemyFormationSlotView] Damage VFX Effect Root is missing. " +
                "Assign the Damage VFX Effect Root before requesting Damage VFX.",
                this);
        }

    }
}
