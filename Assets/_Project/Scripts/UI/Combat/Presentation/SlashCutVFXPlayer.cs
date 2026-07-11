using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SlotRogue.UI.Combat.Presentation
{
    /// <summary>
    /// slash Animator를 재생하고 Animation Event 완료 신호를 UniTask로 전달한다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class SlashCutVFXPlayer : MonoBehaviour
    {
        private const int BaseLayer = 0;
        private const float RestartFromBeginning = 0f;

        [SerializeField] private Animator _animator;
        [SerializeField] private string _animationStateName = "Slash";

        private SlashCutPlaybackState _currentPlayback;
        private CombatDamageVFXCueHub _cueHub;
        private CancellationToken _cancellationToken;
        private UniTask _impactTask;
        private bool _impactRaised;

        private void OnDisable()
        {
            CancelPlayback();
        }

        private void OnDestroy()
        {
            CancelPlayback();
        }

        public async UniTask PlayAsync(CancellationToken cancellationToken)
        {
            if (!ValidateAnimator() || !ValidateAnimationStateName())
            {
                return;
            }

            CancelPlayback();
            _impactTask = UniTask.CompletedTask;
            _impactRaised = false;
            var playback = new SlashCutPlaybackState();
            _currentPlayback = playback;
            int stateHash = Animator.StringToHash(_animationStateName);
            _animator.Play(stateHash, BaseLayer, RestartFromBeginning);

            try
            {
                await playback.WaitForCompletedAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (ReferenceEquals(_currentPlayback, playback))
            {
                CancelPlayback();
                throw;
            }
        }

        public void ConfigureCueHub(CombatDamageVFXCueHub cueHub, CancellationToken cancellationToken)
        {
            _cueHub = cueHub;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// slash 애니메이션의 명중 프레임 Animation Event가 호출한다.
        /// </summary>
        public void NotifyImpact()
        {
            if (_impactRaised)
            {
                return;
            }

            _impactRaised = true;
            _impactTask = _cueHub != null
                ? _cueHub.PublishImpactAsync(transform.position, _cancellationToken)
                : UniTask.CompletedTask;
        }

        /// <summary>
        /// slash 애니메이션 마지막 프레임의 Animation Event가 호출한다.
        /// </summary>
        public void NotifyAnimationCompleted()
        {
            CompleteAfterImpactAsync().Forget();
        }

        private async UniTaskVoid CompleteAfterImpactAsync()
        {
            try
            {
                await _impactTask;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            SlashCutPlaybackState completedPlayback = DetachCurrentPlayback();
            if (completedPlayback == null)
            {
                return;
            }

            completedPlayback.MarkCompleted();
            completedPlayback.Dispose();
        }

        internal void CancelPlayback()
        {
            SlashCutPlaybackState canceledPlayback = DetachCurrentPlayback();
            if (canceledPlayback == null)
            {
                return;
            }

            canceledPlayback.Dispose();
        }

        private SlashCutPlaybackState DetachCurrentPlayback()
        {
            SlashCutPlaybackState playback = _currentPlayback;
            _currentPlayback = null;
            return playback;
        }

        private bool ValidateAnimator()
        {
            if (_animator != null)
            {
                return true;
            }

            Debug.LogError(
                "[SlashCutVFXPlayer] Animator reference is missing. Assign the slash prefab Animator.",
                this);
            return false;
        }

        private bool ValidateAnimationStateName()
        {
            if (!string.IsNullOrWhiteSpace(_animationStateName))
            {
                return true;
            }

            Debug.LogError(
                "[SlashCutVFXPlayer] Animation state name is empty. Assign the slash Animator state name.",
                this);
            return false;
        }
    }

    internal sealed class SlashCutPlaybackState : IDisposable
    {
        private readonly UniTaskCompletionSource _completedCompletion = new();
        private bool _disposed;

        public UniTask WaitForCompletedAsync(CancellationToken cancellationToken)
        {
            return WaitAsync(_completedCompletion, cancellationToken);
        }

        public void MarkCompleted()
        {
            if (_disposed)
            {
                return;
            }

            _completedCompletion.TrySetResult();
        }

        public void Cancel()
        {
            if (_disposed)
            {
                return;
            }

            _completedCompletion.TrySetCanceled();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Cancel();
            _disposed = true;
        }

        private static async UniTask WaitAsync(UniTaskCompletionSource completion, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            using CancellationTokenRegistration registration =
                cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            await completion.Task;
        }
    }
}
