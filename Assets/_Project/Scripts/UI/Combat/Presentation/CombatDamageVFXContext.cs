using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    /// <summary>
    /// Damage VFX module이 재생 중 사용할 scene 참조를 제공한다.
    /// </summary>
    public readonly struct CombatDamageVFXContext
    {
        /// <summary>
        /// 선택된 VFX set의 모든 module이 공유할 runtime context를 만든다.
        /// </summary>
        public CombatDamageVFXContext(
            CombatDamageVFXRequest request,
            GameObject targetObject,
            Transform effectRoot,
            RectTransform damageAnchor)
        {
            Request = request;
            TargetObject = targetObject;
            EffectRoot = effectRoot;
            DamageAnchor = damageAnchor;
        }

        public CombatDamageVFXRequest Request { get; }

        public GameObject TargetObject { get; }

        public Transform EffectRoot { get; }

        public RectTransform DamageAnchor { get; }
    }
}
