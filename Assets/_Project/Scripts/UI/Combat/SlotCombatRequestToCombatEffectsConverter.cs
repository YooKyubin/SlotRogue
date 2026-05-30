using System.Collections.Generic;
using SlotRogue.Core.Combat;
using SlotRogue.Slot.Data;

namespace SlotRogue.UI.Combat
{
    public sealed class SlotCombatRequestToCombatEffectsConverter
    {
        public CombatEffect[] Convert(SlotCombatRequest request)
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
                    effects.Add(new CombatEffect(CombatEffectKind.Damage, request.Damage, CombatEffectTarget.Enemy));
                }
            }

            return effects.ToArray();
        }
    }
}
