using System;
using System.Collections.Generic;
using SlotRogue.Core.Combat;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public sealed class EnemyFormationView : MonoBehaviour
    {
        private readonly Dictionary<int, int> _slotIndexByParticipantId = new();
        private readonly EnemyFormationViewWarnings _warnings = new();

        [SerializeField] private EnemyFormationSlotView[] _formationSlotViews = Array.Empty<EnemyFormationSlotView>();

        public int SlotCount
        {
            get
            {
                return _formationSlotViews != null ? _formationSlotViews.Length : 0;
            }
        }

        public void Bind(EnemyFormationSlotView[] formationSlotViews)
        {
            _formationSlotViews = formationSlotViews ?? Array.Empty<EnemyFormationSlotView>();
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

                if (TryGetFormationSlotView(index, out EnemyFormationSlotView formationSlotView))
                {
                    RenderFormationSlot(formationSlotView, state);
                }
            }
        }

        public void SetPortrait(int slotIndex, Sprite portrait)
        {
            if (TryGetFormationSlotView(slotIndex, out EnemyFormationSlotView formationSlotView))
            {
                formationSlotView.SetPortrait(portrait);
            }
        }

        public Sprite GetPrimaryPortrait()
        {
            for (int index = 0; index < SlotCount; index++)
            {
                if (TryGetFormationSlotView(index, out EnemyFormationSlotView formationSlotView) &&
                    formationSlotView.PortraitSprite != null)
                {
                    return formationSlotView.PortraitSprite;
                }
            }

            return null;
        }

        public void SetClickHandler(int slotIndex, Action action)
        {
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
                RectTransform anchor = GetDamageAnchor(slotIndex);
                if (anchor == null)
                {
                    _warnings.MissingSlotDamageAnchor(slotIndex, participantId);
                }

                return anchor;
            }

            _warnings.MissingDamageAnchor(participantId);
            return null;
        }

        public ShieldGaugeView ResolveShieldGauge(CombatParticipantId participantId)
        {
            if (participantId.IsValid &&
                _slotIndexByParticipantId.TryGetValue(participantId.Value, out int slotIndex) &&
                TryGetFormationSlotView(slotIndex, out EnemyFormationSlotView formationSlotView))
            {
                return formationSlotView.ShieldGauge;
            }

            return null;
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
            formationSlotView.SetShield(state.Shield);
            formationSlotView.SetStatusEffects(state.Statuses);
            formationSlotView.SetUpcomingActions(state.UpcomingActions);
            formationSlotView.SetSelected(state.Selected);
            formationSlotView.SetInteractable(state.Interactable);
        }

        private bool TryGetFormationSlotView(
            int slotIndex,
            out EnemyFormationSlotView formationSlotView)
        {
            formationSlotView = null;
            if (_formationSlotViews == null || slotIndex < 0 || slotIndex >= _formationSlotViews.Length)
            {
                _warnings.MissingFormationSlot(slotIndex);
                return false;
            }

            formationSlotView = _formationSlotViews[slotIndex];
            if (formationSlotView == null)
            {
                _warnings.MissingFormationSlot(slotIndex);
                return false;
            }

            return true;
        }
    }
}
