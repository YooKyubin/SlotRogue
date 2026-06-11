using System.Collections.Generic;
using SlotRogue.Slot.Data;

namespace SlotRogue.Relics.Pool
{
    /// <summary>
    /// v20.3 유물 한 개의 정의(코드 카탈로그 데이터). 데이터만 보유하며 실행 로직은 갖지 않는다.
    /// 실행은 <c>RelicEffectRunner</c>(UI.GameFlow)가 <see cref="EffectType"/>로 분기해 처리한다.
    /// </summary>
    public sealed class RelicDefinition
    {
        public RelicDefinition(
            string id,
            RelicGrade grade,
            string name,
            RelicRole role,
            RelicTriggerType triggerType,
            RelicEffectType effectType,
            SlotSymbolType? triggerSymbol,
            SymbolTag? triggerTag,
            int requiredCount,
            int effectValue,
            int effectValue2,
            int enemyHpBelowPercent,
            int playerHpBelowPercentForBonus,
            bool requiresEnemyStatus,
            bool isStarter,
            bool phase1,
            string description,
            string intent,
            string qaRisk)
        {
            Id = id;
            Grade = grade;
            Name = name;
            Role = role;
            TriggerType = triggerType;
            EffectType = effectType;
            TriggerSymbol = triggerSymbol;
            TriggerTag = triggerTag;
            RequiredCount = requiredCount;
            EffectValue = effectValue;
            EffectValue2 = effectValue2;
            EnemyHpBelowPercent = enemyHpBelowPercent;
            PlayerHpBelowPercentForBonus = playerHpBelowPercentForBonus;
            RequiresEnemyStatus = requiresEnemyStatus;
            IsStarter = isStarter;
            Phase1 = phase1;
            Description = description;
            Intent = intent;
            QaRisk = qaRisk;

            Symbols = BuildSymbols(triggerSymbol, triggerTag);
            Tags = BuildTags(triggerSymbol, triggerTag);
        }

        public string Id { get; }
        public RelicGrade Grade { get; }
        public string Name { get; }
        public RelicRole Role { get; }
        public RelicTriggerType TriggerType { get; }
        public RelicEffectType EffectType { get; }

        /// <summary>MatchSymbol 트리거의 대상 심볼.</summary>
        public SlotSymbolType? TriggerSymbol { get; }

        /// <summary>MatchTag 트리거의 대상 태그.</summary>
        public SymbolTag? TriggerTag { get; }

        /// <summary>심볼 N개 이상 / 태그 N개 이상의 N.</summary>
        public int RequiredCount { get; }

        /// <summary>주 효과 수치(피해/방어/회복/스택 등).</summary>
        public int EffectValue { get; }

        /// <summary>보조 수치(예: 조건부 추가 회복).</summary>
        public int EffectValue2 { get; }

        /// <summary>적 HP가 이 % 이하일 때만 발동(0=무시).</summary>
        public int EnemyHpBelowPercent { get; }

        /// <summary>플레이어 HP가 이 % 이하면 EffectValue2를 추가 적용(0=무시).</summary>
        public int PlayerHpBelowPercentForBonus { get; }

        /// <summary>적이 임의의 상태이상을 가질 때만 발동.</summary>
        public bool RequiresEnemyStatus { get; }

        /// <summary>시작 유물 여부(런 시작 선택지에만 등장).</summary>
        public bool IsStarter { get; }

        /// <summary>Phase 1에서 실제 구현·보상풀 등장 가능 여부. false면 카탈로그에만 존재.</summary>
        public bool Phase1 { get; }

        public string Description { get; }
        public string Intent { get; }
        public string QaRisk { get; }

        /// <summary>표시용 대상 심볼 목록(트리거에서 파생).</summary>
        public IReadOnlyList<SlotSymbolType> Symbols { get; }

        /// <summary>표시용 대상 태그 목록(트리거에서 파생).</summary>
        public IReadOnlyList<SymbolTag> Tags { get; }

        private static IReadOnlyList<SlotSymbolType> BuildSymbols(SlotSymbolType? symbol, SymbolTag? tag)
        {
            if (symbol.HasValue)
            {
                return new[] { symbol.Value };
            }

            if (tag.HasValue)
            {
                return SymbolTagMap.SymbolsWithTag(tag.Value);
            }

            return System.Array.Empty<SlotSymbolType>();
        }

        private static IReadOnlyList<SymbolTag> BuildTags(SlotSymbolType? symbol, SymbolTag? tag)
        {
            if (tag.HasValue)
            {
                return new[] { tag.Value };
            }

            if (symbol.HasValue)
            {
                return SymbolTagMap.TagsOf(symbol.Value);
            }

            return System.Array.Empty<SymbolTag>();
        }
    }
}
