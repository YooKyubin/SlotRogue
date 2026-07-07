using System.Threading;
using Cysharp.Threading.Tasks;

namespace SlotRogue.UI.GameFlow
{
    internal sealed class RunBattleSpinSequence
    {
        private readonly SlotLeverView _leverView;
        private readonly SlotMachineFrameView _frameView;
        private readonly SlotSpinHapticPlayer _hapticPlayer;
        private bool _leverRaised;
        private bool _settled;

        internal RunBattleSpinSequence(
            SlotLeverView leverView,
            SlotMachineFrameView frameView,
            SlotSpinHapticPlayer hapticPlayer = null)
        {
            _leverView = leverView;
            _frameView = frameView;
            _hapticPlayer = hapticPlayer;
        }

        internal bool LeverRaised => _leverRaised;
        internal bool Settled => _settled;

        internal void Reset()
        {
            _hapticPlayer?.StopRolling(playSettleTick: false);
            _leverRaised = false;
            _settled = false;
        }

        internal void SetupImmediate()
        {
            _hapticPlayer?.StopRolling(playSettleTick: false);
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
            _hapticPlayer?.PlayRolling();
        }

        internal void SetReelIdle(int reelIndex)
        {
            _frameView?.SetReelIdle(reelIndex);
            _hapticPlayer?.PlayReelStopTick();
        }

        internal async UniTask SettleIfNeededAsync(CancellationToken ct)
        {
            if (_settled || _frameView == null)
            {
                _hapticPlayer?.StopRolling(playSettleTick: false);
                return;
            }

            try
            {
                await _frameView.StopAtIdleAsync(ct);
                _settled = true;
            }
            finally
            {
                _hapticPlayer?.StopRolling(playSettleTick: !ct.IsCancellationRequested);
            }
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
            _hapticPlayer?.StopRolling(playSettleTick: false);

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
