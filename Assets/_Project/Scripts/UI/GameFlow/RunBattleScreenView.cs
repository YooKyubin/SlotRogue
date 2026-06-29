using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotRogue.Core.Combat;
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
        [SerializeField] private RunBattlePlayerHudView _playerHudView;
        [SerializeField] private RunBattleSlotBoardView _slotBoardView;
        [SerializeField] private RunBattleActionView _actionView;
        [SerializeField] private RunBattlePresentationOverlayView _presentationOverlayView;
        [SerializeField] private RunBattleWorldView _worldView;

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
            // 씬 전체 탐색(FindInSceneRoot)으로 해결한다. 자식 한정 GetComponentInChildren는
            // 계층 재배치 시 깨지므로, 인스펙터 미할당이어도 위치와 무관하게 찾도록 한다.
            // (각 뷰는 전투 화면당 하나뿐이라 씬 전체 탐색이 안전하다.)
            _playerHudView ??= SceneComponentResolver.FindInSceneRoot<RunBattlePlayerHudView>(transform);
            _slotBoardView ??= SceneComponentResolver.FindInSceneRoot<RunBattleSlotBoardView>(transform);
            _actionView ??= SceneComponentResolver.FindInSceneRoot<RunBattleActionView>(transform);
            _presentationOverlayView ??= SceneComponentResolver.FindInSceneRoot<RunBattlePresentationOverlayView>(transform);
            _worldView ??= SceneComponentResolver.FindInSceneRoot<RunBattleWorldView>(transform);
            _worldView?.EnsureReferences();

            bool complete = _playerHudView != null &&
                _slotBoardView != null &&
                _actionView != null &&
                _presentationOverlayView != null &&
                _worldView != null;

            if (!complete)
            {
                // 어떤 하위 뷰 컴포넌트가 씬에 없는지 정확히 알려, 인스펙터/MCP 없이도 진단 가능하게 한다.
                var missing = new System.Text.StringBuilder();
                if (_playerHudView == null) missing.Append("RunBattlePlayerHudView, ");
                if (_slotBoardView == null) missing.Append("RunBattleSlotBoardView, ");
                if (_actionView == null) missing.Append("RunBattleActionView, ");
                if (_presentationOverlayView == null) missing.Append("RunBattlePresentationOverlayView, ");
                if (_worldView == null) missing.Append("RunBattleWorldView, ");
                Debug.LogError(
                    $"[RunBattleScreenView] 씬에서 못 찾은 하위 뷰: {missing.ToString().TrimEnd(',', ' ')}. " +
                    "해당 컴포넌트가 씬 어딘가의 오브젝트에 붙어 있는지 확인하세요.");
            }

            return complete;
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

        public UniTask PlayEnemyDeathAsync(
            CombatParticipantId participantId,
            CancellationToken cancellationToken)
        {
            return _worldView != null
                ? _worldView.PlayEnemyDeathAsync(participantId, cancellationToken)
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
