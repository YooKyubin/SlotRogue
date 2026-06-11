using System;
using System.Collections;
using SlotRogue.Slot.Data;
using SlotRogue.UI.SlotPresentation.Reel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlotRogue.UI.SlotPresentation
{
    public sealed class SlotPresentationManager : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private SlotCellSpinView _slotCellSpinView;
        [SerializeField] private SlotMachineSpinPresenter _spinPresenter;
        [SerializeField] private PatternPresentationView _patternView;
        [SerializeField] private RelicPresentationView _relicView;
        [SerializeField] private FinalResultView _finalResultView;
        [SerializeField] private Graphic _tapSkipGraphic;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _patternClip;
        [SerializeField] private AudioClip[] _patternScaleClips;
        [SerializeField] private AudioClip _relicClip;
        [SerializeField] private AudioClip _finalClip;
        [SerializeField] private bool _skipCurrentOnTap = true;

        public event Action<SlotPresentationResult> Completed;
        public event Action SlotSpinCompleted;
        public event Action<SlotPatternPresentationResult> PatternStepStarted;

        public bool IsPlaying => _playRoutine != null;

        public void Bind(
            PatternPresentationView patternView,
            RelicPresentationView relicView,
            FinalResultView finalResultView,
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

            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
            }

            _playRoutine = StartCoroutine(PlayRoutine(result, onCompleted));
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
        }

        private IEnumerator PlayRoutine(SlotPresentationResult result, Action<SlotPresentationResult> onCompleted)
        {
            _skipRequested = false;
            _skipAllRequested = false;
            _nextPatternClipIndex = 0;
            HideAllViews();
            SetTapSkipEnabled(true);

            IEnumerator spinRoutine = ResolveSpinRoutine(result.SpinResult);
            if (spinRoutine != null)
            {
                yield return spinRoutine;
                _skipRequested = false;
            }

            SlotSpinCompleted?.Invoke();

            if (_skipAllRequested)
            {
                CompletePlayback(result, onCompleted);
                yield break;
            }

            var queue = new SlotPresentationQueue(result);

            for (int index = 0; index < queue.Steps.Count; index++)
            {
                _skipRequested = false;
                SlotPresentationStep step = queue.Steps[index];

                switch (step.Kind)
                {
                    case SlotPresentationStepKind.Pattern:
                        yield return PlayPatternStep(step.Pattern);
                        break;
                    case SlotPresentationStepKind.Relic:
                        yield return PlayRelicStep(step.Relic);
                        break;
                    case SlotPresentationStepKind.Final:
                        yield return PlayFinalStep(step.FinalResult);
                        break;
                }

                if (_skipAllRequested)
                {
                    break;
                }
            }

            CompletePlayback(result, onCompleted);
        }

        private IEnumerator PlayPatternStep(SlotPatternPresentationResult pattern)
        {
            if (_patternView == null || pattern == null)
            {
                yield break;
            }

            PatternStepStarted?.Invoke(pattern);
            PlayPatternSfx();
            yield return _patternView.Play(pattern, IsSkipRequested);
        }

        private IEnumerator PlayRelicStep(SlotRelicTriggerPresentationResult relic)
        {
            if (_relicView == null || relic == null)
            {
                yield break;
            }

            PlayRelicSfx();
            yield return _relicView.Play(relic, IsSkipRequested);
        }

        private IEnumerator PlayFinalStep(SlotFinalPresentationResult finalResult)
        {
            if (_finalResultView == null || finalResult == null)
            {
                yield break;
            }

            PlayFinalSfx();
            yield return _finalResultView.Play(finalResult, IsSkipRequested);
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

            SlotMachineSpinPresenter presenter = EnsureSpinPresenter();
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

        private SlotMachineSpinPresenter EnsureSpinPresenter()
        {
            if (_slotCellSpinView == null ||
                !_slotCellSpinView.TryGetReelBindings(out Image[] cellIcons, out Sprite[] symbolSprites, out Sprite[] spinSprites))
            {
                return _spinPresenter;
            }

            // Prefer a presenter authored in the prefab/scene, then one already on the spin view,
            // otherwise add one at runtime (which builds its reel overlay over the cells).
            if (_spinPresenter == null)
            {
                _spinPresenter = _slotCellSpinView.GetComponent<SlotMachineSpinPresenter>();
            }

            if (_spinPresenter == null)
            {
                _spinPresenter = _slotCellSpinView.gameObject.AddComponent<SlotMachineSpinPresenter>();
            }

            // Cell icons and sprite tables are runtime data, so (re)initialize every time.
            _spinPresenter.Initialize(cellIcons, symbolSprites, spinSprites);
            return _spinPresenter;
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

        private Coroutine _playRoutine;
        private bool _skipRequested;
        private bool _skipAllRequested;
        private int _nextPatternClipIndex;
    }
}
