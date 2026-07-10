using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    public sealed class HitFlashDamageVFXModule : MonoBehaviour, ICombatDamageVFXModule
    {
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField, Min(0f)] private float _flashInDuration = 0.04f;
        [SerializeField, Min(0f)] private float _flashOutDuration = 0.35f;
        [SerializeField] private bool _includeInactiveRenderers = true;

        private Tween _activeTween;
        private RendererColorSnapshot[] _activeSnapshots = Array.Empty<RendererColorSnapshot>();
        private int _activePlaybackId;
        private bool _missingTargetWarningLogged;
        private bool _missingRendererWarningLogged;

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

            RendererColorSnapshot[] snapshots = CaptureSnapshots(renderers);
            _activeSnapshots = snapshots;

            Sequence sequence = DOTween.Sequence().SetLink(gameObject);
            float flashInDuration = Mathf.Max(0f, _flashInDuration);
            float flashOutDuration = Mathf.Max(0f, _flashOutDuration);

            for (int index = 0; index < snapshots.Length; index++)
            {
                RendererColorSnapshot snapshot = snapshots[index];
                Color flashColor = _flashColor;
                flashColor.a = snapshot.Color.a;

                Sequence rendererSequence = DOTween.Sequence()
                    .Append(CreateColorTween(snapshot.Renderer, flashColor, flashInDuration))
                    .Append(CreateColorTween(snapshot.Renderer, snapshot.Color, flashOutDuration));
                sequence.Join(rendererSequence);
            }

            _activeTween = sequence;

            try
            {
                await CombatPresentationTweens.AwaitTweenAsync(sequence, cancellationToken);
            }
            finally
            {
                if (playbackId == _activePlaybackId)
                {
                    RestoreSnapshots(snapshots);
                    _activeSnapshots = Array.Empty<RendererColorSnapshot>();
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

            RestoreSnapshots(_activeSnapshots);
            _activeSnapshots = Array.Empty<RendererColorSnapshot>();
            _activeTween = null;
        }

        private static RendererColorSnapshot[] CaptureSnapshots(SpriteRenderer[] renderers)
        {
            RendererColorSnapshot[] snapshots = new RendererColorSnapshot[renderers.Length];
            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];
                snapshots[index] = new RendererColorSnapshot(renderer, renderer.color);
            }

            return snapshots;
        }

        private static Tween CreateColorTween(
            SpriteRenderer renderer,
            Color targetColor,
            float duration)
        {
            return DOTween.To(
                () => renderer != null ? renderer.color : targetColor,
                color =>
                {
                    if (renderer != null)
                    {
                        renderer.color = color;
                    }
                },
                targetColor,
                duration);
        }

        private static void RestoreSnapshots(RendererColorSnapshot[] snapshots)
        {
            for (int index = 0; index < snapshots.Length; index++)
            {
                RendererColorSnapshot snapshot = snapshots[index];
                if (snapshot.Renderer != null)
                {
                    snapshot.Renderer.color = snapshot.Color;
                }
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
                "Hit flash cannot be played without a target object.",
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

        private readonly struct RendererColorSnapshot
        {
            public RendererColorSnapshot(SpriteRenderer renderer, Color color)
            {
                Renderer = renderer;
                Color = color;
            }

            public SpriteRenderer Renderer { get; }

            public Color Color { get; }
        }
    }
}
