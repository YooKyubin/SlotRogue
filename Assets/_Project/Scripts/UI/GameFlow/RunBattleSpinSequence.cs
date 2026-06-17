using System.Threading;
using Cysharp.Threading.Tasks;

namespace SlotRogue.UI.GameFlow
{
    internal sealed class RunBattleSpinSequence
    {
        private readonly SlotLeverView _leverView;
        private readonly SlotMachineFrameView _frameView;
        private bool _leverRaised;
        private bool _settled;

        internal RunBattleSpinSequence(SlotLeverView leverView, SlotMachineFrameView frameView)
        {
            _leverView = leverView;
            _frameView = frameView;
        }

        internal bool LeverRaised => _leverRaised;
        internal bool Settled => _settled;

        internal void Reset()
        {
            _leverRaised = false;
            _settled = false;
        }

        internal void SetupImmediate()
        {
            _leverView?.SetUpImmediate();
            _frameView?.SetIdleImmediate();
        }

        internal async UniTask PlayDownAsync(CancellationToken ct)
        {
            if (_leverView != null)
            {
                await _leverView.PlayDownAsync(ct);
            }
        }

        internal void StartSpin()
        {
            _frameView?.PlaySpin();
        }

        internal void SetReelIdle(int reelIndex)
        {
            _frameView?.SetReelIdle(reelIndex);
        }

        internal async UniTask SettleIfNeededAsync(CancellationToken ct)
        {
            if (_settled || _frameView == null)
            {
                return;
            }

            await _frameView.StopAtIdleAsync(ct);
            _settled = true;
        }

        internal async UniTask RaiseLeverIfNeededAsync()
        {
            if (_leverRaised || _leverView == null)
            {
                return;
            }

            _leverRaised = true;
            await _leverView.PlayUpAsync();
        }

        internal void ResetImmediate()
        {
            if (!_leverRaised)
            {
                _leverView?.SetUpImmediate();
            }

            if (!_settled)
            {
                _frameView?.SetIdleImmediate();
            }
        }
    }
}
