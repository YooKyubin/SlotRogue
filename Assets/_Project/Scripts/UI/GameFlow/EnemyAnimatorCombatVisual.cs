using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    [RequireComponent(typeof(Animator))]
    public sealed class EnemyAnimatorCombatVisual : MonoBehaviour, IEnemyCombatVisual
    {
        private const int BaseLayer = 0;
        private const float RestartFromBeginning = 0f;

        private static readonly int IdleStateHash = Animator.StringToHash("Idle");

        [SerializeField] private Animator _animator;

        private EnemyActionPlaybackState _currentPlayback;

        private void OnDisable()
        {
            CancelCurrentPlayback();
        }

        private void OnDestroy()
        {
            CancelCurrentPlayback();
        }

        public void PlayIdle()
        {
            CancelCurrentPlayback();
            if (!ValidateAnimator())
            {
                return;
            }

            _animator.Play(IdleStateHash, BaseLayer, RestartFromBeginning);
        }

        public async UniTask PlayActionUntilEffectPointAsync(string actionName, CancellationToken cancellationToken)
        {
            if (!ValidateAnimator())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(actionName))
            {
                Debug.LogError(
                    "[EnemyAnimatorCombatVisual] ActionName is empty.",
                    this);
                return;
            }

            if (_currentPlayback != null)
            {
                Debug.LogError(
                    "[EnemyAnimatorCombatVisual] Previous action playback was still active. " +
                    "It will be canceled before starting the next action.",
                    this);
                CancelCurrentPlayback();
            }

            var playback = new EnemyActionPlaybackState();
            _currentPlayback = playback;
            int stateHash = Animator.StringToHash(actionName);
            _animator.Play(stateHash, BaseLayer, RestartFromBeginning);
            try
            {
                await playback.WaitForEffectPointAsync(cancellationToken);
            }
            catch (System.OperationCanceledException) when (ReferenceEquals(_currentPlayback, playback))
            {
                CancelCurrentPlayback();
                throw;
            }
        }

        public async UniTask WaitForActionCompletedAsync(CancellationToken cancellationToken)
        {
            EnemyActionPlaybackState playback = _currentPlayback;
            if (playback == null)
            {
                return;
            }

            try
            {
                await playback.WaitForActionCompletedAsync(cancellationToken);
            }
            catch (System.OperationCanceledException) when (ReferenceEquals(_currentPlayback, playback))
            {
                CancelCurrentPlayback();
                throw;
            }
        }

        private void OnEffectPoint()
        {
            _currentPlayback?.MarkEffectPoint();
        }

        private void OnActionAnimationCompleted()
        {
            EnemyActionPlaybackState completedPlayback = DetachCurrentPlayback();
            if (completedPlayback == null)
            {
                return;
            }

            completedPlayback.MarkActionCompleted();
            completedPlayback.Dispose();
        }

        private bool ValidateAnimator()
        {
            if (_animator != null)
            {
                return true;
            }

            Debug.LogError(
                "[EnemyAnimatorCombatVisual] Animator reference is missing. " +
                "Assign the Animator used by the MoonRabit combat visual prefab.",
                this);
            return false;
        }

        private void CancelCurrentPlayback()
        {
            EnemyActionPlaybackState canceledPlayback = DetachCurrentPlayback();
            if (canceledPlayback == null)
            {
                return;
            }

            canceledPlayback.Cancel();
            canceledPlayback.Dispose();
        }

        private EnemyActionPlaybackState DetachCurrentPlayback()
        {
            EnemyActionPlaybackState playback = _currentPlayback;
            _currentPlayback = null;
            return playback;
        }
    }

    internal sealed class EnemyActionPlaybackState : System.IDisposable
    {
        private readonly UniTaskCompletionSource _effectPointCompletion = new();
        private readonly UniTaskCompletionSource _actionCompletedCompletion = new();
        private bool _disposed;

        public UniTask WaitForEffectPointAsync(CancellationToken cancellationToken)
        {
            return WaitAsync(_effectPointCompletion, cancellationToken);
        }

        public UniTask WaitForActionCompletedAsync(CancellationToken cancellationToken)
        {
            return WaitAsync(_actionCompletedCompletion, cancellationToken);
        }

        public void MarkEffectPoint()
        {
            if (_disposed)
            {
                return;
            }

            _effectPointCompletion.TrySetResult();
        }

        public void MarkActionCompleted()
        {
            if (_disposed)
            {
                return;
            }

            _actionCompletedCompletion.TrySetResult();
        }

        public void Cancel()
        {
            if (_disposed)
            {
                return;
            }

            _effectPointCompletion.TrySetCanceled();
            _actionCompletedCompletion.TrySetCanceled();
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

        private static async UniTask WaitAsync(
            UniTaskCompletionSource completion,
            CancellationToken cancellationToken)
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
