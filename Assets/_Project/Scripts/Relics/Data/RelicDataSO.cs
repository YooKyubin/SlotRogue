using System.Collections.Generic;
using UnityEngine;

namespace SlotRogue.Relics.Data
{
    /// <summary>
    /// 유물 하나의 데이터(ScriptableObject). <b>데이터만</b> 보유하며 효과 실행 로직을 갖지 않는다.
    /// 효과 실행은 <c>RelicSystem</c> / <c>RelicEffectExecutor</c>가 담당한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Relic/Relic Data", fileName = "NewRelic")]
    public sealed class RelicDataSO : ScriptableObject
    {
        [Header("식별")]
        [SerializeField] private string _relicId;
        [SerializeField] private string _relicName;
        [TextArea(2, 4)]
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private RelicRarity _rarity = RelicRarity.Common;

        [Header("발동")]
        [SerializeField] private RelicTriggerTiming _triggerTiming = RelicTriggerTiming.OnPatternResolved;
        [SerializeField] private RelicApplyMode _applyMode = RelicApplyMode.Once;

        [Header("조건 / 효과")]
        [SerializeField] private RelicConditionData _condition = new RelicConditionData();
        [SerializeField] private List<RelicEffectData> _effects = new List<RelicEffectData>();

        public string RelicId => _relicId;
        public string RelicName => _relicName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public RelicRarity Rarity => _rarity;
        public RelicTriggerTiming TriggerTiming => _triggerTiming;
        public RelicApplyMode ApplyMode => _applyMode;
        public RelicConditionData Condition => _condition;
        public IReadOnlyList<RelicEffectData> Effects => _effects;
    }
}
