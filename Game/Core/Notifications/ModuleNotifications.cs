using System;
using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Module quản lý thông báo: lên lịch / hủy thông báo và tự động lên lịch re-engagement khi người chơi rời game.
    /// Giao tiếp qua SATypedBus và trừu tượng hóa nền tảng qua <see cref="INotificationProvider"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/Notifications", fileName = "Module_Notifications", order = 14)]
    internal sealed class ModuleNotifications : SAModule, ISARemoteConfig
    {
        private INotificationProvider _provider;

        private NotificationsData _runtimeData;

        private const string PrefLastPlay = "SA.Notif.LastPlay";

        private const string PrefLastScheduled = "SA.Notif.LastScheduled";

        [Header("Android")]
        [Tooltip("Notification channel ID registered with Android OS.")]
        public string AndroidChannelId = "sa_game_notifications";

        [Tooltip("Channel name shown in Android system settings.")]
        public string AndroidChannelName = "Game Notifications";

        [Tooltip("Channel description shown in Android system settings.")]
        public string AndroidChannelDescription = "In-game and re-engagement notifications.";

        [Header("Re-engagement Notifications")]
        [Tooltip("Sent when the player has been away. Scheduled automatically on app pause/quit.")]
        public ReEngagementEntry[] ReEngagementNotifications = CreateDefaultReEngagement();

        [Header("Smart Scheduling")]
        [Range(1f, 10f)]
        [Tooltip("How many re-engagement notifications to schedule per session exit.")]
        public int MaxScheduled = 10;

        [Min(0f)]
        [Tooltip("Minimum hours that must pass before rescheduling re-engagement notifications.")]
        public float RescheduleCooldownHours = 1f;

        public string RemoteConfigKey => "notifications_config";

        public override Task InitializeAsync()
        {
            _runtimeData = SADataProvider.Resolve("notifications_config", GetEditorData());
            AndroidChannelId = _runtimeData.AndroidChannelId;
            AndroidChannelName = _runtimeData.AndroidChannelName;
            AndroidChannelDescription = _runtimeData.AndroidChannelDescription;
            MaxScheduled = _runtimeData.MaxScheduled;
            RescheduleCooldownHours = _runtimeData.RescheduleCooldownHours;
            ReEngagementNotifications = _runtimeData.ReEngagementNotifications;
            _provider = NotificationProviderFactory.Create(this);
            if (_provider == null)
            {
                Debug.LogWarning("[SA.Notifications] Provider not found.");
                return Task.CompletedTask;
            }
            _provider.Initialize();
            _provider.RequestPermission(OnPermissionResult);
            // Khi mở app: hủy các thông báo re-engagement đã lên lịch và đánh dấu thời điểm chơi
            CancelReEngagement();
            SaveLastPlayTime();
            _provider.OnAppOpened();
            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            SATypedBus.On<Ev.NotificationSchedule>(OnScheduleCommand);
            SATypedBus.On<Ev.NotificationCancel>(OnCancelCommand);
            SATypedBus.On<Ev.NotificationCancelAll>(OnCancelAllCommand);
            SATypedBus.On<Ev.AppPaused>(OnAppPause);
            SATypedBus.On<Ev.AppResumed>(OnAppResume);
            SATypedBus.On<Ev.AppQuitting>(OnAppQuit);
        }

        public string GetEditorJson()
        {
            return SADataProvider.ToJson(GetEditorData());
        }

        public NotificationsData GetEditorData()
        {
            return new NotificationsData
            {
                AndroidChannelId = AndroidChannelId,
                AndroidChannelName = AndroidChannelName,
                AndroidChannelDescription = AndroidChannelDescription,
                MaxScheduled = MaxScheduled,
                RescheduleCooldownHours = RescheduleCooldownHours,
                ReEngagementNotifications = ReEngagementNotifications
            };
        }

        private void OnScheduleCommand(Ev.NotificationSchedule e)
        {
            if (_provider != null)
            {
                string id = e.Id;
                string title = e.Title;
                string body = e.Body;
                DateTime dateTime = e.TriggerTime;
                DateTime triggerTime;
                if (dateTime != default(DateTime))
                {
                    triggerTime = dateTime;
                }
                else
                {
                    float num = e.DelaySeconds;
                    triggerTime = DateTime.Now.AddSeconds(num);
                }
                _provider.Schedule(id, title, body, triggerTime);
            }
        }

        private void OnCancelCommand(Ev.NotificationCancel e)
        {
            if (_provider != null)
            {
                _provider.Cancel(e.Id);
            }
        }

        private void OnCancelAllCommand(Ev.NotificationCancelAll e)
        {
            if (_provider != null)
            {
                _provider.CancelAll();
            }
        }

        private void OnAppPause(Ev.AppPaused e)
        {
            SaveLastPlayTime();
            ScheduleReEngagement();
        }

        private void OnAppResume(Ev.AppResumed e)
        {
            CancelReEngagement();
            SaveLastPlayTime();
        }

        private void OnAppQuit(Ev.AppQuitting e)
        {
            SaveLastPlayTime();
            ScheduleReEngagement();
        }

        /// <summary>
        /// Lên lịch các thông báo re-engagement khi người chơi rời game, có cơ chế cooldown chống lên lịch lại liên tục.
        /// </summary>
        private void ScheduleReEngagement()
        {
            if (_provider == null || ReEngagementNotifications == null || ReEngagementNotifications.Length == 0)
            {
                return;
            }
            // Bỏ qua nếu vừa lên lịch trong khoảng cooldown
            if (PlayerPrefs.HasKey(PrefLastScheduled))
            {
                long dateData = long.Parse(PlayerPrefs.GetString(PrefLastScheduled));
                DateTime dateTime = DateTime.FromBinary(dateData).ToUniversalTime();
                if ((DateTime.UtcNow - dateTime).TotalHours < (double)RescheduleCooldownHours)
                {
                    return;
                }
            }
            int num = 0;
            ReEngagementEntry[] reEngagementNotifications = ReEngagementNotifications;
            foreach (ReEngagementEntry reEngagementEntry in reEngagementNotifications)
            {
                if (num >= MaxScheduled)
                {
                    break;
                }
                if (!string.IsNullOrEmpty(reEngagementEntry.Id))
                {
                    _provider.Schedule(reEngagementEntry.Id, reEngagementEntry.Title, reEngagementEntry.Body, DateTime.Now.AddHours(reEngagementEntry.DelayHours));
                    num++;
                }
            }
            PlayerPrefs.SetString(PrefLastScheduled, DateTime.UtcNow.ToBinary().ToString());
            PlayerPrefs.Save();
        }

        private void CancelReEngagement()
        {
            if (_provider != null && ReEngagementNotifications != null)
            {
                ReEngagementEntry[] reEngagementNotifications = ReEngagementNotifications;
                foreach (ReEngagementEntry reEngagementEntry in reEngagementNotifications)
                {
                    _provider.Cancel(reEngagementEntry.Id);
                }
                PlayerPrefs.DeleteKey(PrefLastScheduled);
                PlayerPrefs.Save();
            }
        }

        private static void SaveLastPlayTime()
        {
            PlayerPrefs.SetString(PrefLastPlay, DateTime.UtcNow.ToBinary().ToString());
            PlayerPrefs.Save();
        }

        private static void OnPermissionResult(bool granted)
        {
            if (!granted)
            {
                Debug.LogWarning("[SA.Notifications] Notification permission denied by user.");
            }
        }

        /// <summary>
        /// Tạo danh sách mặc định 10 thông báo re-engagement (dùng chung cho module và NotificationsData).
        /// </summary>
        internal static ReEngagementEntry[] CreateDefaultReEngagement()
        {
            return new ReEngagementEntry[10]
            {
                new ReEngagementEntry { Id = "reengagement_0", Title = "We miss you!", Body = "Your adventure is waiting — come back and play!", DelayHours = 2f },
                new ReEngagementEntry { Id = "reengagement_1", Title = "Don't stop now!", Body = "Big rewards are ready for you. Come claim them!", DelayHours = 23f },
                new ReEngagementEntry { Id = "reengagement_2", Title = "Still there?", Body = "A special bonus is waiting just for you.", DelayHours = 47f },
                new ReEngagementEntry { Id = "reengagement_3", Title = "Your adventure awaits!", Body = "Three days gone — your next quest is ready and waiting.", DelayHours = 71f },
                new ReEngagementEntry { Id = "reengagement_4", Title = "Your team needs you!", Body = "Five days away? Jump back in and pick up where you left off!", DelayHours = 119f },
                new ReEngagementEntry { Id = "reengagement_5", Title = "Time to come back!", Body = "It's been a week — your rewards are piling up!", DelayHours = 167f },
                new ReEngagementEntry { Id = "reengagement_6", Title = "We saved your spot!", Body = "Ten days away — your adventure is still here waiting for you.", DelayHours = 239f },
                new ReEngagementEntry { Id = "reengagement_7", Title = "One more chance!", Body = "Two weeks gone — come back and claim your comeback reward!", DelayHours = 335f },
                new ReEngagementEntry { Id = "reengagement_8", Title = "We haven't forgotten!", Body = "Three weeks away? A special gift is waiting just for you.", DelayHours = 503f },
                new ReEngagementEntry { Id = "reengagement_9", Title = "Last call!", Body = "A whole month gone — your ultimate comeback reward is ready!", DelayHours = 671f }
            };
        }
    }
}
