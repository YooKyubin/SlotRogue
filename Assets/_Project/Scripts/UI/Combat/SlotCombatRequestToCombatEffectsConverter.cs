using System;
using System.Collections.Generic;
using SlotRogue.Core.Combat;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;

namespace SlotRogue.UI.Combat
{
    public sealed class SlotCombatRequestToCombatEffectsConverter
    {
        public CombatEffect[] Convert(SlotCombatRequest request)
        {
            return Convert(request, default);
        }

        public CombatEffect[] Convert(
            SlotCombatRequest request,
            CombatParticipantId selectedTargetId)
        {
            if (request == null)
            {
                return System.Array.Empty<CombatEffect>();
            }

            var effects = new List<CombatEffect>();

            if (request.Defense > 0)
            {
                effects.Add(new CombatEffect(CombatEffectKind.Shield, request.Defense, CombatEffectTarget.Self));
            }

            if (request.HealAmount > 0)
            {
                effects.Add(new CombatEffect(CombatEffectKind.Heal, request.HealAmount, CombatEffectTarget.Self));
            }

            if (request.Damage > 0)
            {
                int hitCount = request.AttackCount >= 1 ? request.AttackCount : 1;
                for (int i = 0; i < hitCount; i++)
                {
                    CombatEffectTarget target = selectedTargetId.IsValid
                        ? CombatEffectTarget.SelectedEnemy(selectedTargetId)
                        : CombatEffectTarget.Enemy;
                    effects.Add(new CombatEffect(CombatEffectKind.Damage, request.Damage, target));
                }
            }

            return effects.ToArray();
        }

        /// <summary>
        /// 여러 상태이상을 한 번에 적용하는 오버로드. 유물 다수가 각자 상태이상을 부여할 때 사용한다.
        /// 피해/방어/회복은 단일 경로와 동일하며, 유효한 상태이상마다 ApplyStatus 효과를 추가한다.
        /// </summary>
        public CombatEffect[] Convert(
            SlotCombatRequest request,
            CombatParticipantId selectedTargetId,
            IReadOnlyList<TargetedStatusEffectSpec> statusEffects)
        {
            CombatEffect[] baseEffects = Convert(request, selectedTargetId);

            if (statusEffects == null || statusEffects.Count == 0)
            {
                return baseEffects;
            }

            var effects = new List<CombatEffect>(baseEffects);
            for (int index = 0; index < statusEffects.Count; index++)
            {
                TargetedStatusEffectSpec targetedSpec = statusEffects[index];
                StatusEffectSpec spec = targetedSpec.Spec;
                if (spec.IsValid)
                {
                    effects.Add(CombatEffect.ApplyStatus(
                        spec,
                        ResolveTarget(targetedSpec.TargetMode, selectedTargetId)));
                }
            }

            return effects.ToArray();
        }

        private static CombatEffectTarget ResolveTarget(
            CombatTargetMode targetMode,
            CombatParticipantId selectedTargetId)
        {
            switch (targetMode)
            {
                case CombatTargetMode.Self:
                    return CombatEffectTarget.Self;
                case CombatTargetMode.AllEnemies:
                    return new CombatEffectTarget(CombatTargetMode.AllEnemies);
                case CombatTargetMode.RandomEnemy:
                    return new CombatEffectTarget(CombatTargetMode.RandomEnemy);
                case CombatTargetMode.SelectedEnemy:
                    return selectedTargetId.IsValid
                        ? CombatEffectTarget.SelectedEnemy(selectedTargetId)
                        : CombatEffectTarget.Enemy;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(targetMode),
                        targetMode,
                        "Unsupported combat target mode.");
            }
        }
    }
}
