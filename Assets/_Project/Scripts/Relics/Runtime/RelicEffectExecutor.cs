using SlotRogue.Relics.Data;
using UnityEngine;

namespace SlotRogue.Relics
{
    /// <summary>유물 효과 데이터를 읽어 컨텍스트에 실제 효과를 적용한다.</summary>
    public sealed class RelicEffectExecutor
    {
        private readonly CustomRelicEffectExecutor _custom;

        public RelicEffectExecutor(CustomRelicEffectExecutor custom = null)
        {
            _custom = custom ?? new CustomRelicEffectExecutor();
        }

        public CustomRelicEffectExecutor Custom => _custom;

        /// <summary>
        /// 효과를 <paramref name="repeatCount"/>회 적용한다. 확률(Chance)이 있으면 반복마다 개별 판정한다.
        /// </summary>
        public void Apply(RelicEffectData effect, int repeatCount, RelicContext context)
        {
            if (effect == null || context == null || repeatCount <= 0)
            {
                return;
            }

            for (int index = 0; index < repeatCount; index++)
            {
                if (effect.Chance > 0f && context.Rng.NextDouble() > effect.Chance)
                {
                    continue;
                }

                ApplyOnce(effect, context);
            }
        }

        private void ApplyOnce(RelicEffectData effect, RelicContext context)
        {
            DamageContext damage = context.Damage;

            switch (effect.Type)
            {
                case RelicEffectType.AddFlatDamage:
                case RelicEffectType.ChanceAddDamage:
                    damage.FlatBonusDamage += effect.Amount;
                    break;

                case RelicEffectType.MultiplyDamage:
                    damage.DamageMultiplier *= effect.Value;
                    break;

                case RelicEffectType.HealPlayer:
                    context.HealAccumulated += effect.Amount;
                    break;

                case RelicEffectType.HealByDamagePercent:
                    context.HealAccumulated += Mathf.RoundToInt(context.LastDamageDealt * effect.Value);
                    break;

                case RelicEffectType.AddShield:
                    context.ShieldAccumulated += effect.Amount;
                    break;

                case RelicEffectType.AddGold:
                    context.GoldAccumulated += effect.Amount;
                    break;

                case RelicEffectType.ApplyBurn:
                    context.StatusApplications.Add(RelicStatusApplication.Burn());
                    break;

                case RelicEffectType.ApplyPoison:
                    context.StatusApplications.Add(
                        RelicStatusApplication.Poison(effect.Amount > 0 ? effect.Amount : 1));
                    break;

                case RelicEffectType.ApplyFreeze:
                    context.StatusApplications.Add(RelicStatusApplication.Freeze());
                    break;

                case RelicEffectType.AddCritChance:
                    damage.CritChance += effect.Value;
                    break;

                case RelicEffectType.AddCritDamage:
                    damage.CritDamageMultiplier += effect.Value;
                    break;

                case RelicEffectType.ReduceEnemyAttack:
                    context.EnemyAttackReduction += effect.Amount;
                    break;

                case RelicEffectType.GainPotion:
                    context.PotionsGained += effect.Amount > 0 ? effect.Amount : 1;
                    break;

                case RelicEffectType.GainRespin:
                    context.RespinTicketsGained += effect.Amount > 0 ? effect.Amount : 1;
                    break;

                case RelicEffectType.AddNextTurnBaseDamage:
                    context.NextTurnBaseDamageAccumulated += effect.Amount;
                    break;

                case RelicEffectType.CopyHighestPatternDamage:
                    damage.FlatBonusDamage += RelicPatternQuery.HighestPatternDamage(context.Patterns);
                    break;

                case RelicEffectType.ReviveOnce:
                    context.ReviveRequested = true;
                    break;

                case RelicEffectType.PlayerTakeDamage:
                    context.PlayerDamageAccumulated += effect.Amount;
                    break;

                case RelicEffectType.AddCurseSymbol:
                    context.CurseSymbolCount += effect.Amount > 0 ? effect.Amount : 1;
                    break;

                case RelicEffectType.RemoveRandomSymbol:
                    context.RemoveRandomSymbolCount += effect.Amount > 0 ? effect.Amount : 1;
                    break;

                case RelicEffectType.Custom:
                    _custom.Execute(effect.CustomId, effect, context);
                    break;
            }
        }
    }
}
