using System;

namespace StandardAssets
{
    /// <summary>
    /// Provider mặc định (no-op) khi không có provider nền tảng nào được đăng ký.
    /// Dùng trên Editor hoặc nền tảng không hỗ trợ thông báo.
    /// </summary>
    internal sealed class NotificationProviderDefault : INotificationProvider
    {
        public void Initialize()
        {
        }

        public void Schedule(string id, string title, string body, DateTime triggerTime)
        {
        }

        public void Cancel(string id)
        {
        }

        public void CancelAll()
        {
        }

        public bool RequestPermission(Action<bool> onResult)
        {
            onResult?.Invoke(true);
            return false;
        }

        public void OnAppOpened()
        {
        }
    }

    /// <summary>
    /// Factory tạo provider thông báo. Provider nền tảng tự đăng ký creator qua <see cref="Register"/>;
    /// nếu không có, sẽ trả về provider mặc định.
    /// </summary>
    public static class NotificationProviderFactory
    {
        private static Func<ModuleNotifications, INotificationProvider> _creator;

        internal static void Register(Func<ModuleNotifications, INotificationProvider> creator)
        {
            _creator = creator;
        }

        internal static INotificationProvider Create(ModuleNotifications module)
        {
            if (_creator == null)
            {
                return new NotificationProviderDefault();
            }
            return _creator(module);
        }
    }
}
