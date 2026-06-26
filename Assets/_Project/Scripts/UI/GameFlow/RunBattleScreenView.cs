using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using SlotRogue.Core.Tooling;
using SlotRogue.UI.Combat.Presentation;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleScreenView :
        MonoBehaviour,
        ICombatDamageAnchorRegistry,
        ICombatShieldGaugeRegistry,
        ICombatHealthBarPresentationTarget,
        IEnemyCombatVisualPresentationTarget,
        ICombatStatusPresentationCommands
    {
        [SerializeField, AutoWire("10_BattleView", AutoWireSearchScope.Children)]
        private RunBattlePlayerHudView _playerHudView;
        [SerializeField, AutoWire("Slot Machine Panel", AutoWireSearchScope.Children)]
        private RunBattleSlotBoardView _slotBoardView;
        [SerializeField, AutoWire("10_BattleView", AutoWireSearchScope.Children)]
        private RunBattleActionView _actionView;
        [SerializeField, AutoWire("Presentation Overlay", AutoWireSearchScope.Children)]
        private RunBattlePresentationOverlayView _presentationOverlayView;
        [SerializeField, AutoWire("10_World")]
        private RunBattleWorldView _worldView;

        public event Action SpinRequested;

        public Transform FloatingTextRoot =>
            _presentationOverlayView != null ? _presentationOverlayView.FloatingTextRoot : null;

        public RectTransform PlayerDamageAnchor =>
            _presentationOverlayView != null ? _presentationOverlayView.PlayerDamageAnchor : null;

        public int EnemySlotCount =>
            _worldView != null && _worldView.EnsureReferences() && _worldView.EnemyFormationView != null
                ? _worldView.EnemyFormationView.SlotCount
                : 0;

        private void Awake()
        {
            EnsureReferences();
            SubscribeActions();
        }

        private void OnDestroy()
        {
            UnsubscribeActions();
        }

        public void Bind(
            RunBattlePlayerHudView playerHudView,
            RunBattleSlotBoardView slotBoardView,
            RunBattleActionView actionView,
            RunBattlePresentationOverlayView presentationOverlayView,
            RunBattleWorldView worldView)
        {
            _playerHudView = playerHudView;
            _slotBoardView = slotBoardView;
            _actionView = actionView;
            _presentationOverlayView = presentationOverlayView;
            _worldView = worldView;
        }

        public bool EnsureReferences()
        {
            _playerHudView ??= GetComponentInChildren<RunBattlePlayerHudView>(true);
            _slotBoardView ??= GetComponentInChildren<RunBattleSlotBoardView>(true);
            _actionView ??= GetComponentInChildren<RunBattleActionView>(true);
            _presentationOverlayView ??= GetComponentInChildren<RunBattlePresentationOverlayView>(true);
            _worldView ??= SceneComponentResolver.FindInSceneRoot<RunBattleWorldView>(transform);
            _worldView?.EnsureReferences();

            return _playerHudView != null &&
                _slotBoardView != null &&
                _actionView != null &&
                _presentationOverlayView != null &&
                _worldView != null;
        }

        public bool HasRequiredControls()
        {
            EnsureReferences();
            return _actionView != null && _actionView.HasRequiredControls;
        }

        public void Render(RunBattleScreenState state)
        {
            if (state == null)
            {
                return;
            }

            _playerHudView?.Render(state);
            _slotBoardView?.Render(state);
            _actionView?.Render(state);
            _worldView?.Render(state);
        }

        public void SetEnemyCombatVisualPrefab(int formationSlot, GameObject combatVisualPrefab)
        {
            _worldView?.SetEnemyCombatVisualPrefab(formationSlot, combatVisualPrefab);
        }

        public void ClearEnemyCombatVisualPrefabs()
        {
            _worldView?.ClearEnemyCombatVisualPrefabs();
        }

        public void SetEnemyPortraitSprite(int formationSlot, Sprite portraitSprite)
        {
            _worldView?.SetEnemyPortraitSprite(formationSlot, portraitSprite);
        }

        public void ClearEnemyPortraitSprites()
        {
            _worldView?.ClearEnemyPortraitSprites();
        }

        public Sprite GetPrimaryEnemyPortrait()
        {
            return _worldView != null ? _worldView.GetPrimaryEnemyPortrait() : null;
        }

        public void SetEnemySlotClickHandler(int slotIndex, Action action)
        {
            _worldView?.SetEnemySlotClickHandler(slotIndex, action);
        }

        public RectTransform GetEnemyDamageAnchor(int slotIndex)
        {
            return _worldView != null ? _worldView.GetEnemyDamageAnchor(slotIndex) : null;
        }

        public void SetEnemyDamageAnchor(CombatParticipantId participantId, RectTransform anchor)
        {
            _worldView?.SetEnemyDamageAnchor(participantId, anchor);
        }

        public RectTransform ResolveDamageAnchor(CombatParticipantId participantId, bool isPlayerTarget)
        {
            if (isPlayerTarget)
            {
                return PlayerDamageAnchor;
            }

            RectTransform enemyAnchor = _worldView != null
                ? _worldView.ResolveEnemyDamageAnchor(participantId)
                : null;
            return enemyAnchor;
        }

        public UniTask PlayEnemyCombatVisualActionUntilEffectPointAsync(
            CombatParticipantId participantId,
            string actionName,
            CancellationToken cancellationToken)
        {
            return _worldView != null
                ? _worldView.PlayEnemyCombatVisualActionUntilEffectPointAsync(
                    participantId,
                    actionName,
                    cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask WaitEnemyCombatVisualActionCompletedAsync(
            CombatParticipantId participantId,
            CancellationToken cancellationToken)
        {
            return _worldView != null
                ? _worldView.WaitEnemyCombatVisualActionCompletedAsync(
                    participantId,
                    cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask AddEnemyStatusAsync(
            CombatParticipantId participantId,
            StatusEffectViewData status,
            CancellationToken cancellationToken)
        {
            return _worldView.AddEnemyStatusAsync(participantId, status, cancellationToken);
        }

        public UniTask UpdateEnemyStatusValueAsync(
            CombatParticipantId participantId,
            StatusEffectViewData status,
            CancellationToken cancellationToken)
        {
            return _worldView.UpdateEnemyStatusValueAsync(participantId, status, cancellationToken);
        }

        public UniTask PlayEnemyStatusActivationAsync(
            CombatParticipantId participantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            return _worldView.PlayEnemyStatusActivationAsync(
                participantId,
                kind,
                cancellationToken);
        }

        public UniTask PlayEnemyStatusModifierActivationAsync(
            CombatParticipantId ownerParticipantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            return _worldView.PlayEnemyStatusModifierActivationAsync(
                ownerParticipantId,
                kind,
                cancellationToken);
        }

        public UniTask RemoveEnemyStatusAsync(
            CombatParticipantId participantId,
            StatusEffectKind kind,
            CancellationToken cancellationToken)
        {
            return _worldView.RemoveEnemyStatusAsync(participantId, kind, cancellationToken);
        }

        public UniTask ShowShieldGainAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken)
        {
            ShieldGaugeView shieldGauge = ResolveShieldGauge(request);
            return shieldGauge != null
                ? shieldGauge.PlayGainAsync(request.Amount, cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask ShowShieldHitAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken)
        {
            ShieldGaugeView shieldGauge = ResolveShieldGauge(request);
            return shieldGauge != null
                ? shieldGauge.PlayHitAsync(request.Amount, cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask ShowShieldBreakAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken)
        {
            ShieldGaugeView shieldGauge = ResolveShieldGauge(request);
            return shieldGauge != null
                ? shieldGauge.PlayBreakAsync(cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask ShowShieldExpireAsync(
            ShieldPresentationRequest request,
            CancellationToken cancellationToken)
        {
            ShieldGaugeView shieldGauge = ResolveShieldGauge(request);
            return shieldGauge != null
                ? shieldGauge.PlayExpireAsync(cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask WaitHealthBarAsync(
            CombatParticipantId participantId,
            bool isPlayerTarget,
            CancellationToken cancellationToken)
        {
            if (isPlayerTarget)
            {
                return _playerHudView != null
                    ? _playerHudView.WaitHpFillAsync(cancellationToken)
                    : UniTask.CompletedTask;
            }

            return _worldView != null
                ? _worldView.WaitEnemyHpFillAsync(participantId, cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask PlayPlayerHitFeedbackAsync(CancellationToken cancellationToken)
        {
            return _playerHudView != null
                ? _playerHudView.PlayHitFeedbackAsync(cancellationToken)
                : UniTask.CompletedTask;
        }

        private ShieldGaugeView ResolveShieldGauge(ShieldPresentationRequest request)
        {
            if (request.IsPlayerTarget)
            {
                return null;
            }

            return _worldView != null
                ? _worldView.ResolveEnemyShieldGauge(request.TargetParticipantId)
                : null;
        }

        private void SubscribeActions()
        {
            if (_actionView == null)
            {
                return;
            }

            _actionView.SpinRequested += HandleSpinRequested;
        }

        private void UnsubscribeActions()
        {
            if (_actionView == null)
            {
                return;
            }

            _actionView.SpinRequested -= HandleSpinRequested;
        }

        private void HandleSpinRequested()
        {
            SpinRequested?.Invoke();
        }
    }
}
