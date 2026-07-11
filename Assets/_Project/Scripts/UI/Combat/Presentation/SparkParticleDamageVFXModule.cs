using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    /// <summary>
    /// Slash의 Impact cue 위치에 일회성 Spark particle을 재생한다.
    /// </summary>
    public sealed class SparkParticleDamageVFXModule : MonoBehaviour, ICombatDamageVFXCueSubscriber
    {
        [SerializeField] private ParticleSystem _sparkPrefab;
        [SerializeField] private Vector3 _localPosition;

        private readonly HashSet<ParticleSystem> _activeParticles = new();
        private bool _missingPrefabWarningLogged;
        private bool _missingEffectRootWarningLogged;

        private void OnDisable()
        {
            StopAndDestroyActiveParticles();
        }

        private void OnDestroy()
        {
            StopAndDestroyActiveParticles();
        }

        public IDisposable Subscribe(
            CombatDamageVFXContext context,
            CancellationToken cancellationToken)
        {
            return context.CueHub.SubscribeImpact(
                (cue, cueCancellationToken) => PlayAtImpactAsync(context, cue, cueCancellationToken));
        }

        private async UniTask PlayAtImpactAsync(
            CombatDamageVFXContext context,
            CombatDamageVFXCue cue,
            CancellationToken cancellationToken)
        {
            if (_sparkPrefab == null)
            {
                LogMissingPrefabWarning();
                return;
            }

            if (context.EffectRoot == null)
            {
                LogMissingEffectRootWarning();
                return;
            }

            ParticleSystem sparkParticle = Instantiate(_sparkPrefab, context.EffectRoot);
            ConfigureTransform(sparkParticle.transform, context.EffectRoot, cue.WorldPosition);
            _activeParticles.Add(sparkParticle);

            try
            {
                sparkParticle.Play(withChildren: true);
                await WaitForParticleCompletedAsync(sparkParticle, cancellationToken);
            }
            finally
            {
                _activeParticles.Remove(sparkParticle);
                if (sparkParticle != null)
                {
                    Destroy(sparkParticle.gameObject);
                }
            }
        }

        private void ConfigureTransform(Transform particleTransform, Transform effectRoot, Vector3 impactWorldPosition)
        {
            particleTransform.localPosition = effectRoot.InverseTransformPoint(impactWorldPosition) + _localPosition;
            particleTransform.localRotation = Quaternion.identity;
        }

        private static async UniTask WaitForParticleCompletedAsync(
            ParticleSystem particleSystem,
            CancellationToken cancellationToken)
        {
            while (particleSystem != null && particleSystem.IsAlive(withChildren: true))
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private void StopAndDestroyActiveParticles()
        {
            if (_activeParticles.Count == 0)
            {
                return;
            }

            var activeParticles = new List<ParticleSystem>(_activeParticles);
            _activeParticles.Clear();
            foreach (ParticleSystem particleSystem in activeParticles)
            {
                if (particleSystem == null)
                {
                    continue;
                }

                particleSystem.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Destroy(particleSystem.gameObject);
            }
        }

        private void LogMissingPrefabWarning()
        {
            if (_missingPrefabWarningLogged)
            {
                return;
            }

            _missingPrefabWarningLogged = true;
            Debug.LogError(
                "[SparkParticleDamageVFXModule] Spark particle prefab is missing. Assign a non-looping ParticleSystem prefab.",
                this);
        }

        private void LogMissingEffectRootWarning()
        {
            if (_missingEffectRootWarningLogged)
            {
                return;
            }

            _missingEffectRootWarningLogged = true;
            Debug.LogError(
                "[SparkParticleDamageVFXModule] Damage VFX Effect Root is missing. " +
                "Assign the slot effect root before requesting spark VFX.",
                this);
        }
    }
}
