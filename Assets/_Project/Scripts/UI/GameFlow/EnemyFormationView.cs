using System;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyFormationView : MonoBehaviour
    {
        [SerializeField] private MonsterView[] _monsterViews = Array.Empty<MonsterView>();
        [SerializeField] private EnemyFormationSlotView[] _formationSlotViews = Array.Empty<EnemyFormationSlotView>();

        public int SlotCount
        {
            get
            {
                int monsterCount = _monsterViews != null ? _monsterViews.Length : 0;
                int formationSlotCount = _formationSlotViews != null ? _formationSlotViews.Length : 0;
                return Mathf.Max(monsterCount, formationSlotCount);
            }
        }

        public void Bind(MonsterView[] monsterViews)
        {
            _monsterViews = monsterViews ?? Array.Empty<MonsterView>();
            _formationSlotViews = Array.Empty<EnemyFormationSlotView>();
        }

        public void Bind(EnemyFormationSlotView[] formationSlotViews)
        {
            _formationSlotViews = formationSlotViews ?? Array.Empty<EnemyFormationSlotView>();
            _monsterViews = Array.Empty<MonsterView>();
        }

        public void Render(RunBattleEnemySlotState[] enemySlots)
        {
            int slotCount = SlotCount;
            if (slotCount == 0)
            {
                return;
            }

            for (int index = 0; index < slotCount; index++)
            {
                bool hasState = enemySlots != null && index < enemySlots.Length;
                RunBattleEnemySlotState state = hasState
                    ? enemySlots[index]
                    : RunBattleEnemySlotState.Hidden(index);

                if (TryGetMonsterView(index, out MonsterView monsterView))
                {
                    monsterView.Render(state);
                    continue;
                }

                if (TryGetFormationSlotView(index, out EnemyFormationSlotView formationSlotView))
                {
                    RenderFormationSlot(formationSlotView, state);
                }
            }
        }

        public void SetPortrait(int slotIndex, Sprite portrait)
        {
            if (TryGetMonsterView(slotIndex, out MonsterView monsterView))
            {
                monsterView.SetPortrait(portrait);
                return;
            }

            if (TryGetFormationSlotView(slotIndex, out EnemyFormationSlotView formationSlotView))
            {
                formationSlotView.SetPortrait(portrait);
            }
        }

        public void SetClickHandler(int slotIndex, Action action)
        {
            if (TryGetMonsterView(slotIndex, out MonsterView monsterView))
            {
                monsterView.SetClickHandler(action);
                return;
            }

            if (TryGetFormationSlotView(slotIndex, out EnemyFormationSlotView formationSlotView))
            {
                UnityEngine.Events.UnityAction unityAction = null;
                if (action != null)
                {
                    unityAction = action.Invoke;
                }

                formationSlotView.SetClickHandler(unityAction);
            }
        }

        public RectTransform GetDamageAnchor(int slotIndex)
        {
            if (TryGetMonsterView(slotIndex, out MonsterView monsterView))
            {
                return monsterView.DamageAnchor;
            }

            return TryGetFormationSlotView(slotIndex, out EnemyFormationSlotView formationSlotView)
                ? formationSlotView.DamageAnchor
                : null;
        }

        private static void RenderFormationSlot(
            EnemyFormationSlotView formationSlotView,
            RunBattleEnemySlotState state)
        {
            formationSlotView.SetActive(state.Active);
            if (!state.Active)
            {
                return;
            }

            formationSlotView.SetHud(state.Selected ? $"> {state.HudText}" : state.HudText);
            formationSlotView.SetHpFill(state.Hp, state.MaxHp);
            formationSlotView.SetSelected(state.Selected);
            formationSlotView.SetInteractable(state.Interactable);
        }

        private bool TryGetMonsterView(int slotIndex, out MonsterView monsterView)
        {
            monsterView = null;
            if (_monsterViews == null || slotIndex < 0 || slotIndex >= _monsterViews.Length)
            {
                return false;
            }

            monsterView = _monsterViews[slotIndex];
            return monsterView != null;
        }

        private bool TryGetFormationSlotView(
            int slotIndex,
            out EnemyFormationSlotView formationSlotView)
        {
            formationSlotView = null;
            if (_formationSlotViews == null || slotIndex < 0 || slotIndex >= _formationSlotViews.Length)
            {
                return false;
            }

            formationSlotView = _formationSlotViews[slotIndex];
            return formationSlotView != null;
        }
    }
}
