using System;

namespace VahTyah
{
    /// <summary>Schedules one local notification. TriggerTime takes priority; if default → use DelaySeconds from now.</summary>
    public struct NotificationSchedule : IEvent
    {
        public string Id;
        public string Title;
        public string Body;
        public DateTime TriggerTime;
        public float DelaySeconds;
    }

    /// <summary>Cancels one notification by Id.</summary>
    public struct NotificationCancel : IEvent { public string Id; }

    /// <summary>Cancels all scheduled notifications.</summary>
    public struct NotificationCancelAll : IEvent { }
}
