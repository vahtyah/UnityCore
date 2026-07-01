using Cysharp.Threading.Tasks;

namespace VahTyah
{
    /// <summary>Shortcut tĩnh publish event haptic (gõ gọn, không cần biết ModuleHaptic).</summary>
    public static class Haptic
    {
        public static void Play(HapticType type, bool force = false)
            => EventBus.Publish(new HapticPlay { Type = type, Force = force }).Forget();

        public static void PlaySequence(bool force, params HapticType[] types)
            => EventBus.Publish(new HapticSequence { Types = types, Force = force }).Forget();

        public static void SetActive(bool active)
            => EventBus.Publish(new HapticSetActive { Active = active }).Forget();
    }
}
