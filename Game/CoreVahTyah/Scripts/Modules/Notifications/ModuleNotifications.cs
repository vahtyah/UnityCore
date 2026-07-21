using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    /// <summary>
    /// Schedules / cancels local notifications and AUTOMATICALLY schedules a re-engagement chain when the
    /// player leaves the game (listens to AppPaused/AppQuitting), cancelling it on return (AppResumed / boot).
    /// Platform is abstracted behind <see cref="INotificationProvider"/>.
    ///
    /// ⚠️ NOT RELEASE-READY BY ITSELF. The default provider is a NO-OP — scheduling/cooldown logic runs
    /// correctly but NO real OS notification fires until a platform provider is registered. Real providers live
    /// in Platform/ and are gated behind the VAHTYAH_MOBILE_NOTIFICATIONS define (added by the Install button).
    /// See Docs/MODULES.md → ModuleNotifications.
    ///
    /// Config is read straight from the asset fields. Once RemoteConfig exists it can overlay these (as the old
    /// Core module did via SADataProvider).
    /// </summary>
    [CreateAssetMenu(menuName = "VahTyah/Modules/Notifications", fileName = "Module_Notifications")]
    [ModuleRequires(typeof(ModuleSave))]
    public sealed class ModuleNotifications : Module
    {
        /// <summary>One re-engagement entry: shown after the player has been away for a while.</summary>
        [Serializable]
        public sealed class ReEngagementEntry
        {
            public string Id;
            public string Title;
            [TextArea(1, 3)] public string Body;
            [Min(0f)] [Tooltip("Hours after the player leaves before this notification fires.")]
            public float DelayHours;
        }

        /// <summary>An Android drawable icon to register into Project Settings → Mobile Notifications.</summary>
        [Serializable]
        public sealed class NotificationIcon
        {
            [Tooltip("Identifier used to reference this icon (e.g. \"icon_small\"). The small icon used at " +
                     "runtime is picked by 'Android Small Icon Id' below and must match one Small entry here.")]
            public string Id;
            [Tooltip("Small = status-bar icon (REQUIRED on Android; must be white/transparent silhouette). " +
                     "Large = optional, shown inside the expanded notification.")]
            public bool IsSmall = true;
            public Texture2D Texture;
        }

        [BoxGroup("Android")]
        [Tooltip("Notification channel ID registered with the Android OS.")]
        [SerializeField] private string _androidChannelId = "game_notifications";
        [BoxGroup("Android")]
        [Tooltip("Channel name shown in the Android system settings.")]
        [SerializeField] private string _androidChannelName = "Game Notifications";
        [BoxGroup("Android")]
        [Tooltip("Channel description shown in the Android system settings.")]
        [SerializeField] private string _androidChannelDescription = "In-game and re-engagement notifications.";
        [BoxGroup("Android")]
        [SmallIconId(nameof(_androidIcons))]
        [Tooltip("Which registered Small icon to stamp on every notification (pick one from the icons below). " +
                 "Leave on Auto → if exactly one Small icon is registered it is used automatically; otherwise the " +
                 "app icon is used and some Android devices then show NO notification.")]
        [SerializeField] private string _androidSmallIconId = "";

        [BoxGroup("Android Icons")]
        [Tooltip("Icons registered into Project Settings → Mobile Notifications by the 'Setup Notification Icons' button.")]
        [SerializeField] private NotificationIcon[] _androidIcons = Array.Empty<NotificationIcon>();

        [BoxGroup("Re-engagement")]
        [Tooltip("Fired while the player is away. Scheduled automatically on app pause/quit.")]
        [SerializeField] private ReEngagementEntry[] _reEngagement = CreateDefaultReEngagement();

        [BoxGroup("Smart Scheduling")]
        [Range(1, 10)] [Tooltip("How many re-engagement notifications to schedule per session exit.")]
        [SerializeField] private int _maxScheduled = 10;
        [BoxGroup("Smart Scheduling")]
        [Min(0f)] [Tooltip("Minimum hours before re-engagement may be re-scheduled (prevents spam).")]
        [SerializeField] private float _rescheduleCooldownHours = 1f;

        private const string SaveKey = "notifications";

        private INotificationProvider _provider;
        private NotificationsSaveData _save;

        // Exposed to the platform provider (see Platform/).
        public string AndroidChannelId => _androidChannelId;
        public string AndroidChannelName => _androidChannelName;
        public string AndroidChannelDescription => _androidChannelDescription;
        public string AndroidSmallIcon => ResolveAndroidSmallIconId();

        /// <summary>
        /// Resolves which registered Small icon Id to stamp on notifications:
        /// an explicit selection wins; otherwise, if EXACTLY one Small icon is registered, auto-use it;
        /// otherwise return null so the provider leaves the OS default (app icon) — the fallback case.
        /// </summary>
        private string ResolveAndroidSmallIconId()
        {
            if (!string.IsNullOrEmpty(_androidSmallIconId)) return _androidSmallIconId;

            if (_androidIcons == null) return null;

            string only = null;
            int count = 0;
            foreach (var icon in _androidIcons)
            {
                if (icon == null || !icon.IsSmall || string.IsNullOrEmpty(icon.Id)) continue;
                only = icon.Id;
                count++;
            }
            return count == 1 ? only : null;
        }

#if UNITY_EDITOR
#if !VAHTYAH_MOBILE_NOTIFICATIONS
        // Shown only until the package is installed; once the Install button adds the
        // VAHTYAH_MOBILE_NOTIFICATIONS define, this method compiles out and the button disappears.
        [Button("Install Unity Mobile Notifications", 0)]
        private void InstallMobileNotificationsPackage() => NotificationEditorSetup.InstallPackage();
#endif

        [Button("Setup Notification Icons", 1)]
        private void SetupNotificationIcons() => NotificationEditorSetup.SetupAndroidIcons(_androidIcons);
#endif

        public override UniTask InitializeAsync(Transform holder)
        {
            _save = Services.Get<SaveService>().Load<NotificationsSaveData>(SaveKey);

            _provider = NotificationProviderFactory.Create(this);
            _provider.Initialize();
            _provider.RequestPermission(OnPermissionResult);

            // App opened: cancel any pending re-engagement + mark the play timestamp.
            CancelReEngagement();
            SaveLastPlayTime();
            Persist();
            _provider.OnAppOpened();
            return UniTask.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<NotificationSchedule>(OnSchedule);
            EventBus.On<NotificationCancel>(OnCancel);
            EventBus.On<NotificationCancelAll>(OnCancelAll);
            EventBus.On<AppPaused>(OnAppPause);
            EventBus.On<AppResumed>(OnAppResume);
            EventBus.On<AppQuitting>(OnAppQuit);
        }

        private void OnSchedule(NotificationSchedule e)
        {
            DateTime trigger = e.TriggerTime != default ? e.TriggerTime : DateTime.Now.AddSeconds(e.DelaySeconds);
            _provider.Schedule(e.Id, e.Title, e.Body, trigger);
        }

        private void OnCancel(NotificationCancel e) => _provider.Cancel(e.Id);
        private void OnCancelAll(NotificationCancelAll e) => _provider.CancelAll();

        private void OnAppPause(AppPaused e)
        {
            SaveLastPlayTime();
            ScheduleReEngagement();
            PersistImmediate(); // process may be killed in the background → flush now
        }

        private void OnAppResume(AppResumed e)
        {
            CancelReEngagement();
            SaveLastPlayTime();
            Persist();
        }

        private void OnAppQuit(AppQuitting e)
        {
            SaveLastPlayTime();
            ScheduleReEngagement();
            PersistImmediate();
        }

        /// <summary>Schedules the re-engagement chain, with a cooldown that prevents repeated re-scheduling.</summary>
        private void ScheduleReEngagement()
        {
            if (_reEngagement == null || _reEngagement.Length == 0) return;

            if (_save.LastScheduledBinary != 0)
            {
                DateTime last = DateTime.FromBinary(_save.LastScheduledBinary).ToUniversalTime();
                if ((DateTime.UtcNow - last).TotalHours < _rescheduleCooldownHours) return;
            }

            int count = 0;
            foreach (var entry in _reEngagement)
            {
                if (count >= _maxScheduled) break;
                if (string.IsNullOrEmpty(entry.Id)) continue;

                _provider.Schedule(entry.Id, entry.Title, entry.Body, DateTime.Now.AddHours(entry.DelayHours));
                count++;
            }

            _save.LastScheduledBinary = DateTime.UtcNow.ToBinary();
        }

        private void CancelReEngagement()
        {
            if (_reEngagement == null) return;

            foreach (var entry in _reEngagement)
            {
                if (!string.IsNullOrEmpty(entry.Id)) _provider.Cancel(entry.Id);
            }
            _save.LastScheduledBinary = 0;
        }

        private void SaveLastPlayTime() => _save.LastPlayBinary = DateTime.UtcNow.ToBinary();

        private void Persist() => Services.Get<SaveService>().Set(SaveKey, _save);

        private void PersistImmediate()
        {
            var save = Services.Get<SaveService>();
            save.Set(SaveKey, _save);
            save.SaveAllImmediate();
        }

        private static void OnPermissionResult(bool granted)
        {
            if (!granted) Debug.LogWarning("[Notifications] Notification permission denied by user.");
        }

        /// <summary>Default 10 re-engagement notifications (2h → ~28 days).</summary>
        public static ReEngagementEntry[] CreateDefaultReEngagement()
        {
            return new[]
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
