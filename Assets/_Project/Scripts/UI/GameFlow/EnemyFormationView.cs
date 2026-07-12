using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using SlotRogue.UI.Combat.Presentation;
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

        public void SetCombatVisualPrefab(int formationSlot, GameObject combatVisualPrefab)
        {
            if (TryGetFormationSlotView(formationSlot, out EnemyFormationSlotView formationSlotView))
            {
                formationSlotView.SetCombatVisualPrefab(combatVisualPrefab);
            }
        }

        public void ClearCombatVisualPrefabs()
        {
            for (int slotIndex = 0; slotIndex < SlotCount; slotIndex++)
            {
                if (TryGetFormationSlotView(slotIndex, out EnemyFormationSlotView formationSlotView))
                {
                    formationSlotView.ClearCombatVisual();
                }
            }
        }

        public void SetPortraitSprite(int formationSlot, Sprite portraitSprite)
        {
            if (TryGetFormationSlotView(formationSlot, out EnemyFormationSlotView formationSlotView))
            {
                formationSlotView.SetPortraitSprite(portraitSprite);
            }
        }

        public void ClearPortraitSprites()
        {
            for (int slotIndex = 0; slotIndex < SlotCount; slotIndex++)
            {
                if (TryGetFormationSlotView(slotIndex, out EnemyFormationSlotView formationSlotView))
                {
                    formationSlotView.SetPortraitSprite(null);
                }
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

        public UniTask WaitHpFillAsync(
            CombatParticipantId participantId,
            CancellationToken cancellationToken)
        {
            if (participantId.IsValid &&
                _slotIndexByParticipantId.TryGetValue(participantId.Value, out int slotIndex) &&
                TryGetFormationSlotView(slotIndex, out EnemyFormationSlotView formationSlotView))
            {
                return formationSlotView.WaitHpFillAsync(cancellationToken);
            }

            return UniTask.CompletedTask;
        }

        public UniTask AddStatusAsync(
            CombatParticipantId participantId,
            StatusEffectViewData status,
            CancellationToken cancellationToken)
        {
            return ResolveStatusSlot(participantId).AddStatusAsync(status, cancellationToken);
        }

        public UniTask UpdateStatusValueAsync(
            CombatParticipantId participantId,
            StatusEffectViewData status,
            CancellationToken cancellationToken)
        {
            return ResolveStatusSlot(participantId).UpdateStatusValueAsync(status, cancellationToken);
        }

        public UniTask PlayStatusActivationAsync(
            CombatParticipantId participantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            return ResolveStatusSlot(participantId).PlayStatusActivationAsync(kind, cancellationToken);
        }

        public UniTask PlayStatusModifierActivationAsync(
            CombatParticipantId ownerParticipantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            return ResolveStatusSlot(ownerParticipantId).PlayStatusActivationAsync(
                kind,
                cancellationToken);
        }

        public UniTask RemoveStatusAsync(
            CombatParticipantId participantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            return ResolveStatusSlot(participantId).RemoveStatusAsync(kind, cancellationToken);
        }

        public UniTask PlayCombatVisualActionUntilEffectPointAsync(
            CombatParticipantId participantId,
            string actionName,
            CancellationToken cancellationToken)
        {
            if (participantId.IsValid &&
                _slotIndexByParticipantId.TryGetValue(participantId.Value, out int slotIndex) &&
                TryGetFormationSlotView(slotIndex, out EnemyFormationSlotView formationSlotView))
            {
                return formationSlotView.PlayCombatVisualActionUntilEffectPointAsync(
                    actionName,
                    cancellationToken);
            }

            _warnings.MissingCombatVisualSlot(participantId);
            return UniTask.CompletedTask;
        }

        public UniTask WaitCombatVisualActionCompletedAsync(
            CombatParticipantId participantId,
            CancellationToken cancellationToken)
        {
            if (participantId.IsValid &&
                _slotIndexByParticipantId.TryGetValue(participantId.Value, out int slotIndex) &&
                TryGetFormationSlotView(slotIndex, out EnemyFormationSlotView formationSlotView))
            {
                return formationSlotView.WaitCombatVisualActionCompletedAsync(cancellationToken);
            }

            _warnings.MissingCombatVisualSlot(participantId);
            return UniTask.CompletedTask;
        }

        public UniTask PlayEnemyDeathAsync(
            CombatParticipantId participantId,
            CancellationToken cancellationToken)
        {
            if (participantId.IsValid &&
                _slotIndexByParticipantId.TryGetValue(participantId.Value, out int slotIndex) &&
                TryGetFormationSlotView(slotIndex, out EnemyFormationSlotView formationSlotView))
            {
                return formationSlotView.PlayDeathAsync(cancellationToken);
            }

            _warnings.MissingCombatVisualSlot(participantId);
            return UniTask.CompletedTask;
        }

        public UniTask ShowCombatDamageVFXAsync(
            CombatDamageVFXRequest request,
            CancellationToken cancellationToken)
        {
            CombatParticipantId participantId = request.TargetParticipantId;
            if (participantId.IsValid &&
                _slotIndexByParticipantId.TryGetValue(participantId.Value, out int slotIndex) &&
                TryGetFormationSlotView(slotIndex, out EnemyFormationSlotView formationSlotView))
            {
                return formationSlotView.ShowCombatDamageVFXAsync(request, cancellationToken);
            }

            _warnings.MissingDamageVFXSlot(participantId);
            return UniTask.CompletedTask;
        }

        private static void RenderFormationSlot(
            EnemyFormationSlotView formationSlotView,
            RunBattleEnemySlotState state)
        {
            formationSlotView.SetPresentationActive(state.Active);
            if (!state.Active)
            {
                return;
            }

            formationSlotView.SetHud(state.HudText);
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

        private EnemyFormationSlotView ResolveStatusSlot(CombatParticipantId participantId)
        {
            if (!participantId.IsValid ||
                !_slotIndexByParticipantId.TryGetValue(participantId.Value, out int slotIndex) ||
                !TryGetFormationSlotView(slotIndex, out EnemyFormationSlotView formationSlotView))
            {
                throw new InvalidOperationException(
                    $"Status presentation slot is missing for participant {participantId.Value}.");
            }

            return formationSlotView;
        }
    }
}
