using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using SlotRogue.Core.Combat;
using SlotRogue.Relics.Pool;
using SlotRogue.UI.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private RunBattleSpinButtonView _actionView;
        [SerializeField] private RunBattleShopView _shopView;
        [SerializeField] private Button _shopButton;

        [Header("Tutorial Targets")]
        [SerializeField] private RunBattleTutorialTargets _tutorialTargets = new();

        [Header("Currency HUD")]
        [SerializeField] private RunBattleCurrencyView _currencyView;

        [Header("Shop Shutter")]
        [SerializeField] private RectTransform[] _slotScreenShutters = Array.Empty<RectTransform>();
        [SerializeField] private float _shopFlipDuration = 0.36f;
        [SerializeField] private float _shopShutterFoldedVisibleHeight = 0f;
        [SerializeField] private float _shopShutterRevealDelay = 0.08f;

        [SerializeField] private RunBattlePresentationOverlayView _presentationOverlayView;
        [SerializeField] private RunBattleWorldView _worldView;

        private Button _subscribedShopButton;
        private Sequence _shopFlipSequence;
        private readonly Dictionary<RectTransform, SlotShutterRestPose> _shopShutterRestPoses = new();
        private readonly HashSet<RectTransform> _reportedMissingShutterMasks = new();
        private bool _hasRenderedShopBoardVisibility;
        private bool _shopBoardTransitionActive;
        private bool _shopBoardVisible;

        private readonly struct SlotShutterRestPose
        {
            public SlotShutterRestPose(Vector2 anchoredPosition, Vector3 localScale)
            {
                AnchoredPosition = anchoredPosition;
                LocalScale = localScale;
            }

            public Vector2 AnchoredPosition { get; }

            public Vector3 LocalScale { get; }
        }

        public event Action SpinRequested;

        public event Action<int> SlotCellSelected;

        public event Action<int, int> SlotCellsDragged;

        public event Action<int> RelicShopPurchaseRequested;

        public event Action RelicShopRerollRequested;

        public event Action RelicShopToggleRequested;

        public event Action<RunBattleRelicShopOfferState> ShopOfferSelected;

        public RunBattleTutorialTargets TutorialTargets =>
            _tutorialTargets ?? RunBattleTutorialTargets.Empty;

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
            _shopFlipSequence?.Kill();
            _shopFlipSequence = null;
            _shopBoardTransitionActive = false;
            UnsubscribeActions();
        }

        public void Bind(
            RunBattlePlayerHudView playerHudView,
            RunBattleSlotBoardView slotBoardView,
            RunBattleSpinButtonView actionView,
            RunBattlePresentationOverlayView presentationOverlayView,
            RunBattleWorldView worldView,
            RunBattleShopView shopView = null,
            RunBattleCurrencyView currencyView = null)
        {
            UnsubscribeActions();
            _playerHudView = playerHudView;
            _slotBoardView = slotBoardView;
            _actionView = actionView;
            _presentationOverlayView = presentationOverlayView;
            _worldView = worldView;
            if (shopView != null)
            {
                _shopView = shopView;
            }

            if (currencyView != null)
            {
                _currencyView = currencyView;
            }

            EnsureReferences();
            SubscribeActions();
        }

        public bool EnsureReferences()
        {
            _worldView?.EnsureReferences();

            // 슬롯 셔터 연출·보유유물 아이콘 줄은 선택 요소다. 미배선이어도 각자 null-guard되므로
            // 필수 참조에서 제외한다(없으면 셔터 연출/보유유물 표시만 생략).
            bool complete = _playerHudView != null &&
                _slotBoardView != null &&
                _actionView != null &&
                _shopView != null &&
                _shopButton != null &&
                _currencyView != null &&
                _presentationOverlayView != null &&
                _worldView != null;

            if (!complete)
            {
                string missing = BuildMissingReferenceSummary();
                Debug.LogError(
                    "[RunBattleScreenView] Battle screen references must be wired in the inspector. " +
                    $"Missing: {missing}");
            }

            return complete;
        }

        public bool HasRequiredControls()
        {
            EnsureReferences();
            return _actionView != null && _actionView.HasRequiredControls;
        }

        public void SetSlotHighlightSymbolSprites(Sprite[] sprites)
        {
            EnsureReferences();
            _slotBoardView?.SetHighlightSymbolSprites(sprites);
        }

        public void Render(RunBattleScreenState state)
        {
            if (state == null)
            {
                return;
            }

            if (_shopView == null || _shopButton == null)
            {
                EnsureReferences();
                SubscribeShopButton();
            }

            bool shopVisible = state.RelicShop?.Visible == true;
            _playerHudView?.Render(state);
            _slotBoardView?.Render(state);
            _actionView?.Render(state);
            RenderShopBoardTransition(shopVisible);
            RenderShopViewWhenReady(state, shopVisible);
            _currencyView?.Render(state.RunCoins, shopVisible);
            if (_shopButton != null)
            {
                _shopButton.gameObject.SetActive(true);
                _shopButton.interactable =
                    _shopView != null &&
                    _shopView.EnsureReferences() &&
                    state.RelicShop?.CanOpenShop == true;
            }

            _worldView?.Render(state);
        }

        private void RenderShopViewWhenReady(RunBattleScreenState state, bool shopVisible)
        {
            if (!IsShopBoardTransitionPlaying())
            {
                _shopView?.Render(state);
                return;
            }

            if (shopVisible)
            {
                _shopView?.Render(state);
            }
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
                ? shieldGauge.PlayBreakAsync(request.Amount, cancellationToken)
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

        private void RenderShopBoardTransition(bool shopVisible)
        {
            if (_slotBoardView == null || _shopView == null)
            {
                return;
            }

            RectTransform shopPanel = _shopView.PanelTransform;
            if (shopPanel == null)
            {
                return;
            }

            List<RectTransform> slotShutters = BuildSlotScreenShutterList();
            if (slotShutters.Count == 0)
            {
                return;
            }

            if (!_hasRenderedShopBoardVisibility)
            {
                _hasRenderedShopBoardVisibility = true;
                _shopBoardVisible = shopVisible;
                SetShopBoardFacesImmediate(slotShutters, shopPanel, shopVisible);
                return;
            }

            if (_shopBoardVisible == shopVisible)
            {
                if (IsShopBoardTransitionPlaying())
                {
                    MaintainShopBoardShutterDuringAnimation(shopPanel, shopVisible);
                }
                else
                {
                    SetShopBoardFacesImmediate(slotShutters, shopPanel, shopVisible);
                }

                return;
            }

            _shopBoardVisible = shopVisible;
            PlayShopBoardShutter(slotShutters, shopPanel, shopVisible);
        }

        private void PlayShopBoardShutter(
            List<RectTransform> slotShutters,
            RectTransform shopFace,
            bool shopVisible)
        {
            _shopBoardTransitionActive = false;
            _shopFlipSequence?.Kill();
            _shopBoardTransitionActive = true;

            float shutterDuration = Mathf.Max(0.08f, _shopFlipDuration);

            _shopFlipSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true);

            shopFace.gameObject.SetActive(true);
            shopFace.localRotation = Quaternion.identity;

            if (shopVisible)
            {
                SetSlotShuttersActive(slotShutters, true);
                SetSlotShutterSlideState(slotShutters, hidden: false);
                InsertSlotShutterSlideTweens(slotShutters, hidden: true, shutterDuration, Ease.InCubic);
                AppendShopShutterRevealDelay();
                _shopFlipSequence.OnComplete(() =>
                {
                    SetSlotShuttersActive(slotShutters, false);
                    SetSlotShutterSlideState(slotShutters, hidden: true);
                    if (shopFace != null)
                    {
                        shopFace.gameObject.SetActive(true);
                        shopFace.localRotation = Quaternion.identity;
                    }

                    _shopBoardTransitionActive = false;
                });
                return;
            }

            SetSlotShuttersActive(slotShutters, true);
            SetSlotShutterSlideState(slotShutters, hidden: true);
            InsertSlotShutterSlideTweens(slotShutters, hidden: false, shutterDuration, Ease.OutCubic);
            _shopFlipSequence.OnComplete(() =>
            {
                if (shopFace != null)
                {
                    shopFace.gameObject.SetActive(false);
                    shopFace.localRotation = Quaternion.identity;
                }

                SetSlotShutterSlideState(slotShutters, hidden: false);
                SetSlotShuttersActive(slotShutters, true);
                _shopBoardTransitionActive = false;
            });
        }

        private void AppendShopShutterRevealDelay()
        {
            float revealDelay = Mathf.Max(0f, _shopShutterRevealDelay);
            if (revealDelay > 0f)
            {
                _shopFlipSequence.AppendInterval(revealDelay);
            }
        }

        private void InsertSlotShutterSlideTweens(
            List<RectTransform> slotShutters,
            bool hidden,
            float duration,
            Ease ease)
        {
            for (int index = 0; index < slotShutters.Count; index++)
            {
                RectTransform shutter = slotShutters[index];
                if (shutter == null)
                {
                    continue;
                }

                EnsureSlotShutterClipMask(shutter);
                List<RectTransform> movingParts = ResolveSlotShutterMovingParts(shutter);
                for (int partIndex = 0; partIndex < movingParts.Count; partIndex++)
                {
                    RectTransform movingPart = movingParts[partIndex];
                    if (movingPart == null)
                    {
                        continue;
                    }

                    SlotShutterRestPose restPose = GetSlotShutterRestPose(movingPart);
                    Vector2 targetPosition = hidden
                        ? GetSlotShutterHiddenAnchoredPosition(shutter, movingPart, restPose)
                        : restPose.AnchoredPosition;
                    _shopFlipSequence.Insert(
                        0f,
                        CreateSlotShutterSlideTween(movingPart, targetPosition, duration, ease));
                }
            }
        }

        private void SetShopBoardFacesImmediate(
            List<RectTransform> reelShutters,
            RectTransform shopFace,
            bool shopVisible)
        {
            _shopBoardTransitionActive = false;
            SetSlotShutterSlideState(reelShutters, hidden: shopVisible);
            SetSlotShuttersActive(reelShutters, !shopVisible);

            if (shopFace != null)
            {
                shopFace.gameObject.SetActive(shopVisible);
                shopFace.localRotation = Quaternion.identity;
            }
        }

        private void MaintainShopBoardShutterDuringAnimation(RectTransform shopFace, bool shopVisible)
        {
            if (shopFace == null)
            {
                return;
            }

            shopFace.gameObject.SetActive(true);
            shopFace.localRotation = Quaternion.identity;
        }

        private bool IsShopBoardTransitionPlaying()
        {
            return _shopBoardTransitionActive &&
                _shopFlipSequence != null &&
                _shopFlipSequence.IsActive();
        }

        private void SetSlotShutterSlideState(List<RectTransform> slotShutters, bool hidden)
        {
            for (int index = 0; index < slotShutters.Count; index++)
            {
                RectTransform shutter = slotShutters[index];
                if (shutter == null)
                {
                    continue;
                }

                shutter.localRotation = Quaternion.identity;
                EnsureSlotShutterClipMask(shutter);
                List<RectTransform> movingParts = ResolveSlotShutterMovingParts(shutter);
                for (int partIndex = 0; partIndex < movingParts.Count; partIndex++)
                {
                    RectTransform movingPart = movingParts[partIndex];
                    if (movingPart == null)
                    {
                        continue;
                    }

                    SlotShutterRestPose restPose = GetSlotShutterRestPose(movingPart);
                    movingPart.localRotation = Quaternion.identity;
                    movingPart.localScale = restPose.LocalScale;
                    movingPart.anchoredPosition = hidden
                        ? GetSlotShutterHiddenAnchoredPosition(shutter, movingPart, restPose)
                        : restPose.AnchoredPosition;
                }
            }
        }

        private static void SetSlotShuttersActive(List<RectTransform> slotShutters, bool active)
        {
            for (int index = 0; index < slotShutters.Count; index++)
            {
                RectTransform shutter = slotShutters[index];
                if (shutter != null)
                {
                    shutter.gameObject.SetActive(active);
                }
            }
        }

        private SlotShutterRestPose GetSlotShutterRestPose(RectTransform shutter)
        {
            if (!_shopShutterRestPoses.TryGetValue(shutter, out SlotShutterRestPose restPose))
            {
                restPose = new SlotShutterRestPose(shutter.anchoredPosition, shutter.localScale);
                _shopShutterRestPoses[shutter] = restPose;
            }

            return restPose;
        }

        private void EnsureSlotShutterClipMask(RectTransform shutter)
        {
            if (shutter == null || shutter.GetComponent<RectMask2D>() != null)
            {
                return;
            }

            if (_reportedMissingShutterMasks.Add(shutter))
            {
                Debug.LogWarning(
                    "[RunBattleScreenView] Slot screen shutter is missing RectMask2D. Assign the mask on the referenced shutter RectTransform.",
                    shutter);
            }
        }

        private List<RectTransform> ResolveSlotShutterMovingParts(RectTransform shutter)
        {
            var movingParts = new List<RectTransform>();
            if (shutter == null)
            {
                return movingParts;
            }

            for (int index = 0; index < shutter.childCount; index++)
            {
                if (shutter.GetChild(index) is RectTransform child)
                {
                    movingParts.Add(child);
                }
            }

            if (movingParts.Count == 0)
            {
                movingParts.Add(shutter);
            }

            return movingParts;
        }

        private Vector2 GetSlotShutterHiddenAnchoredPosition(
            RectTransform shutter,
            RectTransform movingPart,
            SlotShutterRestPose restPose,
            bool includeVisibleHeight = true)
        {
            float visibleHeight = includeVisibleHeight
                ? Mathf.Max(0f, _shopShutterFoldedVisibleHeight)
                : 0f;
            float slideDistance = Mathf.Max(
                    Mathf.Abs(shutter.rect.height),
                    Mathf.Abs(movingPart.rect.height))
                + 2f -
                visibleHeight;
            slideDistance = Mathf.Max(0f, slideDistance);
            return new Vector2(
                restPose.AnchoredPosition.x,
                restPose.AnchoredPosition.y + slideDistance);
        }

        private static Tween CreateSlotShutterSlideTween(
            RectTransform shutter,
            Vector2 targetPosition,
            float duration,
            Ease ease)
        {
            return DOTween.To(
                    () => shutter != null ? shutter.anchoredPosition : targetPosition,
                    value =>
                    {
                        if (shutter != null)
                        {
                            shutter.anchoredPosition = value;
                        }
                    },
                    targetPosition,
                    duration)
                .SetEase(ease)
                .SetTarget(shutter);
        }

        private List<RectTransform> BuildSlotScreenShutterList()
        {
            var slotShutters = new List<RectTransform>();
            if (_slotScreenShutters != null)
            {
                for (int index = 0; index < _slotScreenShutters.Length; index++)
                {
                    AddSlotScreenShutter(slotShutters, _slotScreenShutters[index]);
                }
            }

            return slotShutters;
        }

        private static void AddSlotScreenShutter(
            List<RectTransform> slotShutters,
            RectTransform shutter)
        {
            if (slotShutters == null || shutter == null || slotShutters.Contains(shutter))
            {
                return;
            }

            slotShutters.Add(shutter);
        }

        private string BuildMissingReferenceSummary()
        {
            var missing = new List<string>();
            if (_playerHudView == null) missing.Add("Player HUD View");
            if (_slotBoardView == null) missing.Add("Slot Board View");
            if (_actionView == null) missing.Add("Action View");
            if (_shopView == null) missing.Add("Shop View");
            if (_shopButton == null) missing.Add("Shop Button");
            if (_currencyView == null) missing.Add("Currency View");
            if (_presentationOverlayView == null) missing.Add("Presentation Overlay View");
            if (_worldView == null) missing.Add("World View");
            return missing.Count > 0 ? string.Join(", ", missing) : "None";
        }

        private void SubscribeActions()
        {
            if (_actionView != null)
            {
                _actionView.SpinRequested += HandleSpinRequested;
            }

            if (_slotBoardView != null)
            {
                _slotBoardView.SlotCellSelected += HandleSlotCellSelected;
                _slotBoardView.SlotCellsDragged += HandleSlotCellsDragged;
            }

            if (_shopView != null)
            {
                _shopView.PurchaseRequested += HandleRelicShopPurchaseRequested;
                _shopView.RerollRequested += HandleRelicShopRerollRequested;
                _shopView.OfferSelected += HandleShopOfferSelected;
            }

            SubscribeShopButton();
        }

        private void UnsubscribeActions()
        {
            if (_actionView != null)
            {
                _actionView.SpinRequested -= HandleSpinRequested;
            }

            if (_slotBoardView != null)
            {
                _slotBoardView.SlotCellSelected -= HandleSlotCellSelected;
                _slotBoardView.SlotCellsDragged -= HandleSlotCellsDragged;
            }

            if (_shopView != null)
            {
                _shopView.PurchaseRequested -= HandleRelicShopPurchaseRequested;
                _shopView.RerollRequested -= HandleRelicShopRerollRequested;
                _shopView.OfferSelected -= HandleShopOfferSelected;
            }

            UnsubscribeShopButton();
        }

        private void HandleSpinRequested()
        {
            SpinRequested?.Invoke();
        }

        private void HandleSlotCellSelected(int cellIndex)
        {
            SlotCellSelected?.Invoke(cellIndex);
        }

        private void HandleSlotCellsDragged(int firstIndex, int secondIndex)
        {
            SlotCellsDragged?.Invoke(firstIndex, secondIndex);
        }

        private void HandleRelicShopPurchaseRequested(int offerIndex)
        {
            RelicShopPurchaseRequested?.Invoke(offerIndex);
        }

        private void HandleRelicShopRerollRequested()
        {
            RelicShopRerollRequested?.Invoke();
        }

        private void HandleRelicShopToggleRequested()
        {
            RelicShopToggleRequested?.Invoke();
        }

        private void HandleShopOfferSelected(RunBattleRelicShopOfferState offer)
        {
            ShopOfferSelected?.Invoke(offer);
        }

        private void SubscribeShopButton()
        {
            if (_shopButton == null)
            {
                return;
            }

            if (_subscribedShopButton != null && _subscribedShopButton != _shopButton)
            {
                UnsubscribeShopButton();
            }

            _shopButton.gameObject.SetActive(true);
            _shopButton.onClick.RemoveListener(HandleRelicShopToggleRequested);
            _shopButton.onClick.AddListener(HandleRelicShopToggleRequested);
            _subscribedShopButton = _shopButton;
        }

        private void UnsubscribeShopButton()
        {
            if (_subscribedShopButton == null)
            {
                return;
            }

            _subscribedShopButton.onClick.RemoveListener(HandleRelicShopToggleRequested);
            _subscribedShopButton = null;
        }

    }
}
