using System;

namespace VahTyah
{
    /// <summary>
    /// Default (no-op) provider used when no platform provider has been registered.
    /// Runs on Editor or unsupported platforms. ⚠️ Does NOT schedule any real OS notification.
    /// </summary>
    internal sealed class NotificationProviderDefault : INotificationProvider
    {
        public void Initialize() { }
        public void Schedule(string id, string title, string body, DateTime triggerTime) { }
        public void Cancel(string id) { }
        public void CancelAll() { }

        public bool RequestPermission(Action<bool> onResult)
        {
            onResult?.Invoke(true);
            return false;
        }

        public void OnAppOpened() { }
    }

    /// <summary>
    /// Factory that creates the notification provider. Platform providers (Android/iOS) register their creator via
    /// <see cref="Register"/> during SDK integration; otherwise the no-op default provider is returned.
    /// </summary>
    public static class NotificationProviderFactory
    {
        private static Func<ModuleNotifications, INotificationProvider> _creator;

        public static void Register(Func<ModuleNotifications, INotificationProvider> creator)
            => _creator = creator;

        internal static INotificationProvider Create(ModuleNotifications module)
            => (_creator != null ? _creator(module) : null) ?? new NotificationProviderDefault();
    }
}
