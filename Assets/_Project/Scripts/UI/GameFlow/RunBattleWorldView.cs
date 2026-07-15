using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using SlotRogue.UI.Combat.Presentation;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleWorldView : MonoBehaviour
    {
        [SerializeField] private Transform _battleShakeRoot;
        [SerializeField] private EnemyFormationView _enemyFormationView;

        private bool _missingReferenceErrorLogged;

        public Transform BattleShakeRoot => _battleShakeRoot;

        public EnemyFormationView EnemyFormationView => _enemyFormationView;

        public bool EnsureReferences()
        {
            bool complete = _battleShakeRoot != null &&
                _enemyFormationView != null &&
                _enemyFormationView.SlotCount > 0;
            if (!complete && !_missingReferenceErrorLogged)
            {
                _missingReferenceErrorLogged = true;
                Debug.LogError(
                    "[RunBattleWorldView] Battle world references must be wired in the inspector. " +
                    $"Missing: {BuildMissingReferenceSummary()}");
            }

            return complete;
        }

        public void Bind(Transform battleShakeRoot, EnemyFormationView enemyFormationView)
        {
            _battleShakeRoot = battleShakeRoot;
            _enemyFormationView = enemyFormationView;
        }

        public void Render(RunBattleScreenState state)
        {
            EnsureReferences();
            _enemyFormationView?.Render(state.EnemySlots);
        }

        public void ApplyEnemyFormationLayout(IReadOnlyList<int> occupiedSlotIndices)
        {
            EnsureReferences();
            _enemyFormationView?.ApplyFormationLayout(occupiedSlotIndices);
        }

        public void SetEnemyCombatVisualPrefab(int formationSlot, GameObject combatVisualPrefab)
        {
            EnsureReferences();
            _enemyFormationView?.SetCombatVisualPrefab(formationSlot, combatVisualPrefab);
        }

        public void ClearEnemyCombatVisualPrefabs()
        {
            EnsureReferences();
            _enemyFormationView?.ClearCombatVisualPrefabs();
        }

        public void SetEnemyPortraitSprite(int formationSlot, Sprite portraitSprite)
        {
            EnsureReferences();
            _enemyFormationView?.SetPortraitSprite(formationSlot, portraitSprite);
        }

        public void ClearEnemyPortraitSprites()
        {
            EnsureReferences();
            _enemyFormationView?.ClearPortraitSprites();
        }

        public Sprite GetPrimaryEnemyPortrait()
        {
            EnsureReferences();
            return _enemyFormationView != null
                ? _enemyFormationView.GetPrimaryPortrait()
                : null;
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

        public void SetEnemyDamageAnchor(
            CombatParticipantId participantId,
            RectTransform anchor)
        {
            EnsureReferences();
            _enemyFormationView?.SetEnemyDamageAnchor(participantId, anchor);
        }

        public RectTransform ResolveEnemyDamageAnchor(CombatParticipantId participantId)
        {
            EnsureReferences();
            return _enemyFormationView != null ? _enemyFormationView.ResolveDamageAnchor(participantId) : null;
        }

        public UniTask PlayEnemyCombatVisualActionUntilEffectPointAsync(
            CombatParticipantId participantId,
            string actionName,
            CancellationToken cancellationToken)
        {
            EnsureReferences();
            return _enemyFormationView != null
                ? _enemyFormationView.PlayCombatVisualActionUntilEffectPointAsync(
                    participantId,
                    actionName,
                    cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask WaitEnemyCombatVisualActionCompletedAsync(
            CombatParticipantId participantId,
            CancellationToken cancellationToken)
        {
            EnsureReferences();
            return _enemyFormationView != null
                ? _enemyFormationView.WaitCombatVisualActionCompletedAsync(
                    participantId,
                    cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask PlayEnemyDeathAsync(
            CombatParticipantId participantId,
            CancellationToken cancellationToken)
        {
            EnsureReferences();
            return _enemyFormationView != null
                ? _enemyFormationView.PlayEnemyDeathAsync(participantId, cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask ShowCombatDamageVFXAsync(
            CombatDamageVFXRequest request,
            CancellationToken cancellationToken)
        {
            EnsureReferences();
            return _enemyFormationView != null
                ? _enemyFormationView.ShowCombatDamageVFXAsync(request, cancellationToken)
                : UniTask.CompletedTask;
        }

        public ShieldGaugeView ResolveEnemyShieldGauge(CombatParticipantId participantId)
        {
            EnsureReferences();
            return _enemyFormationView != null ? _enemyFormationView.ResolveShieldGauge(participantId) : null;
        }

        public UniTask WaitEnemyHpFillAsync(
            CombatParticipantId participantId,
            CancellationToken cancellationToken)
        {
            EnsureReferences();
            return _enemyFormationView != null
                ? _enemyFormationView.WaitHpFillAsync(participantId, cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask AddEnemyStatusAsync(
            CombatParticipantId participantId,
            StatusEffectViewData status,
            CancellationToken cancellationToken)
        {
            EnsureReferences();
            return _enemyFormationView.AddStatusAsync(participantId, status, cancellationToken);
        }

        public UniTask UpdateEnemyStatusValueAsync(
            CombatParticipantId participantId,
            StatusEffectViewData status,
            CancellationToken cancellationToken)
        {
            EnsureReferences();
            return _enemyFormationView.UpdateStatusValueAsync(participantId, status, cancellationToken);
        }

        public UniTask PlayEnemyStatusActivationAsync(
            CombatParticipantId participantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            EnsureReferences();
            return _enemyFormationView.PlayStatusActivationAsync(
                participantId,
                kind,
                cancellationToken);
        }

        public UniTask PlayEnemyStatusModifierActivationAsync(
            CombatParticipantId ownerParticipantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            EnsureReferences();
            return _enemyFormationView.PlayStatusModifierActivationAsync(
                ownerParticipantId,
                kind,
                cancellationToken);
        }

        public UniTask RemoveEnemyStatusAsync(
            CombatParticipantId participantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            EnsureReferences();
            return _enemyFormationView.RemoveStatusAsync(participantId, kind, cancellationToken);
        }

        private string BuildMissingReferenceSummary()
        {
            var missing = new System.Collections.Generic.List<string>();
            if (_battleShakeRoot == null)
                missing.Add("Battle Shake Root");
            if (_enemyFormationView == null)
            {
                missing.Add("Enemy Formation View");
            }
            else if (_enemyFormationView.SlotCount == 0)
            {
                missing.Add("Enemy Formation View / Formation Slot Views");
            }

            return missing.Count > 0 ? string.Join(", ", missing) : "None";
        }
    }
}
