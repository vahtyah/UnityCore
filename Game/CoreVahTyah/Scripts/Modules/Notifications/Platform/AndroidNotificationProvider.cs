#if UNITY_ANDROID && !UNITY_EDITOR && VAHTYAH_MOBILE_NOTIFICATIONS
using System;
using Cysharp.Threading.Tasks;
using Unity.Notifications.Android;

namespace VahTyah
{
    /// <summary>
    /// Android provider wrapping Unity Mobile Notifications (com.unity.mobile.notifications).
    /// Android uses int ids → maps string Id ↔ int with a stable hash (not string.GetHashCode).
    /// </summary>
    internal sealed class AndroidNotificationProvider : INotificationProvider
    {
        private readonly string _channelId;
        private readonly string _channelName;
        private readonly string _channelDesc;
        private readonly string _smallIcon;

        public AndroidNotificationProvider(string channelId, string channelName, string channelDesc, string smallIcon)
        {
            _channelId = channelId;
            _channelName = channelName;
            _channelDesc = channelDesc;
            _smallIcon = smallIcon;
        }

        public void Initialize()
        {
            var channel = new AndroidNotificationChannel
            {
                Id = _channelId,
                Name = _channelName,
                Description = _channelDesc,
                Importance = Importance.Default
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);
        }

        public void Schedule(string id, string title, string body, DateTime triggerTime)
        {
            var notification = new AndroidNotification
            {
                Title = title,
                Text = body,
                FireTime = triggerTime
            };
            // Small icon comes from ModuleNotifications._androidSmallIcon (an identifier declared in Project Settings →
            // Mobile Notifications). Leave it empty → default app icon; some devices will then show NO notification.
            if (!string.IsNullOrEmpty(_smallIcon)) notification.SmallIcon = _smallIcon;
            AndroidNotificationCenter.SendNotificationWithExplicitID(notification, _channelId, StableId(id));
        }

        public void Cancel(string id) => AndroidNotificationCenter.CancelNotification(StableId(id));

        public void CancelAll() => AndroidNotificationCenter.CancelAllNotifications();

        public bool RequestPermission(Action<bool> onResult)
        {
            RequestAsync(onResult).Forget();
            return true; // async
        }

        // Android 13+ (API 33) needs the runtime POST_NOTIFICATIONS permission. Older versions are treated as granted.
        private static async UniTaskVoid RequestAsync(Action<bool> onResult)
        {
            var request = new PermissionRequest();
            while (request.Status == PermissionStatus.RequestPending)
                await UniTask.Yield();

            bool granted = request.Status == PermissionStatus.Allowed
                        || request.Status == PermissionStatus.NotRequested;
            onResult?.Invoke(granted);
        }

        public void OnAppOpened() => AndroidNotificationCenter.CancelAllDisplayedNotifications();

        // Stable string Id → positive int. Not string.GetHashCode (differs across runs/platforms).
        private static int StableId(string s)
        {
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < s.Length; i++) hash = hash * 31 + s[i];
                return hash & 0x7fffffff;
            }
        }
    }
}
#endif
