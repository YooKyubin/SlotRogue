using System;
using System.Collections;
using System.Collections.Generic;
using SlotRogue.Slot.Data;
using SlotRogue.UI.GameFlow;
using SlotRogue.UI.SlotPresentation.Reel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class SlotPresentationManager : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private SlotCellSpinView _slotCellSpinView;
        [SerializeField] private SlotMachineSpinDirector _spinPresenter;
        [SerializeField] private PatternPresentationDirector _patternView;
        [SerializeField] private RelicPresentationDirector _relicView;
        [SerializeField] private RectTransform _inventoryButtonAnchor;
        [SerializeField] private FinalResultDirector _finalResultView;
        [SerializeField] private Graphic _tapSkipGraphic;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _patternClip;
        [SerializeField] private AudioClip[] _patternScaleClips;
        [SerializeField] private AudioClip _relicClip;
        [SerializeField] private AudioClip _finalClip;
        [SerializeField] private AudioSource _slotSpinAudioSource;
        [SerializeField] private AudioClip _slotSpinClip;
        [SerializeField] private bool _loopSlotSpinClip;
        [SerializeField] private AudioClip _slotReelStopClip;
        [SerializeField] private AudioClip _slotSpinCompleteClip;
        [SerializeField] private bool _skipCurrentOnTap = true;

        public event Action<SlotPresentationResult> Completed;
        public event Action SlotSpinCompleted;
        public event Action<int> SlotReelStopped;
        public event Action<SlotPatternPresentationResult> PatternStepStarted;

        public bool IsPlaying => _playRoutine != null;

        public void Bind(
            PatternPresentationDirector patternView,
            RelicPresentationDirector relicView,
            FinalResultDirector finalResultView,
            AudioSource audioSource,
            AudioClip[] patternScaleClips = null,
            AudioClip relicClip = null,
            AudioClip finalClip = null,
            Graphic tapSkipGraphic = null,
            SlotCellSpinView slotCellSpinView = null,
            AudioSource slotSpinAudioSource = null,
            AudioClip slotSpinClip = null,
            AudioClip slotReelStopClip = null,
            AudioClip slotSpinCompleteClip = null)
        {
            _patternView = patternView;
            _relicView = relicView;
            _finalResultView = finalResultView;
            _audioSource = audioSource;
            _patternScaleClips = patternScaleClips ?? _patternScaleClips;
            _relicClip = relicClip != null ? relicClip : _relicClip;
            _finalClip = finalClip != null ? finalClip : _finalClip;
            _tapSkipGraphic = tapSkipGraphic != null ? tapSkipGraphic : _tapSkipGraphic;
            _slotCellSpinView = slotCellSpinView != null ? slotCellSpinView : _slotCellSpinView;
            _slotSpinAudioSource = slotSpinAudioSource != null ? slotSpinAudioSource : _slotSpinAudioSource;
            _slotSpinClip = slotSpinClip != null ? slotSpinClip : _slotSpinClip;
            _slotReelStopClip = slotReelStopClip != null ? slotReelStopClip : _slotReelStopClip;
            _slotSpinCompleteClip = slotSpinCompleteClip != null
                ? slotSpinCompleteClip
                : _slotSpinCompleteClip;
            SetTapSkipEnabled(false);
        }

        public void Play(SlotPresentationResult result, Action<SlotPresentationResult> onCompleted)
        {
            PlayInternal(result, onCompleted, playSpin: true);
        }

        public void PlayResolved(SlotPresentationResult result, Action<SlotPresentationResult> onCompleted)
        {
            PlayInternal(result, onCompleted, playSpin: false);
        }

        // Plays only the reel spin animation (reels spinning and stopping one by one) and leaves
        // the final symbols on screen, without running the pattern/relic/final resolution.
        // Used so the player can spin, see the result, swap symbols, and only then resolve.
        public void PlaySpinOnly(SlotSpinResult spinResult, Action onCompleted)
        {
            if (!isActiveAndEnabled || spinResult == null)
            {
                onCompleted?.Invoke();
                return;
            }

            EnsureViews();
            CancelSwapAnimation();

            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
            }

            _playRoutine = StartCoroutine(PlaySpinOnlyRoutine(spinResult, onCompleted));
        }

        private IEnumerator PlaySpinOnlyRoutine(SlotSpinResult spinResult, Action onCompleted)
        {
            _skipRequested = false;
            _skipAllRequested = false;
            HideAllViews(includeSlotDisplay: true);
            SetTapSkipEnabled(true);

            IEnumerator spinRoutine = ResolveSpinRoutine(spinResult);
            if (spinRoutine != null)
            {
                yield return spinRoutine;
            }

            SlotSpinCompleted?.Invoke();

            _playRoutine = null;
            _skipRequested = false;
            _skipAllRequested = false;
            SetTapSkipEnabled(false);
            onCompleted?.Invoke();
        }

        private void PlayInternal(
            SlotPresentationResult result,
            Action<SlotPresentationResult> onCompleted,
            bool playSpin)
        {
            if (!isActiveAndEnabled)
            {
                onCompleted?.Invoke(result);
                Completed?.Invoke(result);
                return;
            }

            EnsureViews();
            CancelSwapAnimation();

            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
            }

            _playRoutine = StartCoroutine(PlayRoutine(result, onCompleted, playSpin));
        }

        public void ShowImmediate(SlotSpinResult result)
        {
            if (result == null)
            {
                return;
            }

            EnsureViews();
            CancelSwapAnimation();

            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            _skipRequested = false;
            _skipAllRequested = false;
            SetTapSkipEnabled(false);

            SettleBoardImmediate(result);
        }

        private void SettleBoardImmediate(SlotSpinResult result)
        {
            SlotMachineSpinDirector presenter = EnsureSpinPresenter();
            if (presenter != null)
            {
                presenter.ShowImmediate(result);
                return;
            }

            _slotCellSpinView?.StopImmediate(result);
        }

        // Slides the two swapped cell icons across each other before settling on the swapped board.
        // Uses floating "ghost" copies parented to the icon canvas so the animation renders above the
        // reel masks (real reel icons would clip when a horizontal swap crosses column windows).
        public void PlaySwap(int indexA, int indexB, SlotSpinResult settledResult, Action onCompleted = null)
        {
            if (settledResult == null)
            {
                onCompleted?.Invoke();
                return;
            }

            EnsureViews();

            if (!isActiveAndEnabled ||
                indexA == indexB ||
                !SlotSpinResult.IsValidIndex(indexA) ||
                !SlotSpinResult.IsValidIndex(indexB) ||
                !TryGetSwapIcons(out Image[] icons) ||
                icons[indexA] == null ||
                icons[indexB] == null)
            {
                ShowImmediate(settledResult);
                onCompleted?.Invoke();
                return;
            }

            CancelSwapAnimation();
            _swapOnCompleted = onCompleted;
            _swapRoutine = StartCoroutine(
                PlaySwapRoutine(icons[indexA], icons[indexB], settledResult));
        }

        private bool TryGetSwapIcons(out Image[] icons)
        {
            icons = null;

            SlotMachineSpinDirector presenter = EnsureSpinPresenter();
            if (presenter != null && presenter.TryGetVisibleCellIcons(out icons))
            {
                return icons != null;
            }

            return _slotCellSpinView != null &&
                _slotCellSpinView.TryGetReelBindings(out icons, out _, out _) &&
                icons != null;
        }

        private IEnumerator PlaySwapRoutine(
            Image iconA,
            Image iconB,
            SlotSpinResult settledResult)
        {
            RectTransform ghostA = CreateSwapGhost(iconA);
            RectTransform ghostB = CreateSwapGhost(iconB);

            HideSwapIcon(iconA);
            HideSwapIcon(iconB);

            Vector3 startA = ghostA.position;
            Vector3 startB = ghostB.position;
            Vector3 scaleA = ghostA.localScale;
            Vector3 scaleB = ghostB.localScale;

            float elapsed = 0f;
            while (elapsed < SwapAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / SwapAnimationDuration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                float pop = 1f + SwapAnimationPop * Mathf.Sin(progress * Mathf.PI);

                ghostA.position = Vector3.Lerp(startA, startB, eased);
                ghostB.position = Vector3.Lerp(startB, startA, eased);
                ghostA.localScale = scaleA * pop;
                ghostB.localScale = scaleB * pop;
                yield return null;
            }

            RestoreSwapIcons();
            DestroySwapGhosts();
            _swapRoutine = null;
            SettleBoardImmediate(settledResult);

            Action onCompleted = _swapOnCompleted;
            _swapOnCompleted = null;
            onCompleted?.Invoke();
        }

        private RectTransform CreateSwapGhost(Image source)
        {
            var ghostObject = new GameObject("SwapGhost", typeof(RectTransform), typeof(Image));
            var ghostRect = (RectTransform)ghostObject.transform;
            RectTransform sourceRect = source.rectTransform;

            // Copy the source layout, then reparent to the shared canvas keeping the world transform so
            // the ghost overlays the reel at the exact size/position of the icon it replaces.
            ghostRect.SetParent(sourceRect.parent, worldPositionStays: false);
            ghostRect.pivot = sourceRect.pivot;
            ghostRect.anchorMin = sourceRect.anchorMin;
            ghostRect.anchorMax = sourceRect.anchorMax;
            ghostRect.sizeDelta = sourceRect.sizeDelta;
            ghostRect.anchoredPosition = sourceRect.anchoredPosition;
            ghostRect.localRotation = sourceRect.localRotation;
            ghostRect.localScale = sourceRect.localScale;

            var ghostImage = ghostObject.GetComponent<Image>();
            ghostImage.sprite = source.sprite;
            ghostImage.color = source.color;
            ghostImage.material = source.material;
            ghostImage.preserveAspect = source.preserveAspect;
            ghostImage.raycastTarget = false;

            Transform canvasRoot = source.canvas != null ? source.canvas.transform : sourceRect.parent;
            ghostRect.SetParent(canvasRoot, worldPositionStays: true);
            ghostRect.SetAsLastSibling();

            _swapGhosts.Add(ghostObject);
            return ghostRect;
        }

        private void HideSwapIcon(Image icon)
        {
            if (icon == null)
            {
                return;
            }

            // Image.enabled만 끄면 자식으로 붙은 족보 하이라이트 오버레이가 남아 고스트 뒤로 비친다.
            // GameObject를 통째로 비활성화해 심볼과 하이라이트를 함께 숨긴다(비활성 GO는 재활성도 무효).
            icon.gameObject.SetActive(false);
            _swapHiddenIcons.Add(icon);
        }

        private void RestoreSwapIcons()
        {
            for (int index = 0; index < _swapHiddenIcons.Count; index++)
            {
                if (_swapHiddenIcons[index] != null)
                {
                    _swapHiddenIcons[index].gameObject.SetActive(true);
                }
            }

            _swapHiddenIcons.Clear();
        }

        private void DestroySwapGhosts()
        {
            for (int index = 0; index < _swapGhosts.Count; index++)
            {
                if (_swapGhosts[index] != null)
                {
                    Destroy(_swapGhosts[index]);
                }
            }

            _swapGhosts.Clear();
        }

        private void CancelSwapAnimation()
        {
            if (_swapRoutine != null)
            {
                StopCoroutine(_swapRoutine);
                _swapRoutine = null;
            }

            RestoreSwapIcons();
            DestroySwapGhosts();

            // 진행 중 취소돼도 대기 중인 정산 흐름이 멈추지 않도록 완료 콜백을 반드시 호출한다.
            Action onCompleted = _swapOnCompleted;
            _swapOnCompleted = null;
            onCompleted?.Invoke();
        }

        public void SetSymbolSprites(Sprite[] symbolSprites, Sprite[] spinSymbolSprites)
        {
            _slotCellSpinView?.SetSymbolSprites(symbolSprites, spinSymbolSprites);
        }

        public void SkipCurrent()
        {
            _skipRequested = true;
        }

        public void SkipAll()
        {
            _skipRequested = true;
            _skipAllRequested = true;
        }

        public void PlayPatternSfx()
        {
            PlayClip(SelectNextPatternClip());
        }

        public void PlayRelicSfx()
        {
            PlayClip(_relicClip);
        }

        public void PlayFinalSfx()
        {
            PlayClip(_finalClip);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_skipCurrentOnTap || _playRoutine == null)
            {
                return;
            }

            SkipCurrent();
        }

        private void OnDisable()
        {
            CancelSwapAnimation();

            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            _skipRequested = false;
            _skipAllRequested = false;
            SetTapSkipEnabled(false);
            StopSlotSpinSfx();
            HideAllViews(includeSlotDisplay: true);
            UnsubscribeSpinPresenterEvents();
        }

        private IEnumerator PlayRoutine(
            SlotPresentationResult result,
            Action<SlotPresentationResult> onCompleted,
            bool playSpin)
        {
            _skipRequested = false;
            _skipAllRequested = false;
            _nextPatternClipIndex = 0;
            HideAllViews(includeSlotDisplay: playSpin);
            SetTapSkipEnabled(true);

            SlotLiveResultState liveResultState = SlotLiveResultState.Create(result?.FinalResult);
            Coroutine liveResultIntro = StartLiveResult(liveResultState);

            if (playSpin)
            {
                IEnumerator spinRoutine = ResolveSpinRoutine(result?.SpinResult);
                if (spinRoutine != null)
                {
                    yield return spinRoutine;
                    _skipRequested = false;
                }
            }

            if (liveResultIntro != null)
            {
                yield return liveResultIntro;
                _skipRequested = false;
            }

            SlotSpinCompleted?.Invoke();

            if (_skipAllRequested)
            {
                HideFinalResultImmediate();
                CompletePlayback(result, onCompleted);
                yield break;
            }

            var queue = new SlotPresentationQueue(result);
            bool[] playedRelics = CreateRelicPlayedFlags(result);

            for (int index = 0; index < queue.Steps.Count; index++)
            {
                _skipRequested = false;
                SlotPresentationStep step = queue.Steps[index];

                switch (step.Kind)
                {
                    case SlotPresentationStepKind.Pattern:
                        List<SlotRelicTriggerPresentationResult> relics =
                            CollectRelicsForPattern(result, playedRelics, step.Pattern);
                        yield return PlayPatternStep(step.Pattern, relics, liveResultState);
                        break;
                }

                if (_skipAllRequested)
                {
                    break;
                }
            }

            if (_skipAllRequested)
            {
                HideFinalResultImmediate();
                CompletePlayback(result, onCompleted);
                yield break;
            }

            yield return PlayRemainingRelics(result, playedRelics, liveResultState);

            if (_skipAllRequested)
            {
                HideFinalResultImmediate();
                CompletePlayback(result, onCompleted);
                yield break;
            }

            yield return CompleteLiveResult(result, liveResultState);

            CompletePlayback(result, onCompleted);
        }

        private IEnumerator PlayPatternStep(
            SlotPatternPresentationResult pattern,
            IReadOnlyList<SlotRelicTriggerPresentationResult> relics,
            SlotLiveResultState liveResultState)
        {
            if (pattern == null)
            {
                yield break;
            }

            RefreshPatternTargets();
            PatternStepStarted?.Invoke(pattern);
            PlayPatternSfx();
            liveResultState?.ApplyPattern(pattern);
            UpdateLiveResult(liveResultState, true);

            Coroutine relicRoutine = StartRelicIconRoutine(relics, liveResultState);

            if (_patternView != null)
            {
                yield return _patternView.Play(pattern, IsSkipRequested);
            }

            if (relicRoutine != null)
            {
                yield return relicRoutine;
            }
        }

        private IEnumerator PlayRemainingRelics(
            SlotPresentationResult result,
            bool[] playedRelics,
            SlotLiveResultState liveResultState)
        {
            List<SlotRelicTriggerPresentationResult> relics =
                CollectRemainingRelics(result, playedRelics);
            if (relics.Count == 0)
            {
                yield break;
            }

            Coroutine relicRoutine = StartRelicIconRoutine(relics, liveResultState);
            if (relicRoutine != null)
            {
                yield return relicRoutine;
            }
        }

        private IEnumerator CompleteLiveResult(
            SlotPresentationResult result,
            SlotLiveResultState liveResultState)
        {
            if (_finalResultView == null)
            {
                yield break;
            }

            SlotFinalPresentationResult finalResult =
                result?.FinalResult ?? liveResultState?.ToPresentationResult();
            if (finalResult == null)
            {
                yield break;
            }

            PlayFinalSfx();
            yield return _finalResultView.CompleteLive(finalResult, IsSkipRequested);
        }

        private Coroutine StartLiveResult(SlotLiveResultState liveResultState)
        {
            if (_finalResultView == null || liveResultState == null)
            {
                return null;
            }

            return StartCoroutine(
                _finalResultView.ShowLive(
                    liveResultState.ToPresentationResult(),
                    IsSkipRequested));
        }

        private void UpdateLiveResult(
            SlotLiveResultState liveResultState,
            bool pulse,
            SlotRelicTriggerPresentationResult impactRelic = null)
        {
            if (_finalResultView == null || liveResultState == null)
            {
                return;
            }

            _finalResultView.UpdateLive(liveResultState.ToPresentationResult(), pulse, impactRelic);
        }

        private void HideFinalResultImmediate()
        {
            if (_finalResultView != null)
            {
                _finalResultView.HideImmediate();
            }
        }

        private Coroutine StartRelicIconRoutine(
            IReadOnlyList<SlotRelicTriggerPresentationResult> relics,
            SlotLiveResultState liveResultState)
        {
            if (relics == null || relics.Count == 0)
            {
                return null;
            }

            if (_relicView == null)
            {
                ApplyRelicsToLiveResult(liveResultState, relics);
                UpdateLiveResult(liveResultState, true);
                return null;
            }

            List<SlotRelicTriggerPresentationResult> visualRelics =
                CollectVisualRelicsAndApplyInvisible(relics, liveResultState);
            if (visualRelics.Count == 0)
            {
                return null;
            }

            PlayRelicSfx();
            return StartCoroutine(
                _relicView.PlayBurstAtAnchor(
                    visualRelics,
                    _inventoryButtonAnchor,
                    relic =>
                    {
                        liveResultState?.ApplyRelic(relic);
                        UpdateLiveResult(liveResultState, true, relic);
                    },
                    IsSkipRequested));
        }

        private static void ApplyRelicsToLiveResult(
            SlotLiveResultState liveResultState,
            IReadOnlyList<SlotRelicTriggerPresentationResult> relics)
        {
            if (liveResultState == null || relics == null)
            {
                return;
            }

            for (int index = 0; index < relics.Count; index++)
            {
                liveResultState.ApplyRelic(relics[index]);
            }
        }

        private List<SlotRelicTriggerPresentationResult> CollectVisualRelicsAndApplyInvisible(
            IReadOnlyList<SlotRelicTriggerPresentationResult> relics,
            SlotLiveResultState liveResultState)
        {
            var visualRelics = new List<SlotRelicTriggerPresentationResult>(relics.Count);
            bool appliedInvisible = false;
            for (int index = 0; index < relics.Count; index++)
            {
                SlotRelicTriggerPresentationResult relic = relics[index];
                if (relic == null)
                {
                    continue;
                }

                if (relic.Icon != null)
                {
                    visualRelics.Add(relic);
                    continue;
                }

                liveResultState?.ApplyRelic(relic);
                appliedInvisible = true;
            }

            if (appliedInvisible)
            {
                UpdateLiveResult(liveResultState, true);
            }

            return visualRelics;
        }

        private static bool[] CreateRelicPlayedFlags(SlotPresentationResult result)
        {
            return result?.RelicTriggers != null
                ? new bool[result.RelicTriggers.Count]
                : Array.Empty<bool>();
        }

        private static List<SlotRelicTriggerPresentationResult> CollectRelicsForPattern(
            SlotPresentationResult result,
            bool[] playedRelics,
            SlotPatternPresentationResult pattern)
        {
            var relics = new List<SlotRelicTriggerPresentationResult>();
            if (result?.RelicTriggers == null || playedRelics == null || pattern == null)
            {
                return relics;
            }

            for (int index = 0; index < result.RelicTriggers.Count; index++)
            {
                if (index >= playedRelics.Length || playedRelics[index])
                {
                    continue;
                }

                SlotRelicTriggerPresentationResult relic = result.RelicTriggers[index];
                if (relic != null && relic.TriggerPatternIndex == pattern.SfxLevel)
                {
                    playedRelics[index] = true;
                    relics.Add(relic);
                }
            }

            return relics;
        }

        private static List<SlotRelicTriggerPresentationResult> CollectRemainingRelics(
            SlotPresentationResult result,
            bool[] playedRelics)
        {
            var relics = new List<SlotRelicTriggerPresentationResult>();
            if (result?.RelicTriggers == null || playedRelics == null)
            {
                return relics;
            }

            for (int index = 0; index < result.RelicTriggers.Count; index++)
            {
                if (index >= playedRelics.Length || playedRelics[index])
                {
                    continue;
                }

                SlotRelicTriggerPresentationResult relic = result.RelicTriggers[index];
                if (relic == null)
                {
                    continue;
                }

                playedRelics[index] = true;
                relics.Add(relic);
            }

            return relics;
        }

        private void CompletePlayback(SlotPresentationResult result, Action<SlotPresentationResult> onCompleted)
        {
            _playRoutine = null;
            _skipRequested = false;
            _skipAllRequested = false;
            SetTapSkipEnabled(false);
            onCompleted?.Invoke(result);
            Completed?.Invoke(result);
        }

        private bool IsSkipRequested()
        {
            return _skipRequested || _skipAllRequested;
        }

        private IEnumerator ResolveSpinRoutine(SlotSpinResult spinResult)
        {
            if (spinResult == null)
            {
                return null;
            }

            SlotMachineSpinDirector presenter = EnsureSpinPresenter();
            if (presenter != null)
            {
                return PlaySpinRoutine(presenter.Play(spinResult, IsSkipRequested));
            }

            if (_slotCellSpinView != null)
            {
                return PlaySpinRoutine(_slotCellSpinView.Play(spinResult, IsSkipRequested));
            }

            return null;
        }

        private IEnumerator PlaySpinRoutine(IEnumerator spinRoutine)
        {
            if (spinRoutine == null)
            {
                yield break;
            }

            StartSlotSpinSfx();
            try
            {
                yield return spinRoutine;
            }
            finally
            {
                StopSlotSpinSfx();
                if (!IsSkipRequested())
                {
                    PlaySlotSpinCompleteSfx();
                }
            }
        }

        private void EnsureViews()
        {
            if (_viewsResolved)
            {
                return;
            }

            _viewsResolved = true;

            var missing = new System.Text.StringBuilder();
            if (_slotCellSpinView == null) missing.Append("SlotCellSpinView(slot spin), ");
            if (_patternView == null) missing.Append("PatternPresentationDirector(pattern), ");
            if (_relicView == null) missing.Append("RelicPresentationDirector(relic), ");
            if (_finalResultView == null) missing.Append("FinalResultDirector(final), ");
            if (missing.Length > 0)
            {
                Debug.LogError(
                    $"[SlotPresentationManager] Presentation UI references must be wired in the inspector. Missing: {missing.ToString().TrimEnd(',', ' ')}.",
                    this);
            }
        }

        private SlotMachineSpinDirector EnsureSpinPresenter()
        {
            if (_slotCellSpinView == null ||
                !_slotCellSpinView.TryGetReelBindings(out Image[] cellIcons, out Sprite[] symbolSprites, out Sprite[] spinSprites))
            {
                SubscribeSpinPresenterEvents(_spinPresenter);
                return _spinPresenter;
            }

            if (_spinPresenter == null)
            {
                return null;
            }

            // Cell icons and sprite tables are runtime data, so (re)initialize every time.
            _spinPresenter.Initialize(cellIcons, symbolSprites, spinSprites);
            SubscribeSpinPresenterEvents(_spinPresenter);
            return _spinPresenter;
        }

        private void SubscribeSpinPresenterEvents(SlotMachineSpinDirector presenter)
        {
            if (_subscribedSpinPresenter == presenter)
            {
                return;
            }

            UnsubscribeSpinPresenterEvents();
            _subscribedSpinPresenter = presenter;

            if (_subscribedSpinPresenter != null)
            {
                _subscribedSpinPresenter.ReelStopped += HandleReelStopped;
            }
        }

        private void UnsubscribeSpinPresenterEvents()
        {
            if (_subscribedSpinPresenter == null)
            {
                return;
            }

            _subscribedSpinPresenter.ReelStopped -= HandleReelStopped;
            _subscribedSpinPresenter = null;
        }

        private void HandleReelStopped(int column)
        {
            if (!IsSkipRequested())
            {
                PlaySlotReelStopSfx();
            }

            SlotReelStopped?.Invoke(column);
        }

        private void RefreshPatternTargets()
        {
            if (_patternView == null)
            {
                return;
            }

            if (_spinPresenter != null &&
                _spinPresenter.TryGetVisibleCellIcons(out Image[] reelIcons))
            {
                _patternView.SetSlotCellIcons(reelIcons);
                return;
            }

            if (_slotCellSpinView != null &&
                _slotCellSpinView.TryGetReelBindings(
                    out Image[] cellIcons,
                    out _,
                    out _))
            {
                _patternView.SetSlotCellIcons(cellIcons);
            }
        }

        private void HideAllViews(bool includeSlotDisplay)
        {
            StopSlotSpinSfx();

            if (includeSlotDisplay)
            {
                if (_spinPresenter != null)
                {
                    _spinPresenter.StopImmediate();
                }
                else if (_slotCellSpinView != null)
                {
                    _slotCellSpinView.StopImmediate();
                }
            }

            if (_patternView != null)
            {
                _patternView.HideImmediate();
            }

            if (_relicView != null)
            {
                _relicView.HideImmediate();
            }

            if (_finalResultView != null)
            {
                _finalResultView.HideImmediate();
            }
        }

        private void PlayClip(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        private void StartSlotSpinSfx()
        {
            AudioSource audioSource = ResolveSlotSpinAudioSource();
            if (audioSource == null)
            {
                return;
            }

            AudioClip clip = _slotSpinClip != null ? _slotSpinClip : audioSource.clip;
            if (clip == null)
            {
                return;
            }

            if (!_loopSlotSpinClip)
            {
                audioSource.PlayOneShot(clip);
                return;
            }

            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            if (!audioSource.isPlaying)
            {
                _activeSlotSpinAudioSource = audioSource;
                audioSource.Play();
            }
        }

        private void StopSlotSpinSfx()
        {
            if (_activeSlotSpinAudioSource != null && _activeSlotSpinAudioSource.isPlaying)
            {
                _activeSlotSpinAudioSource.Stop();
                _activeSlotSpinAudioSource.loop = false;
            }

            _activeSlotSpinAudioSource = null;
        }

        private void PlaySlotReelStopSfx()
        {
            PlayClip(_slotReelStopClip);
        }

        private void PlaySlotSpinCompleteSfx()
        {
            PlayClip(_slotSpinCompleteClip);
        }

        private AudioSource ResolveSlotSpinAudioSource()
        {
            if (_slotSpinAudioSource != null)
            {
                return _slotSpinAudioSource;
            }

            return _audioSource;
        }

        private AudioClip SelectNextPatternClip()
        {
            if (_patternScaleClips != null && _patternScaleClips.Length > 0)
            {
                int level = Mathf.Clamp(_nextPatternClipIndex, 0, _patternScaleClips.Length - 1);
                _nextPatternClipIndex++;
                AudioClip clip = _patternScaleClips[level];

                if (clip != null)
                {
                    return clip;
                }
            }

            return _patternClip;
        }

        private void SetTapSkipEnabled(bool enabled)
        {
            if (_tapSkipGraphic != null)
            {
                _tapSkipGraphic.raycastTarget = enabled && _skipCurrentOnTap;
            }
        }

        private sealed class SlotLiveResultState
        {
            private SlotLiveResultState(
                int attackCount,
                int targetDamage,
                int targetDefense,
                int targetHeal)
            {
                AttackCount = Mathf.Max(1, attackCount);
                _targetDamage = Mathf.Max(0, targetDamage);
                _targetDefense = Mathf.Max(0, targetDefense);
                _targetHeal = Mathf.Max(0, targetHeal);
                Damage = 0;
            }

            internal int Damage { get; private set; }

            internal int Defense { get; private set; }

            internal int AttackCount { get; }

            internal int HealAmount { get; private set; }

            internal static SlotLiveResultState Create(SlotFinalPresentationResult finalResult)
            {
                return new SlotLiveResultState(
                    finalResult?.AttackCount ?? 1,
                    finalResult?.Damage ?? 0,
                    finalResult?.Defense ?? 0,
                    finalResult?.HealAmount ?? 0);
            }

            internal void ApplyPattern(SlotPatternPresentationResult pattern)
            {
                if (pattern == null)
                {
                    return;
                }

                Damage = AddClamped(
                    Damage,
                    Mathf.Max(0, pattern.BonusValue),
                    _targetDamage);
            }

            internal void ApplyRelic(SlotRelicTriggerPresentationResult relic)
            {
                if (relic == null)
                {
                    return;
                }

                Damage = AddClamped(Damage, Mathf.Max(0, relic.DamagePerHit), _targetDamage);
                Defense = AddClamped(Defense, Mathf.Max(0, relic.Block), _targetDefense);
                HealAmount = AddClamped(HealAmount, Mathf.Max(0, relic.Heal), _targetHeal);
            }

            internal SlotFinalPresentationResult ToPresentationResult()
            {
                return new SlotFinalPresentationResult(
                    Damage,
                    Defense,
                    AttackCount,
                    HealAmount,
                    BuildSummaryText());
            }

            private string BuildSummaryText()
            {
                string summary = $"ATK {Damage} / DEF {Defense} / HEAL {HealAmount}";

                if (AttackCount > 1)
                {
                    summary += $" / HIT {AttackCount}";
                }

                return summary;
            }

            private static int AddClamped(int current, int delta, int target)
            {
                int next = current + Mathf.Max(0, delta);
                return target > 0 ? Mathf.Min(next, target) : next;
            }

            private readonly int _targetDamage;
            private readonly int _targetDefense;
            private readonly int _targetHeal;
        }

        private const float SwapAnimationDuration = 0.2f;
        private const float SwapAnimationPop = 0.12f;

        private Coroutine _playRoutine;
        private Coroutine _swapRoutine;
        private Action _swapOnCompleted;
        private readonly List<GameObject> _swapGhosts = new();
        private readonly List<Image> _swapHiddenIcons = new();
        private bool _skipRequested;
        private bool _skipAllRequested;
        private int _nextPatternClipIndex;
        private AudioSource _activeSlotSpinAudioSource;
        private SlotMachineSpinDirector _subscribedSpinPresenter;
        private bool _viewsResolved;
    }
}
