using System.Collections.Generic;
using SlotRogue.Core.Combat;
using SlotRogue.Slot.Data;

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
            return Convert(request, selectedTargetId, StatusEffectSpec.None);
        }

        public CombatEffect[] Convert(
            SlotCombatRequest request,
            CombatParticipantId selectedTargetId,
            StatusEffectSpec statusEffectToApply)
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

            if (statusEffectToApply.IsValid)
            {
                CombatEffectTarget target = selectedTargetId.IsValid
                    ? CombatEffectTarget.SelectedEnemy(selectedTargetId)
                    : CombatEffectTarget.Enemy;
                effects.Add(CombatEffect.ApplyStatus(statusEffectToApply, target));
            }

            return effects.ToArray();
        }
    }
}
