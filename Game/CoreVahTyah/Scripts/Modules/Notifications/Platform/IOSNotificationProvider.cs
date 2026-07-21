#if UNITY_IOS && !UNITY_EDITOR && VAHTYAH_MOBILE_NOTIFICATIONS
using System;
using Cysharp.Threading.Tasks;
using Unity.Notifications.iOS;

namespace VahTyah
{
    /// <summary>
    /// iOS provider wrapping Unity Mobile Notifications. iOS uses a native string identifier → maps straight to our Id.
    /// </summary>
    internal sealed class IOSNotificationProvider : INotificationProvider
    {
        public void Initialize() { }

        public void Schedule(string id, string title, string body, DateTime triggerTime)
        {
            TimeSpan interval = triggerTime - DateTime.Now;
            if (interval.TotalSeconds < 1) interval = TimeSpan.FromSeconds(1); // iOS requires > 0

            var notification = new iOSNotification
            {
                Identifier = id,
                Title = title,
                Body = body,
                ShowInForeground = false,
                Trigger = new iOSNotificationTimeIntervalTrigger
                {
                    TimeInterval = interval,
                    Repeats = false
                }
            };
            iOSNotificationCenter.ScheduleNotification(notification);
        }

        public void Cancel(string id) => iOSNotificationCenter.RemoveScheduledNotification(id);

        public void CancelAll() => iOSNotificationCenter.RemoveAllScheduledNotifications();

        public bool RequestPermission(Action<bool> onResult)
        {
            RequestAsync(onResult).Forget();
            return true; // async
        }

        private static async UniTaskVoid RequestAsync(Action<bool> onResult)
        {
            var options = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
            using (var request = new AuthorizationRequest(options, true))
            {
                while (!request.IsFinished) await UniTask.Yield();
                onResult?.Invoke(request.Granted);
            }
        }

        public void OnAppOpened()
        {
            iOSNotificationCenter.ApplicationBadge = 0;
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
        }
    }
}
#endif
