using System;

namespace VahTyah
{
    /// <summary>
    /// Abstracts the notification provider per platform (Android/iOS/Default).
    /// The runtime module works only through this interface; it never calls a platform SDK directly.
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
