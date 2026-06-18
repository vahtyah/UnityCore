using System;

namespace StandardAssets
{
    /// <summary>
    /// Trừu tượng hóa nhà cung cấp thông báo (notification) theo nền tảng (Android/iOS/Default).
    /// Runtime module chỉ làm việc qua interface này, không gọi trực tiếp SDK nền tảng.
    /// </summary>
    public interface INotificationProvider
    {
        void Initialize();

        void Schedule(string id, string title, string body, DateTime triggerTime);

        void Cancel(string id);

        void CancelAll();

        bool RequestPermission(Action<bool> onResult);

        void OnAppOpened();
    }
}
