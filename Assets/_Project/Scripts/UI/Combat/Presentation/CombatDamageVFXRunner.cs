using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    internal sealed class CombatDamageVFXRunner
    {
        private readonly HashSet<CombatDamageVFXProfile> _missingSetWarningsLogged = new();
        private readonly HashSet<string> _invalidModuleWarningsLogged = new();

        public async UniTask PlayAsync(
            CombatDamageVFXRequest request,
            IReadOnlyList<CombatDamageVFXSet> sets,
            GameObject targetObject,
            Transform effectRoot,
            RectTransform damageAnchor,
            CancellationToken cancellationToken)
        {
            if (!TryFindSet(request.Profile, sets, out CombatDamageVFXSet damageVFXSet))
            {
                LogMissingSetWarning(request.Profile);
                return;
            }

            IReadOnlyList<MonoBehaviour> modules = damageVFXSet.Modules;
            using var cueHub = new CombatDamageVFXCueHub();
            var subscriptions = new List<IDisposable>();
            CombatDamageVFXContext context = new(request, targetObject, effectRoot, damageAnchor, cueHub);

            try
            {
                for (int index = 0; index < modules.Count; index++)
                {
                    MonoBehaviour module = modules[index];
                    if (module == null)
                    {
                        LogInvalidModuleWarning(request.Profile, index, "module reference is missing");
                        continue;
                    }

                    if (module is ICombatDamageVFXCueSubscriber cueSubscriber)
                    {
                        subscriptions.Add(cueSubscriber.Subscribe(context, cancellationToken));
                    }
                }

                var tasks = new List<UniTask>();
                for (int index = 0; index < modules.Count; index++)
                {
                    MonoBehaviour module = modules[index];
                    if (module == null)
                    {
                        continue;
                    }

                    if (module is ICombatDamageVFXModule damageVFXModule)
                    {
                        tasks.Add(damageVFXModule.PlayAsync(context, cancellationToken));
                        continue;
                    }

                    if (module is not ICombatDamageVFXCueSubscriber)
                    {
                        LogInvalidModuleWarning(
                            request.Profile,
                            index,
                            $"{module.GetType().Name} does not implement a Damage VFX module contract");
                    }
                }

                if (tasks.Count > 0)
                {
                    await UniTask.WhenAll(tasks);
                }
            }
            finally
            {
                for (int index = subscriptions.Count - 1; index >= 0; index--)
                {
                    subscriptions[index].Dispose();
                }
            }
        }

        private static bool TryFindSet(
            CombatDamageVFXProfile profile,
            IReadOnlyList<CombatDamageVFXSet> sets,
            out CombatDamageVFXSet damageVFXSet)
        {
            damageVFXSet = null;
            if (sets == null)
            {
                return false;
            }

            for (int index = 0; index < sets.Count; index++)
            {
                CombatDamageVFXSet candidate = sets[index];
                if (candidate != null && candidate.Profile == profile)
                {
                    damageVFXSet = candidate;
                    return true;
                }
            }

            return false;
        }

        private void LogMissingSetWarning(CombatDamageVFXProfile profile)
        {
            if (!_missingSetWarningsLogged.Add(profile))
            {
                return;
            }

            Debug.LogError(
                $"[CombatDamageVFXRunner] Damage VFX set for profile '{profile}' is missing. " +
                "Assign a CombatDamageVFXSet on the enemy formation slot.");
        }

        private void LogInvalidModuleWarning(
            CombatDamageVFXProfile profile,
            int moduleIndex,
            string reason)
        {
            string key = $"{profile}:{moduleIndex}";
            if (!_invalidModuleWarningsLogged.Add(key))
            {
                return;
            }

            Debug.LogError(
                $"[CombatDamageVFXRunner] Damage VFX module {moduleIndex} for profile '{profile}' is invalid: {reason}.");
        }
    }
}
