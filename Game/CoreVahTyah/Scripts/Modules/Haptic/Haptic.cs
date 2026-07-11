using Cysharp.Threading.Tasks;

namespace VahTyah
{
    /// <summary>Shortcut tĩnh phát haptic — gọi thẳng <see cref="HapticService"/> (không qua EventBus).</summary>
    public static class Haptic
    {
        public static void Play(HapticType type, bool force = false)
            => Services.Get<HapticService>()?.Play(type, force);

        public static UniTask PlaySequence(bool force, params HapticType[] types)
        {
            var service = Services.Get<HapticService>();
            return service != null ? service.PlaySequence(force, types) : UniTask.CompletedTask;
        }
    }
}
