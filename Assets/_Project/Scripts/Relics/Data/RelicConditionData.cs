using System;
using System.Collections.Generic;
using SlotRogue.Slot.Data;
using UnityEngine;

namespace SlotRogue.Relics.Data
{
    /// <summary>
    /// 유물이 언제 발동하는지 판단하는 조건 데이터. 로직은 갖지 않으며
    /// <c>RelicConditionChecker</c>가 이 데이터를 읽어 판정한다.
    /// </summary>
    [Serializable]
    public sealed class RelicConditionData
    {
        [SerializeField] private RelicConditionType _type = RelicConditionType.Always;

        [Header("심볼 / 그룹 / 패턴")]
        [Tooltip("SpecificSymbol 조건의 대상 심볼")]
        [SerializeField] private SlotSymbolType _targetSymbol;
        [Tooltip("SymbolGroup / SymbolCountInPool(GroupSymbolCountAtLeast) 조건의 대상 그룹")]
        [SerializeField] private RelicSymbolGroup _group;
        [Tooltip("MultipleSpecificSymbolsInSameTurn 조건: 모두 발동해야 하는 심볼들")]
        [SerializeField] private List<SlotSymbolType> _symbols = new List<SlotSymbolType>();
        [Tooltip("SpecificPattern 조건: 허용되는 족보 랭크들(가로/세로/대각 등)")]
        [SerializeField] private List<SlotPatternRank> _patternRanks = new List<SlotPatternRank>();

        [Header("수치 / 모드")]
        [Tooltip("족보 수·풀 수 등 비교 임계값(이상/이하)")]
        [SerializeField] private int _minCount = 1;
        [SerializeField] private PatternCountMode _patternCountMode = PatternCountMode.Total;
        [SerializeField] private PoolQueryMode _poolQueryMode = PoolQueryMode.GroupSymbolCountAtLeast;

        [Header("HP / 상태이상 (전용 타입)")]
        [Tooltip("HpBelowPercent 타입: 이 비율(%) 이하일 때 발동")]
        [SerializeField] private int _hpPercentThreshold = 30;
        [Tooltip("EnemyHasStatus 타입: 요구하는 적 상태이상")]
        [SerializeField] private RelicStatusType _requiredEnemyStatus = RelicStatusType.None;

        [Header("추가 게이트 (모든 조건에 AND로 적용, 0/None이면 무시)")]
        [Tooltip("플레이어 HP가 이 비율(%) 이하일 때만 발동. 0이면 무시")]
        [SerializeField] private int _hpBelowPercentGate;
        [Tooltip("적이 이 상태이상일 때만 발동. None이면 무시")]
        [SerializeField] private RelicStatusType _enemyStatusGate = RelicStatusType.None;

        public RelicConditionType Type => _type;
        public SlotSymbolType TargetSymbol => _targetSymbol;
        public RelicSymbolGroup Group => _group;
        public IReadOnlyList<SlotSymbolType> Symbols => _symbols;
        public IReadOnlyList<SlotPatternRank> PatternRanks => _patternRanks;
        public int MinCount => _minCount;
        public PatternCountMode PatternCountMode => _patternCountMode;
        public PoolQueryMode PoolQueryMode => _poolQueryMode;
        public int HpPercentThreshold => _hpPercentThreshold;
        public RelicStatusType RequiredEnemyStatus => _requiredEnemyStatus;
        public int HpBelowPercentGate => _hpBelowPercentGate;
        public RelicStatusType EnemyStatusGate => _enemyStatusGate;
    }
}
