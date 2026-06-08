using System;
using System.Collections.Generic;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyFormationView : MonoBehaviour
    {
        private readonly Dictionary<int, int> _slotIndexByParticipantId = new();

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

            _slotIndexByParticipantId.Clear();
            for (int index = 0; index < slotCount; index++)
            {
                bool hasState = enemySlots != null && index < enemySlots.Length;
                RunBattleEnemySlotState state = hasState
                    ? enemySlots[index]
                    : RunBattleEnemySlotState.Hidden(index);

                if (state.Active && state.ParticipantId.IsValid)
                {
                    _slotIndexByParticipantId[state.ParticipantId.Value] = state.SlotIndex;
                }

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

        public void SetEnemyDamageAnchor(
            CombatParticipantId participantId,
            RectTransform anchor)
        {
            if (!participantId.IsValid || anchor == null)
            {
                return;
            }

            for (int index = 0; index < SlotCount; index++)
            {
                if (GetDamageAnchor(index) == anchor)
                {
                    _slotIndexByParticipantId[participantId.Value] = index;
                    return;
                }
            }
        }

        public RectTransform ResolveDamageAnchor(CombatParticipantId participantId)
        {
            if (participantId.IsValid &&
                _slotIndexByParticipantId.TryGetValue(participantId.Value, out int slotIndex))
            {
                return GetDamageAnchor(slotIndex);
            }

            return GetDamageAnchor(Mathf.Min(1, Mathf.Max(0, SlotCount - 1)));
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
            formationSlotView.SetStatusEffects(state.Statuses);
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
