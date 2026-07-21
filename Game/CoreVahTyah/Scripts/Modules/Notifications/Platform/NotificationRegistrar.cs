using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Registers the platform notification provider into NotificationProviderFactory.
    /// Runs [BeforeSceneLoad] → before Bootstrap initializes ModuleNotifications, so the provider is ready in time.
    /// Editor → registers nothing → the factory falls back to Default (no-op).
    /// </summary>
    internal static class NotificationRegistrar
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            NotificationProviderFactory.Register(module =>
            {
#if UNITY_ANDROID && !UNITY_EDITOR && VAHTYAH_MOBILE_NOTIFICATIONS
                return new AndroidNotificationProvider(
                    module.AndroidChannelId, module.AndroidChannelName, module.AndroidChannelDescription, module.AndroidSmallIcon);
#elif UNITY_IOS && !UNITY_EDITOR && VAHTYAH_MOBILE_NOTIFICATIONS
                return new IOSNotificationProvider();
#else
                // Editor, other platforms, OR package not installed (define not set) → factory falls back to Default (no-op).
                return null;
#endif
            });
        }
    }
}
