using System;
using System.Collections;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace SlotRogue.UI.Ads
{
    public sealed class AdsManager : MonoBehaviour
    {
        private const string RevivePlacement = "revive";
        private const string RewardRerollPlacement = "reward_reroll";
        private const string ExtraRewardPlacement = "reward_extra";
        private const string RewardDoublePlacement = "reward_double";
        private const float ReloadDelaySeconds = 2f;

        [SerializeField] private string appKey;
        [SerializeField] private string rewardedAdUnitId;

        private LevelPlayRewardedAd _rewardedAd;
        private Action _pendingReward;
        private Coroutine _reloadCoroutine;
        private Coroutine _sessionCloseCoroutine;
        private RewardedAdPurpose _activePurpose;
        private bool _isInitialized;
        private bool _isLoading;
        private bool _isShowing;
        private bool _rewardGranted;
        private bool _hasActiveSession;

        public static AdsManager Instance { get; private set; }

        public event Action RewardedAvailabilityChanged;

        public event Action<RewardedAdPurpose, bool> RewardedSessionEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent != null)
            {
                transform.SetParent(null, false);
            }

            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            StopReloadCoroutine();
            StopSessionCloseCoroutine();
            UnsubscribeInitializationEvents();
            DisposeRewardedAd();
            Instance = null;
        }

        public bool CanShowRewarded(RewardedAdPurpose purpose)
        {
            if (!_isInitialized ||
                _isLoading ||
                _isShowing ||
                _rewardedAd == null ||
                !_rewardedAd.IsAdReady())
            {
                return false;
            }

            string placementName = GetPlacementName(purpose);
            return !string.IsNullOrEmpty(placementName) &&
                !LevelPlayRewardedAd.IsPlacementCapped(placementName);
        }

        public void ShowRewarded(RewardedAdPurpose purpose, Action onReward)
        {
            if (onReward == null)
            {
                Debug.LogWarning("[AdsManager] Reward callback is missing.");
                return;
            }

            if (!CanShowRewarded(purpose))
            {
                Debug.LogWarning($"[AdsManager] Rewarded is not ready for {purpose}.");
                return;
            }

            _pendingReward = onReward;
            _activePurpose = purpose;
            _hasActiveSession = true;
            _rewardGranted = false;
            _isShowing = true;
            NotifyRewardedAvailabilityChanged();

            try
            {
                _rewardedAd.ShowAd(GetPlacementName(purpose));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AdsManager] Rewarded Display Failed: {exception.Message}");
                // 상태 플래그를 먼저 정리한 뒤 세션 종료를 통지한다. RewardedSessionEnded 핸들러가
                // 동기적으로 CanShowRewarded를 호출해도 일관된 상태(showing=false)를 보게 한다.
                _isShowing = false;
                CompleteRewardedSession(rewarded: false);
                NotifyRewardedAvailabilityChanged();
                LoadRewarded();
            }
        }

        private void Initialize()
        {
#if UNITY_EDITOR && !UNITY_ANDROID && !UNITY_IOS
            Debug.LogError(
                "[AdsManager] Init Failed: LevelPlay rewarded ads require the " +
                "Unity Build Target to be Android or iOS. Switch Build Profiles " +
                "to Android before testing in the Editor.");
            NotifyRewardedAvailabilityChanged();
#else
            if (string.IsNullOrWhiteSpace(appKey))
            {
                Debug.LogError("[AdsManager] Init Failed: App Key is not configured.");
                return;
            }

            if (string.IsNullOrWhiteSpace(rewardedAdUnitId))
            {
                Debug.LogError("[AdsManager] Init Failed: Rewarded Ad Unit ID is not configured.");
                return;
            }

            LevelPlay.OnInitSuccess += HandleInitSuccess;
            LevelPlay.OnInitFailed += HandleInitFailed;
            try
            {
                LevelPlay.Init(appKey.Trim());
            }
            catch (Exception exception)
            {
                UnsubscribeInitializationEvents();
                Debug.LogError($"[AdsManager] Init Failed: {exception.Message}");
                NotifyRewardedAvailabilityChanged();
            }
#endif
        }

        private void HandleInitSuccess(LevelPlayConfiguration configuration)
        {
            UnsubscribeInitializationEvents();
            _isInitialized = true;
            GameLog.Info("[AdsManager] Init Success");

            _rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId.Trim());
            _rewardedAd.OnAdLoaded += HandleRewardedLoaded;
            _rewardedAd.OnAdLoadFailed += HandleRewardedLoadFailed;
            _rewardedAd.OnAdDisplayed += HandleRewardedDisplayed;
            _rewardedAd.OnAdDisplayFailed += HandleRewardedDisplayFailed;
            _rewardedAd.OnAdRewarded += HandleRewardedRewarded;
            _rewardedAd.OnAdClosed += HandleRewardedClosed;
            LoadRewarded();
        }

        private void HandleInitFailed(LevelPlayInitError error)
        {
            UnsubscribeInitializationEvents();
            Debug.LogError($"[AdsManager] Init Failed: {error}");
            NotifyRewardedAvailabilityChanged();
        }

        private void HandleRewardedLoaded(LevelPlayAdInfo adInfo)
        {
            _isLoading = false;
            GameLog.Info("[AdsManager] Rewarded Loaded");
            NotifyRewardedAvailabilityChanged();
        }

        private void HandleRewardedLoadFailed(LevelPlayAdError error)
        {
            _isLoading = false;
            Debug.LogError($"[AdsManager] Rewarded Load Failed: {error}");
            NotifyRewardedAvailabilityChanged();
            ScheduleReload();
        }

        private void HandleRewardedDisplayed(LevelPlayAdInfo adInfo)
        {
            GameLog.Info("[AdsManager] Rewarded Displayed");
        }

        private void HandleRewardedDisplayFailed(
            LevelPlayAdInfo adInfo,
            LevelPlayAdError error)
        {
            Debug.LogError($"[AdsManager] Rewarded Display Failed: {error}");
            _isShowing = false;
            CompleteRewardedSession(rewarded: false);
            NotifyRewardedAvailabilityChanged();
            LoadRewarded();
        }

        private void HandleRewardedRewarded(
            LevelPlayAdInfo adInfo,
            LevelPlayReward reward)
        {
            GameLog.Info("[AdsManager] Rewarded Rewarded");
            if (_rewardGranted)
            {
                return;
            }

            _rewardGranted = true;
            Action rewardCallback = _pendingReward;
            _pendingReward = null;

            try
            {
                rewardCallback?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            CompleteRewardedSession(rewarded: true);
        }

        private void HandleRewardedClosed(LevelPlayAdInfo adInfo)
        {
            GameLog.Info("[AdsManager] Rewarded Closed");
            _isShowing = false;
            if (_hasActiveSession && !_rewardGranted)
            {
                StopSessionCloseCoroutine();
                _sessionCloseCoroutine = StartCoroutine(
                    CompleteClosedSessionAfterFrame());
            }

            NotifyRewardedAvailabilityChanged();
            LoadRewarded();
        }

        private IEnumerator CompleteClosedSessionAfterFrame()
        {
            // LevelPlay's Editor mock emits Closed before Rewarded.
            yield return null;
            _sessionCloseCoroutine = null;
            if (_hasActiveSession && !_rewardGranted)
            {
                CompleteRewardedSession(rewarded: false);
            }
        }

        private void LoadRewarded()
        {
            if (!_isInitialized ||
                _rewardedAd == null ||
                _isLoading ||
                _isShowing)
            {
                return;
            }

            StopReloadCoroutine();
            _isLoading = true;
            NotifyRewardedAvailabilityChanged();
            _rewardedAd.LoadAd();
        }

        private void ScheduleReload()
        {
            if (_reloadCoroutine == null)
            {
                _reloadCoroutine = StartCoroutine(ReloadAfterDelay());
            }
        }

        private IEnumerator ReloadAfterDelay()
        {
            yield return new WaitForSecondsRealtime(ReloadDelaySeconds);
            _reloadCoroutine = null;
            LoadRewarded();
        }

        private void StopReloadCoroutine()
        {
            if (_reloadCoroutine == null)
            {
                return;
            }

            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }

        private void StopSessionCloseCoroutine()
        {
            if (_sessionCloseCoroutine == null)
            {
                return;
            }

            StopCoroutine(_sessionCloseCoroutine);
            _sessionCloseCoroutine = null;
        }

        private void DisposeRewardedAd()
        {
            if (_rewardedAd == null)
            {
                return;
            }

            _rewardedAd.OnAdLoaded -= HandleRewardedLoaded;
            _rewardedAd.OnAdLoadFailed -= HandleRewardedLoadFailed;
            _rewardedAd.OnAdDisplayed -= HandleRewardedDisplayed;
            _rewardedAd.OnAdDisplayFailed -= HandleRewardedDisplayFailed;
            _rewardedAd.OnAdRewarded -= HandleRewardedRewarded;
            _rewardedAd.OnAdClosed -= HandleRewardedClosed;
            _rewardedAd.DestroyAd();
            _rewardedAd = null;
        }

        private void UnsubscribeInitializationEvents()
        {
            LevelPlay.OnInitSuccess -= HandleInitSuccess;
            LevelPlay.OnInitFailed -= HandleInitFailed;
        }

        private void ResetPendingReward()
        {
            _pendingReward = null;
            _rewardGranted = false;
        }

        private void CompleteRewardedSession(bool rewarded)
        {
            if (!_hasActiveSession)
            {
                return;
            }

            StopSessionCloseCoroutine();
            RewardedAdPurpose purpose = _activePurpose;
            _hasActiveSession = false;
            _activePurpose = default;

            if (!rewarded)
            {
                ResetPendingReward();
            }

            try
            {
                RewardedSessionEnded?.Invoke(purpose, rewarded);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void NotifyRewardedAvailabilityChanged()
        {
            RewardedAvailabilityChanged?.Invoke();
        }

        private static string GetPlacementName(RewardedAdPurpose purpose)
        {
            return purpose switch
            {
                RewardedAdPurpose.Revive => RevivePlacement,
                RewardedAdPurpose.RewardReroll => RewardRerollPlacement,
                RewardedAdPurpose.ExtraReward => ExtraRewardPlacement,
                RewardedAdPurpose.RewardDouble => RewardDoublePlacement,
                _ => string.Empty,
            };
        }
    }
}
