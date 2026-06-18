using System.Runtime.InteropServices;

namespace VahTyah
{
    /// <summary>Nguồn phát haptic theo nền tảng. Play trả thời lượng rung (ms) cho phát tuần tự.</summary>
    public interface IHapticProvider
    {
        int Play(HapticType type);
    }

    /// <summary>Điểm gắn provider iOS native tuỳ biến (nếu game muốn override provider mặc định).</summary>
    public static class HapticNativeRegistry
    {
        public static IHapticProvider IOSProvider { get; set; }
    }

    /// <summary>
    /// iOS native qua UIFeedbackGenerator (HapticFeedback.mm): Light/Medium/Heavy → UIImpactFeedback,
    /// Success/Warning/Failure → UINotificationFeedback. Editor no-op (symbol __Internal chỉ có trong build).
    /// </summary>
    public sealed class HapticProviderIOS : IHapticProvider
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void _impactOccurred(string style);
        [DllImport("__Internal")] private static extern void _notificationOccurred(string style);
#endif

        public int Play(HapticType type)
        {
#if UNITY_IOS && !UNITY_EDITOR
            switch (type)
            {
                case HapticType.Light: _impactOccurred("Light"); break;
                case HapticType.Medium: _impactOccurred("Medium"); break;
                case HapticType.Heavy: _impactOccurred("Heavy"); break;
                case HapticType.Success: _notificationOccurred("Success"); break;
                case HapticType.Warning: _notificationOccurred("Warning"); break;
                case HapticType.Failure: _notificationOccurred("Error"); break;
            }
#endif
            return 0;
        }
    }

    /// <summary>No-op (Editor / nền tảng không hỗ trợ).</summary>
    public sealed class HapticProviderDefault : IHapticProvider
    {
        public int Play(HapticType type) => 0;
    }
}
