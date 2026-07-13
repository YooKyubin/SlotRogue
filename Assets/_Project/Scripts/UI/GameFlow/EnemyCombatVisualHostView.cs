using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using SlotRogue.UI.Combat.Presentation;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 슬롯에 생성되는 적 전투 비주얼의 수명주기와 시각 연출을 관리합니다.
    /// 전투 비주얼 프리팹의 생성·파괴, Animator 행동 위임, 사망 Tween, Damage VFX를 담당합니다.
    /// 사망 여부 판단, HUD·Intent 표시, 슬롯 입력 제어는 EnemyFormationSlotView의 책임입니다.
    /// </summary>
    public sealed class EnemyCombatVisualHostView : MonoBehaviour
    {
        private const float DeathDuration = 0.35f;
        private const float DeathEndScale = 0.82f;
        private const float DeathDropDistance = 0.18f;

        [Header("Damage VFX")]
        [SerializeField] private Transform _damageVFXEffectRoot;
        [SerializeField] private RectTransform _damageAnchor;
        [SerializeField] private CombatDamageVFXSet[] _damageVFXSets = Array.Empty<CombatDamageVFXSet>();

        private readonly CombatDamageVFXRunner _damageVFXRunner = new();
        private GameObject _combatVisualPrefab;
        private GameObject _combatVisualInstance;
        private IEnemyCombatVisual _combatVisual;
        private bool _idlePlaybackPending;
        private bool _combatVisualMissingWarningLogged;
        private bool _damageVFXTargetMissingWarningLogged;
        private bool _damageVFXEffectRootMissingWarningLogged;
        private Tween _deathTween;

        private void OnDisable()
        {
            _deathTween?.Kill();
        }

        private void OnDestroy()
        {
            _deathTween?.Kill();
            DestroyCombatVisualInstance();
        }

        public bool TrySetCombatVisualPrefab(GameObject combatVisualPrefab)
        {
            ClearCombatVisual();

            if (combatVisualPrefab == null)
            {
                Debug.LogError(
                    "[EnemyCombatVisualHostView] Combat visual prefab is missing. " +
                    "Assign MonsterVisualDefinition.CombatVisualPrefab before binding the enemy slot.",
                    this);
                return false;
            }

            _combatVisualPrefab = combatVisualPrefab;
            _combatVisualInstance = Instantiate(combatVisualPrefab, transform);
            _combatVisualInstance.transform.localPosition = Vector3.zero;
            _combatVisualInstance.transform.localRotation = Quaternion.identity;
            _combatVisual = _combatVisualInstance.GetComponentInChildren<IEnemyCombatVisual>(includeInactive: true);
            if (_combatVisual == null)
            {
                LogMissingCombatVisualWarning(combatVisualPrefab);
                return false;
            }

            _idlePlaybackPending = true;
            return true;
        }

        public void ClearCombatVisual()
        {
            _deathTween?.Kill();
            _combatVisualPrefab = null;
            _combatVisual = null;
            _idlePlaybackPending = false;
            DestroyCombatVisualInstance();
        }

        public UniTask PlayActionUntilEffectPointAsync(string actionName, CancellationToken cancellationToken)
        {
            if (_combatVisual == null)
            {
                LogMissingCombatVisualWarning(_combatVisualPrefab);
                return UniTask.CompletedTask;
            }

            return _combatVisual.PlayActionUntilEffectPointAsync(actionName, cancellationToken);
        }

        public UniTask WaitForActionCompletedAsync(CancellationToken cancellationToken)
        {
            if (_combatVisual == null)
            {
                LogMissingCombatVisualWarning(_combatVisualPrefab);
                return UniTask.CompletedTask;
            }

            return _combatVisual.WaitForActionCompletedAsync(cancellationToken);
        }

        public UniTask ShowDamageVFXAsync(CombatDamageVFXRequest request, CancellationToken cancellationToken)
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
            if (_combatVisualInstance == null)
            {
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
                            if (spriteRenderer == null)
                            {
                                return;
                            }

                            spriteRenderer.color = startColor;
                        }));
            }

            _deathTween = sequence;
            await CombatPresentationTweens.AwaitTweenAsync(sequence, cancellationToken);
        }

        public void SetPresentationActive(bool active)
        {
            if (active && _idlePlaybackPending)
            {
                _idlePlaybackPending = false;
                PlayIdle();
            }
        }

        public void ResetPresentation()
        {
            _deathTween?.Kill();
            _deathTween = null;
            SetVisible(true);
            ResetSpriteRendererAlpha(_combatVisualInstance);
        }

        public void SetVisible(bool visible)
        {
            if (_combatVisualInstance != null)
            {
                _combatVisualInstance.SetActive(visible);
            }
        }

        private void PlayIdle()
        {
            if (_combatVisual == null)
            {
                LogMissingCombatVisualWarning(_combatVisualPrefab);
                return;
            }

            _combatVisual.PlayIdle();
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

        private void LogMissingCombatVisualWarning(GameObject combatVisualPrefab)
        {
            if (_combatVisualMissingWarningLogged)
            {
                return;
            }

            _combatVisualMissingWarningLogged = true;
            string prefabName = combatVisualPrefab != null ? combatVisualPrefab.name : "the bound combat visual prefab";
            Debug.LogError(
                $"[EnemyCombatVisualHostView] {prefabName} does not provide IEnemyCombatVisual. " +
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
                "[EnemyCombatVisualHostView] Damage VFX target is missing. " +
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
                "[EnemyCombatVisualHostView] Damage VFX Effect Root is missing. " +
                "Assign the Damage VFX Effect Root before requesting Damage VFX.",
                this);
        }
    }
}
