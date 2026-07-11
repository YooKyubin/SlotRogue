using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class HitFlashDamageVFXModule : MonoBehaviour, ICombatDamageVFXModule
    {
        private static readonly int FlashAmountPropertyId = Shader.PropertyToID("_FlashAmount");
        private static readonly int FlashColorPropertyId = Shader.PropertyToID("_FlashColor");

        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField, Min(0f)] private float _flashInDuration = 0.04f;
        [SerializeField, Min(0f)] private float _flashOutDuration = 0.12f;
        [SerializeField] private bool _includeInactiveRenderers = true;

        private MaterialPropertyBlock _propertyBlock;
        private Tween _activeTween;
        private SpriteRenderer[] _activeRenderers = Array.Empty<SpriteRenderer>();
        private float _activeFlashAmount;
        private int _activePlaybackId;
        private bool _missingTargetWarningLogged;
        private bool _missingRendererWarningLogged;
        private bool _unsupportedMaterialWarningLogged;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnDisable()
        {
            _activePlaybackId++;
            CancelActivePlayback();
        }

        private void OnDestroy()
        {
            _activePlaybackId++;
            CancelActivePlayback();
        }

        public async UniTask PlayAsync(CombatDamageVFXContext context, CancellationToken cancellationToken)
        {
            int playbackId = BeginPlayback();
            if (context.TargetObject == null)
            {
                LogMissingTargetWarning();
                return;
            }

            SpriteRenderer[] renderers = context.TargetObject.GetComponentsInChildren<SpriteRenderer>(_includeInactiveRenderers);
            if (renderers.Length == 0)
            {
                LogMissingRendererWarning(context.TargetObject);
                return;
            }

            if (!SupportsWhiteOverrideFlash(renderers, context.TargetObject))
            {
                return;
            }

            _activeRenderers = renderers;
            SetFlash(renderers, 0f);

            Sequence sequence = DOTween.Sequence().SetLink(gameObject)
                .Append(CreateFlashTween(renderers, 1f, _flashInDuration))
                .Append(CreateFlashTween(renderers, 0f, _flashOutDuration));
            _activeTween = sequence;

            try
            {
                await CombatPresentationTweens.AwaitTweenAsync(sequence, cancellationToken);
            }
            finally
            {
                if (playbackId == _activePlaybackId)
                {
                    SetFlash(renderers, 0f);
                    _activeRenderers = Array.Empty<SpriteRenderer>();
                    _activeTween = null;
                }
            }
        }

        private int BeginPlayback()
        {
            CancelActivePlayback();
            _activePlaybackId++;
            return _activePlaybackId;
        }

        private void CancelActivePlayback()
        {
            if (_activeTween != null && _activeTween.active)
            {
                _activeTween.Kill();
            }

            SetFlash(_activeRenderers, 0f);
            _activeRenderers = Array.Empty<SpriteRenderer>();
            _activeFlashAmount = 0f;
            _activeTween = null;
        }

        private Tween CreateFlashTween(
            SpriteRenderer[] renderers,
            float targetAmount,
            float duration)
        {
            return DOTween.To(
                    () => _activeFlashAmount,
                    amount =>
                    {
                        _activeFlashAmount = amount;
                        SetFlash(renderers, amount);
                    },
                    targetAmount,
                    Mathf.Max(0f, duration))
                .SetEase(Ease.OutQuad);
        }

        private void SetFlash(SpriteRenderer[] renderers, float amount)
        {
            if (_propertyBlock == null)
            {
                return;
            }

            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(FlashColorPropertyId, _flashColor);
                _propertyBlock.SetFloat(FlashAmountPropertyId, Mathf.Clamp01(amount));
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void LogMissingTargetWarning()
        {
            if (_missingTargetWarningLogged)
            {
                return;
            }

            _missingTargetWarningLogged = true;
            Debug.LogWarning(
                "[HitFlashDamageVFXModule] Damage VFX target is missing. " +
                "White override hit flash cannot be played without a target object.",
                this);
        }

        private void LogMissingRendererWarning(GameObject targetObject)
        {
            if (_missingRendererWarningLogged)
            {
                return;
            }

            _missingRendererWarningLogged = true;
            Debug.LogWarning(
                $"[HitFlashDamageVFXModule] {targetObject.name} has no SpriteRenderer children. " +
                "Assign this module to a damage VFX set whose target visual uses SpriteRenderer.",
                this);
        }

        private bool SupportsWhiteOverrideFlash(SpriteRenderer[] renderers, GameObject targetObject)
        {
            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];
                Material material = renderer.sharedMaterial;
                if (material != null &&
                    material.HasProperty(FlashAmountPropertyId) &&
                    material.HasProperty(FlashColorPropertyId))
                {
                    continue;
                }

                LogUnsupportedMaterialWarning(targetObject, renderer);
                return false;
            }

            return true;
        }

        private void LogUnsupportedMaterialWarning(GameObject targetObject, SpriteRenderer renderer)
        {
            if (_unsupportedMaterialWarningLogged)
            {
                return;
            }

            _unsupportedMaterialWarningLogged = true;
            string materialName = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "no material";
            Debug.LogError(
                $"[HitFlashDamageVFXModule] {targetObject.name} renderer '{renderer.name}' uses {materialName}. " +
                "White override hit flash requires a material with _FlashAmount and _FlashColor properties.",
                this);
        }
    }
}
