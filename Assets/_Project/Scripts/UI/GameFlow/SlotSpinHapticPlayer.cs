using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    public sealed class SlotSpinHapticPlayer : MonoBehaviour
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField, Range(4, 30)] private int _pulseDurationMs = 10;
        [SerializeField, Range(35, 120)] private int _pulseIntervalMs = 58;
        [SerializeField, Range(1, 255)] private int _rollingAmplitude = 48;
        [SerializeField] private bool _playReelStopTicks = true;
        [SerializeField, Range(4, 30)] private int _reelStopTickDurationMs = 8;
        [SerializeField, Range(1, 255)] private int _reelStopTickAmplitude = 72;
        [SerializeField] private bool _playSettleTick = true;
        [SerializeField, Range(4, 35)] private int _settleTickDurationMs = 14;
        [SerializeField, Range(1, 255)] private int _settleTickAmplitude = 88;
        [SerializeField] private bool _useHandheldFallback;

        private CancellationTokenSource _rollingCts;
        private bool _isRolling;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool _androidVibrationUnavailable;
#endif

        public void PlayRolling()
        {
            if (!_enabled || _isRolling || !isActiveAndEnabled)
            {
                return;
            }

            _isRolling = true;
            _rollingCts = new CancellationTokenSource();
            PlayRollingAsync(_rollingCts.Token).Forget();
        }

        public void PlayReelStopTick()
        {
            if (!_enabled || !_playReelStopTicks || !isActiveAndEnabled)
            {
                return;
            }

            Pulse(_reelStopTickDurationMs, _reelStopTickAmplitude);
        }

        public void StopRolling(bool playSettleTick)
        {
            bool wasRolling = _isRolling;

            _isRolling = false;
            if (_rollingCts != null)
            {
                _rollingCts.Cancel();
                _rollingCts.Dispose();
                _rollingCts = null;
            }

            CancelNativeVibration();

            if (wasRolling && playSettleTick && _playSettleTick && _enabled && isActiveAndEnabled)
            {
                Pulse(_settleTickDurationMs, _settleTickAmplitude);
            }
        }

        private void OnDisable()
        {
            StopRolling(playSettleTick: false);
        }

        private void OnDestroy()
        {
            StopRolling(playSettleTick: false);
        }

        private async UniTaskVoid PlayRollingAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Pulse(_pulseDurationMs, _rollingAmplitude);

                    await UniTask.Delay(
                        TimeSpan.FromMilliseconds(Math.Max(_pulseIntervalMs, _pulseDurationMs)),
                        DelayType.UnscaledDeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void Pulse(int durationMs, int amplitude)
        {
            durationMs = Mathf.Max(1, durationMs);
            amplitude = Mathf.Clamp(amplitude, 1, 255);

#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid(durationMs, amplitude);
#else
            if (_useHandheldFallback)
            {
                Handheld.Vibrate();
            }
#endif
        }

        private static void CancelNativeVibration()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_androidVibrationUnavailable)
            {
                return;
            }

            AndroidJavaObject vibrator = TryGetVibrator();
            try
            {
                vibrator?.Call("cancel");
            }
            catch (Exception)
            {
                _androidVibrationUnavailable = true;
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void VibrateAndroid(int durationMs, int amplitude)
        {
            if (_androidVibrationUnavailable)
            {
                return;
            }

            AndroidJavaObject vibrator = TryGetVibrator();
            if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
            {
                return;
            }

            try
            {
                using AndroidJavaClass versionClass = new("android.os.Build$VERSION");
                int sdkInt = versionClass.GetStatic<int>("SDK_INT");

                if (sdkInt >= 26)
                {
                    using AndroidJavaClass effectClass = new("android.os.VibrationEffect");
                    using AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot",
                        (long)durationMs,
                        amplitude);
                    vibrator.Call("vibrate", effect);
                    return;
                }

                vibrator.Call("vibrate", (long)durationMs);
            }
            catch (Exception)
            {
                _androidVibrationUnavailable = true;
            }
        }

        private static AndroidJavaObject TryGetVibrator()
        {
            try
            {
                using AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity =
                    unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                return activity?.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            catch (Exception)
            {
                _androidVibrationUnavailable = true;
                return null;
            }
        }
#endif
    }
}
