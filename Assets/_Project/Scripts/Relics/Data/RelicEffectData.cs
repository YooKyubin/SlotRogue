using System;
using UnityEngine;

namespace SlotRogue.Relics.Data
{
    /// <summary>
    /// 유물이 적용할 효과 한 개의 데이터. 로직은 갖지 않으며
    /// <c>RelicEffectExecutor</c>가 이 데이터를 읽어 실행한다.
    /// </summary>
    [Serializable]
    public sealed class RelicEffectData
    {
        [SerializeField] private RelicEffectType _type = RelicEffectType.AddFlatDamage;

        [Tooltip("정수 수치: 피해/회복/방어/골드/스택 등")]
        [SerializeField] private int _amount;

        [Tooltip("실수 수치: MultiplyDamage(배율), HealByDamagePercent(0~1), AddCritChance/AddCritDamage 등")]
        [SerializeField] private float _value;

        [Tooltip("발동 확률(0~1). 0이면 항상 발동. ChanceAddDamage 등 확률 효과에 사용")]
        [Range(0f, 1f)]
        [SerializeField] private float _chance;

        [Tooltip("Custom 효과의 식별자(예: copy_highest_pattern_damage, revive_once)")]
        [SerializeField] private string _customId = string.Empty;

        public RelicEffectType Type => _type;
        public int Amount => _amount;
        public float Value => _value;
        public float Chance => _chance;
        public string CustomId => _customId;
    }
}
