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
        private const int DamagePerPatternValue = 2;

        [SerializeField] private SlotCellSpinView _slotCellSpinView;
        [SerializeField] private SlotMachineSpinDirector _spinPresenter;
        [SerializeField] private PatternPresentationDirector _patternView;
        [SerializeField] private RelicPresentationDirector _relicView;
        [SerializeField] private FinalResultDirector _finalResultView;
        [SerializeField] private Graphic _tapSkipGraphic;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _patternClip;
        [SerializeField] private AudioClip[] _patternScaleClips;
        [SerializeField] private AudioClip _relicClip;
        [SerializeField] private AudioClip _finalClip;
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
            SlotCellSpinView slotCellSpinView = null)
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
            SetTapSkipEnabled(false);
        }

        public void Play(SlotPresentationResult result, Action<SlotPresentationResult> onCompleted)
        {
            if (!isActiveAndEnabled)
            {
                onCompleted?.Invoke(result);
                Completed?.Invoke(result);
                return;
            }

            EnsureViews();

            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
            }

            _playRoutine = StartCoroutine(PlayRoutine(result, onCompleted));
        }

        public void ShowImmediate(SlotSpinResult result)
        {
            if (result == null)
            {
                return;
            }

            EnsureViews();

            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            _skipRequested = false;
            _skipAllRequested = false;
            SetTapSkipEnabled(false);

            SlotMachineSpinDirector presenter = EnsureSpinPresenter();
            if (presenter != null)
            {
                presenter.ShowImmediate(result);
                return;
            }

            _slotCellSpinView?.StopImmediate(result);
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
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            _skipRequested = false;
            _skipAllRequested = false;
            SetTapSkipEnabled(false);
            HideAllViews();
            UnsubscribeSpinPresenterEvents();
        }

        private IEnumerator PlayRoutine(SlotPresentationResult result, Action<SlotPresentationResult> onCompleted)
        {
            _skipRequested = false;
            _skipAllRequested = false;
            _nextPatternClipIndex = 0;
            HideAllViews();
            SetTapSkipEnabled(true);

            SlotLiveResultState liveResultState = SlotLiveResultState.Create(result?.FinalResult);
            Coroutine liveResultIntro = StartLiveResult(liveResultState);

            IEnumerator spinRoutine = ResolveSpinRoutine(result?.SpinResult);
            if (spinRoutine != null)
            {
                yield return spinRoutine;
                _skipRequested = false;
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
            RectTransform impactAnchor = _finalResultView != null ? _finalResultView.ImpactAnchor : null;
            return StartCoroutine(
                _relicView.PlayIconFlyToResult(
                    visualRelics,
                    impactAnchor,
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
                return presenter.Play(spinResult, IsSkipRequested);
            }

            if (_slotCellSpinView != null)
            {
                return _slotCellSpinView.Play(spinResult, IsSkipRequested);
            }

            return null;
        }

        // Rebind presentation views from the scene when inspector references were lost.
        // Missing views warn once; that presentation layer is skipped if the component is absent.
        private void EnsureViews()
        {
            _slotCellSpinView ??= SceneComponentResolver.FindInSceneRoot<SlotCellSpinView>(transform);
            _patternView ??= SceneComponentResolver.FindInSceneRoot<PatternPresentationDirector>(transform);
            _relicView ??= SceneComponentResolver.FindInSceneRoot<RelicPresentationDirector>(transform);
            _finalResultView ??= SceneComponentResolver.FindInSceneRoot<FinalResultDirector>(transform);

            if (_viewsResolved)
            {
                return;
            }

            _viewsResolved = true;

            var missing = new System.Text.StringBuilder();
            if (_patternView == null) missing.Append("PatternPresentationDirector(pattern), ");
            if (_relicView == null) missing.Append("RelicPresentationDirector(relic), ");
            if (_finalResultView == null) missing.Append("FinalResultDirector(final), ");
            if (missing.Length > 0)
            {
                Debug.LogWarning(
                    $"[SlotPresentationManager] Missing presentation views: {missing.ToString().TrimEnd(',', ' ')}. " +
                    "The corresponding presentation layer will be skipped.");
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

            // Prefer a presenter authored in the prefab/scene, then one already on the spin view,
            // otherwise add one at runtime (which builds its reel overlay over the cells).
            if (_spinPresenter == null)
            {
                _spinPresenter = _slotCellSpinView.GetComponent<SlotMachineSpinDirector>();
            }

            if (_spinPresenter == null)
            {
                _spinPresenter = _slotCellSpinView.gameObject.AddComponent<SlotMachineSpinDirector>();
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

        private void HideAllViews()
        {
            if (_spinPresenter != null)
            {
                _spinPresenter.StopImmediate();
            }
            else if (_slotCellSpinView != null)
            {
                _slotCellSpinView.StopImmediate();
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
                Damage = ResolveInitialDamage(_targetDamage);
            }

            internal int Damage { get; private set; }

            internal int Defense { get; private set; }

            internal int AttackCount { get; }

            internal int HealAmount { get; private set; }

            internal static SlotLiveResultState Create(SlotFinalPresentationResult finalResult)
            {
                return new SlotLiveResultState(
                    finalResult?.AttackCount ?? 1,
                    finalResult?.Damage ?? SlotCombatRequest.BaseAttackDamage,
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
                    Mathf.Max(0, pattern.BonusValue) * DamagePerPatternValue,
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

            private static int ResolveInitialDamage(int targetDamage)
            {
                if (targetDamage <= 0)
                {
                    return 0;
                }

                return Mathf.Min(SlotCombatRequest.BaseAttackDamage, targetDamage);
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

        private Coroutine _playRoutine;
        private bool _skipRequested;
        private bool _skipAllRequested;
        private int _nextPatternClipIndex;
        private SlotMachineSpinDirector _subscribedSpinPresenter;
        private bool _viewsResolved;
    }
}
