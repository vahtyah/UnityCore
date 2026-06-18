using System;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Cấu hình module thông báo (kênh Android, lịch re-engagement) - có thể chỉnh qua Inspector / remote config.
    /// </summary>
    [Serializable]
    public class NotificationsData : ISAModuleData
    {
        [Tooltip("Notification channel ID registered with Android OS.")]
        public string AndroidChannelId = "sa_game_notifications";

        [Tooltip("Channel name shown in Android system settings.")]
        public string AndroidChannelName = "Game Notifications";

        [Tooltip("Channel description shown in Android system settings.")]
        public string AndroidChannelDescription = "In-game and re-engagement notifications.";

        [Range(1f, 10f)]
        [Tooltip("How many re-engagement notifications to schedule per session exit.")]
        public int MaxScheduled = 10;

        [Min(0f)]
        [Tooltip("Minimum hours before rescheduling re-engagement notifications.")]
        public float RescheduleCooldownHours = 1f;

        [Tooltip("Sent when the player has been away. Scheduled automatically on app pause/quit.")]
        public ReEngagementEntry[] ReEngagementNotifications = ModuleNotifications.CreateDefaultReEngagement();
    }

    /// <summary>
    /// Một mục thông báo re-engagement: hiển thị sau khi người chơi rời game một khoảng thời gian.
    /// </summary>
    [Serializable]
    public class ReEngagementEntry
    {
        public string Id;

        public string Title;

        [TextArea(1, 3)]
        public string Body;

        [Tooltip("Hours after app exit before this notification fires.")]
        [Min(0f)]
        public float DelayHours;
    }
}
