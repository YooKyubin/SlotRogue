using System;
using System.Collections;
using Unity.Notifications;
using UnityEngine;

namespace SlotRogue.UI.Notifications
{
    public sealed class LocalNotificationController : MonoBehaviour
    {
        public const string LastPlayUtcTicksKey =
            "slotrogue.notifications.last_play_utc_ticks";

        private const int InactivityNotificationId = 24001;
        private const int RankingDeadlineNotificationId = 24002;

        private static LocalNotificationController _instance;

        [Header("Schedule")]
        [SerializeField] private bool _notificationsEnabled = true;
        [SerializeField, Min(1f)] private float _inactivityHours = 24f;
        [SerializeField, Min(0f)] private float _rankingDeadlineLeadHours = 3f;

        [Header("Android Channel")]
        [SerializeField] private string _androidChannelId =
            "slotrogue_reminders";
        [SerializeField] private string _androidChannelName =
            "SlotRogue 알림";
        [SerializeField] private string _androidChannelDescription =
            "복귀 및 주간 랭킹 마감 알림";

        [Header("24 Hour Reminder")]
        [SerializeField] private string _inactivityTitle = "SlotRogue";
        [SerializeField] private string _inactivityText =
            "새로운 런이 기다리고 있어요. 다시 도전해 보세요!";

        [Header("Weekly Ranking Reminder")]
        [SerializeField] private string _rankingTitle =
            "주간 랭킹 마감 임박";
        [SerializeField] private string _rankingText =
            "주간 랭킹이 곧 마감돼요. 마지막 기록에 도전하세요!";

        private bool _initialized;
        private bool _permissionGranted;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (!_notificationsEnabled)
            {
                return;
            }

            var args = NotificationCenterArgs.Default;
            args.PresentationOptions =
                NotificationPresentation.Alert |
                NotificationPresentation.Sound |
                NotificationPresentation.Badge;
            args.AndroidChannelId = _androidChannelId;
            args.AndroidChannelName = _androidChannelName;
            args.AndroidChannelDescription = _androidChannelDescription;
            NotificationCenter.Initialize(args);
            _initialized = true;
        }

        private void Start()
        {
            if (_initialized)
            {
                StartCoroutine(RequestPermission());
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (!_initialized)
            {
                return;
            }

            if (isPaused)
            {
                ScheduleBackgroundNotifications(DateTime.UtcNow);
                return;
            }

            HandleForeground(DateTime.UtcNow);
        }

        private void OnApplicationQuit()
        {
            if (_initialized)
            {
                ScheduleBackgroundNotifications(DateTime.UtcNow);
            }
        }

        private IEnumerator RequestPermission()
        {
            NotificationsPermissionRequest request =
                NotificationCenter.RequestPermission();
            yield return request;

            _permissionGranted =
                request.Status == NotificationsPermissionStatus.Granted;
            if (_permissionGranted)
            {
                HandleForeground(DateTime.UtcNow);
            }
        }

        private void HandleForeground(DateTime utcNow)
        {
            CancelNotification(InactivityNotificationId);
            NotificationCenter.ClearBadge();

            if (_permissionGranted)
            {
                ScheduleRankingDeadline(utcNow);
            }
        }

        private void ScheduleBackgroundNotifications(DateTime utcNow)
        {
            RecordLastPlayUtc(utcNow);
            if (!_permissionGranted)
            {
                return;
            }

            ScheduleInactivityReminder();
            ScheduleRankingDeadline(utcNow);
        }

        private void ScheduleInactivityReminder()
        {
            CancelNotification(InactivityNotificationId);

            var notification = new Notification
            {
                Identifier = InactivityNotificationId,
                Title = _inactivityTitle,
                Text = _inactivityText,
                Data = "inactivity_reminder",
                ShowInForeground = false,
            };
            var schedule = new NotificationIntervalSchedule(
                TimeSpan.FromHours(Math.Max(1f, _inactivityHours)));
            NotificationCenter.ScheduleNotification(notification, schedule);
        }

        private void ScheduleRankingDeadline(DateTime utcNow)
        {
            CancelNotification(RankingDeadlineNotificationId);

            DateTime deadlineUtc = WeeklyRankingSchedule.GetNextDeadlineUtc(
                utcNow,
                TimeSpan.FromHours(Math.Max(0f, _rankingDeadlineLeadHours)));
            var notification = new Notification
            {
                Identifier = RankingDeadlineNotificationId,
                Title = _rankingTitle,
                Text = _rankingText,
                Data = "weekly_ranking_deadline",
                ShowInForeground = false,
            };
            var schedule = new NotificationDateTimeSchedule(
                deadlineUtc.ToLocalTime());
            NotificationCenter.ScheduleNotification(notification, schedule);
        }

        private static void CancelNotification(int notificationId)
        {
            NotificationCenter.CancelScheduledNotification(notificationId);
            NotificationCenter.CancelDeliveredNotification(notificationId);
        }

        private static void RecordLastPlayUtc(DateTime utcNow)
        {
            long ticks = utcNow.Kind == DateTimeKind.Utc
                ? utcNow.Ticks
                : utcNow.ToUniversalTime().Ticks;
            PlayerPrefs.SetString(
                LastPlayUtcTicksKey,
                ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }
    }
}
