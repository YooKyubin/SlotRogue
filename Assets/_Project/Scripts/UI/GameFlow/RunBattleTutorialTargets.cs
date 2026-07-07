using System;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    [Serializable]
    public sealed class RunBattleTutorialTargets
    {
        [SerializeField] private RectTransform _spinTarget;
        [SerializeField] private RectTransform _swapDecisionTarget;
        [SerializeField] private RectTransform _shopTarget;
        [SerializeField] private RectTransform _enemyTarget;

        public static RunBattleTutorialTargets Empty { get; } = new();

        public RectTransform SpinTarget => _spinTarget;

        public RectTransform SwapDecisionTarget => _swapDecisionTarget;

        public RectTransform ShopTarget => _shopTarget;

        public RectTransform EnemyTarget => _enemyTarget;
    }
}
