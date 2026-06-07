using System;
using System.Collections.Generic;
using SlotRogue.Relics.Data;
using UnityEngine;

namespace SlotRogue.Relics
{
    /// <summary>
    /// CustomEffectId로 처리되는 특수 유물 효과를 실행한다.
    /// 새 특수 효과는 여기에 핸들러를 등록(<see cref="Register"/>)하면 SO 데이터만으로 연결된다.
    /// </summary>
    public sealed class CustomRelicEffectExecutor
    {
        private readonly Dictionary<string, Action<RelicEffectData, RelicContext>> _handlers =
            new Dictionary<string, Action<RelicEffectData, RelicContext>>();

        public CustomRelicEffectExecutor()
        {
            RegisterDefaults();
        }

        /// <summary>커스텀 효과 핸들러를 등록/교체한다.</summary>
        public void Register(string customId, Action<RelicEffectData, RelicContext> handler)
        {
            if (string.IsNullOrEmpty(customId) || handler == null)
            {
                return;
            }

            _handlers[customId] = handler;
        }

        public void Execute(string customId, RelicEffectData effect, RelicContext context)
        {
            if (string.IsNullOrEmpty(customId))
            {
                Debug.LogWarning("[Relic] Custom 효과에 customId가 비어 있습니다.");
                return;
            }

            if (_handlers.TryGetValue(customId, out Action<RelicEffectData, RelicContext> handler))
            {
                handler(effect, context);
                return;
            }

            Debug.LogWarning($"[Relic] 등록되지 않은 customId: {customId}");
        }

        private void RegisterDefaults()
        {
            // 가장 높은 피해 족보 피해를 한 번 더 추가.
            Register("copy_highest_pattern_damage", (effect, context) =>
            {
                int bonus = RelicPatternQuery.HighestPatternDamage(context.Patterns);
                context.Damage.FlatBonusDamage += bonus;
            });

            // 전투당 1회 부활 요청(실제 부활은 연결 계층이 1회 제한과 함께 처리).
            Register("revive_once", (effect, context) =>
            {
                context.ReviveRequested = true;
            });

            // 부자의 검: 보유 골드 10당 +1, 최대 +10.
            Register("rich_sword_damage", (effect, context) =>
            {
                int bonus = Mathf.Clamp(context.PlayerGold / 10, 0, 10);
                context.Damage.FlatBonusDamage += bonus;
            });

            // 도박사의 손: 이번 피해에 50%~150% 랜덤 배율.
            Register("gamblers_hand", (effect, context) =>
            {
                float multiplier = 0.5f + (float)context.Rng.NextDouble();
                context.Damage.DamageMultiplier *= multiplier;
            });

            // 불안정한 머신: 족보가 있으면 최종 피해 +50%, 없으면 플레이어 5 피해.
            Register("unstable_machine", (effect, context) =>
            {
                if (context.Patterns != null && context.Patterns.Count > 0)
                {
                    context.Damage.DamageMultiplier *= 1.5f;
                }
                else
                {
                    context.PlayerDamageAccumulated += 5;
                }
            });

            // 저주받은 7: 전투 시작 시 저주 심볼 1개 추가(연결 계층이 풀에 반영).
            Register("cursed_seven_add_curse", (effect, context) =>
            {
                context.CurseSymbolCount += effect.Amount > 0 ? effect.Amount : 1;
            });

            // 상인의 눈 / 정리 전문가: 보상·상점 시스템 연결 전까지 동작 없는 placeholder
            // (연결 시 여기서 보상 선택지 +1 / 제거 비용 감소를 구현).
            Register("merchant_eye", (effect, context) => { });
            Register("shop_removal_discount", (effect, context) => { });
        }
    }
}
