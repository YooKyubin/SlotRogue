using System;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleWorldView : MonoBehaviour
    {
        [SerializeField] private Transform _battleShakeRoot;
        [SerializeField] private EnemyFormationView _enemyFormationView;

        public Transform BattleShakeRoot => _battleShakeRoot;

        public EnemyFormationView EnemyFormationView => _enemyFormationView;

        public bool EnsureReferences()
        {
            _battleShakeRoot ??= ResolveBattleShakeRoot();
            _enemyFormationView ??= GetComponentInChildren<EnemyFormationView>(true);
            _enemyFormationView ??= CreateEnemyFormationView();

            if (_enemyFormationView == null)
            {
                return false;
            }

            if (_enemyFormationView.SlotCount == 0)
            {
                BindFormationChildren(_enemyFormationView);
            }

            return _enemyFormationView.SlotCount > 0;
        }

        public void Bind(Transform battleShakeRoot, EnemyFormationView enemyFormationView)
        {
            _battleShakeRoot = battleShakeRoot;
            _enemyFormationView = enemyFormationView;
            if (_enemyFormationView != null && _enemyFormationView.SlotCount == 0)
            {
                BindFormationChildren(_enemyFormationView);
            }
        }

        public void Render(RunBattleScreenState state)
        {
            EnsureReferences();
            _enemyFormationView?.Render(state.EnemySlots);
        }

        public void SetEnemyPortrait(int slotIndex, Sprite portrait)
        {
            EnsureReferences();
            _enemyFormationView?.SetPortrait(slotIndex, portrait);
        }

        public void SetEnemySlotClickHandler(int slotIndex, Action action)
        {
            EnsureReferences();
            _enemyFormationView?.SetClickHandler(slotIndex, action);
        }

        public RectTransform GetEnemyDamageAnchor(int slotIndex)
        {
            EnsureReferences();
            return _enemyFormationView != null ? _enemyFormationView.GetDamageAnchor(slotIndex) : null;
        }

        private Transform ResolveBattleShakeRoot()
        {
            Transform shakeRoot = SceneComponentResolver.FindDeepChild(transform, "BattleShakeRoot");
            return shakeRoot != null ? shakeRoot : transform;
        }

        private EnemyFormationView CreateEnemyFormationView()
        {
            Transform formationRoot =
                SceneComponentResolver.FindDeepChild(transform, "EnemyFormationView") ??
                SceneComponentResolver.FindDeepChild(transform, "FormationSlotsRoot");
            if (formationRoot == null)
            {
                return null;
            }

            return formationRoot.gameObject.AddComponent<EnemyFormationView>();
        }

        private void BindFormationChildren(EnemyFormationView formationView)
        {
            MonsterView[] monsterViews = ResolveMonsterViews();
            if (monsterViews.Length > 0)
            {
                formationView.Bind(monsterViews);
                return;
            }

            EnemyFormationSlotView[] formationSlotViews = ResolveFormationSlotViews();
            if (formationSlotViews.Length > 0)
            {
                formationView.Bind(formationSlotViews);
            }
        }

        private MonsterView[] ResolveMonsterViews()
        {
            MonsterView[] views = GetComponentsInChildren<MonsterView>(true);
            SortByHierarchyName(views);
            return views;
        }

        private EnemyFormationSlotView[] ResolveFormationSlotViews()
        {
            EnemyFormationSlotView[] views = GetComponentsInChildren<EnemyFormationSlotView>(true);
            SortByHierarchyName(views);
            return views;
        }

        private static void SortByHierarchyName<T>(T[] views)
            where T : Component
        {
            if (views == null || views.Length <= 1)
            {
                return;
            }

            Array.Sort(
                views,
                (left, right) => CompareHierarchyNames(left != null ? left.name : null, right != null ? right.name : null));
        }

        private static int CompareHierarchyNames(string left, string right)
        {
            int leftIndex = ExtractTrailingNumber(left);
            int rightIndex = ExtractTrailingNumber(right);
            if (leftIndex != rightIndex)
            {
                return leftIndex.CompareTo(rightIndex);
            }

            return string.Compare(left, right, StringComparison.Ordinal);
        }

        private static int ExtractTrailingNumber(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return int.MaxValue;
            }

            int end = value.Length - 1;
            while (end >= 0 && !char.IsDigit(value[end]))
            {
                end--;
            }

            if (end < 0)
            {
                return int.MaxValue;
            }

            int start = end;
            while (start > 0 && char.IsDigit(value[start - 1]))
            {
                start--;
            }

            return int.TryParse(value.Substring(start, end - start + 1), out int number)
                ? number
                : int.MaxValue;
        }
    }
}
