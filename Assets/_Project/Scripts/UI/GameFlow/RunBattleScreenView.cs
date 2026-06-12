using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
using SlotRogue.UI.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace SlotRogue.UI.GameFlow
{
    public sealed class RunBattleScreenView : MonoBehaviour, ICombatDamageAnchorRegistry, ICombatShieldGaugeRegistry
    {
        [SerializeField] private RunBattlePlayerHudView _playerHudView;
        [SerializeField] private RunBattleStatusView _statusView;
        [SerializeField] private RunBattleSlotBoardView _slotBoardView;
        [SerializeField] private RunBattleActionView _actionView;
        [SerializeField] private RunBattlePresentationOverlayView _presentationOverlayView;
        [SerializeField] private RunBattleWorldView _worldView;

        public event Action SpinRequested;

        public Text StatusText => _statusView != null ? _statusView.StatusText : null;

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
            RunBattleStatusView statusView,
            RunBattleSlotBoardView slotBoardView,
            RunBattleActionView actionView,
            RunBattlePresentationOverlayView presentationOverlayView,
            RunBattleWorldView worldView)
        {
            _playerHudView = playerHudView;
            _statusView = statusView;
            _slotBoardView = slotBoardView;
            _actionView = actionView;
            _presentationOverlayView = presentationOverlayView;
            _worldView = worldView;
        }

        public bool EnsureReferences()
        {
            _playerHudView ??= GetComponentInChildren<RunBattlePlayerHudView>(true);
            _statusView ??= GetComponentInChildren<RunBattleStatusView>(true);
            _slotBoardView ??= GetComponentInChildren<RunBattleSlotBoardView>(true);
            _actionView ??= GetComponentInChildren<RunBattleActionView>(true);
            _presentationOverlayView ??= GetComponentInChildren<RunBattlePresentationOverlayView>(true);
            _worldView ??= SceneComponentResolver.FindInSceneRoot<RunBattleWorldView>(transform);
            _worldView?.EnsureReferences();

            return _playerHudView != null &&
                _statusView != null &&
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
            _statusView?.Render(state);
            _slotBoardView?.Render(state);
            _actionView?.Render(state);
            _worldView?.Render(state);
        }

        public void SetEnemyPortrait(int slotIndex, Sprite portrait)
        {
            _worldView?.SetEnemyPortrait(slotIndex, portrait);
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
