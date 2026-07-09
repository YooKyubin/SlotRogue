using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    /// <summary>
    /// 하나의 Damage VFX profile과 이를 구성하는 module 목록을 Inspector에서 직렬화해 묶는다.
    /// </summary>
    [Serializable]
    public sealed class CombatDamageVFXSet
    {
        [SerializeField] private CombatDamageVFXProfile _profile;
        [SerializeField] private MonoBehaviour[] _modules = Array.Empty<MonoBehaviour>();

        public CombatDamageVFXProfile Profile => _profile;

        public IReadOnlyList<MonoBehaviour> Modules => _modules ?? Array.Empty<MonoBehaviour>();
    }
}
