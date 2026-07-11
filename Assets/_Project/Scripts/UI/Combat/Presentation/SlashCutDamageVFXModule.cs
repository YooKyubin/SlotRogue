using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    /// <summary>
    /// Damage VFX Effect Root 아래에서 slash 애니메이션 프리팹을 재생한다.
    /// </summary>
    public sealed class SlashCutDamageVFXModule : MonoBehaviour, ICombatDamageVFXModule
    {
        [SerializeField] private SlashCutVFXPlayer _slashPrefab;
        [SerializeField] private Vector3 _localPosition;

        private readonly HashSet<SlashCutVFXPlayer> _activeSlashPlayers = new();
        private bool _missingPrefabWarningLogged;
        private bool _missingEffectRootWarningLogged;

        private void OnDisable()
        {
            CancelAndDestroyActiveSlashPlayers();
        }

        private void OnDestroy()
        {
            CancelAndDestroyActiveSlashPlayers();
        }

        public async UniTask PlayAsync(CombatDamageVFXContext context, CancellationToken cancellationToken)
        {
            if (_slashPrefab == null)
            {
                LogMissingPrefabWarning();
                return;
            }

            if (context.EffectRoot == null)
            {
                LogMissingEffectRootWarning();
                return;
            }

            SlashCutVFXPlayer slashPlayer = Instantiate(_slashPrefab, context.EffectRoot);
            ConfigureTransform(slashPlayer.transform);
            slashPlayer.ConfigureCueHub(context.CueHub, cancellationToken);
            _activeSlashPlayers.Add(slashPlayer);

            try
            {
                await slashPlayer.PlayAsync(cancellationToken);
            }
            finally
            {
                _activeSlashPlayers.Remove(slashPlayer);
                if (slashPlayer != null)
                {
                    Destroy(slashPlayer.gameObject);
                }
            }
        }

        private void ConfigureTransform(Transform slashTransform)
        {
            slashTransform.localPosition = _localPosition;
        }

        private void CancelAndDestroyActiveSlashPlayers()
        {
            if (_activeSlashPlayers.Count == 0)
            {
                return;
            }

            var activeSlashPlayers = new List<SlashCutVFXPlayer>(_activeSlashPlayers);
            _activeSlashPlayers.Clear();
            foreach (SlashCutVFXPlayer slashPlayer in activeSlashPlayers)
            {
                if (slashPlayer == null)
                {
                    continue;
                }

                slashPlayer.CancelPlayback();
                Destroy(slashPlayer.gameObject);
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
                "[SlashCutDamageVFXModule] Slash prefab is missing. Assign a SlashCutVFXPlayer prefab.",
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
                "[SlashCutDamageVFXModule] Damage VFX Effect Root is missing. " +
                "Assign the slot effect root before requesting slash VFX.",
                this);
        }
    }
}
