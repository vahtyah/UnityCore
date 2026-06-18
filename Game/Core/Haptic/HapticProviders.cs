using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Interface cho một nguồn phát haptic theo nền tảng.
    /// Play trả về thời lượng rung ước tính (ms) để dùng cho phát tuần tự.
    /// </summary>
    public interface IHapticProvider
    {
        int Play(HapticType type);
    }

    /// <summary>
    /// Điểm đăng ký provider iOS native (gán từ code native iOS nếu có).
    /// </summary>
    public static class HapticNativeRegistry
    {
        public static IHapticProvider IOSProvider { get; set; }
    }

    /// <summary>Provider iOS mặc định (no-op, thường được thay bằng native).</summary>
    internal sealed class HapticProviderIOS : IHapticProvider
    {
        public int Play(HapticType type)
        {
            return 0;
        }
    }

    /// <summary>Provider không làm gì (nền tảng không hỗ trợ haptic).</summary>
    internal sealed class HapticProviderDefault : IHapticProvider
    {
        public int Play(HapticType type)
        {
            return 0;
        }
    }

    /// <summary>
    /// Provider Android: rung qua Vibrator service, scale theo intensity.
    /// Dùng pattern cho Success/Warning/Failure, thời lượng đơn cho Light/Medium/Heavy.
    /// </summary>
    internal sealed class HapticProviderAndroid : IHapticProvider
    {
        private static readonly long[] PatternSuccess = new long[4] { 0L, 30L, 60L, 60L };

        private static readonly long[] PatternWarning = new long[4] { 0L, 60L, 40L, 60L };

        private static readonly long[] PatternFailure = new long[6] { 0L, 100L, 30L, 40L, 30L, 40L };

        private readonly float _intensity;

        internal HapticProviderAndroid(float intensity)
        {
            _intensity = Mathf.Max(0f, intensity);
        }

        public int Play(HapticType type)
        {
            if (_intensity <= 0f)
            {
                return 0;
            }
            using AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", new object[1] { "vibrator" });

            long[] pattern = type switch
            {
                HapticType.Success => PatternSuccess,
                HapticType.Warning => PatternWarning,
                HapticType.Failure => PatternFailure,
                _ => null,
            };

            if (pattern != null)
            {
                // Scale toàn bộ pattern theo intensity và rung không lặp (-1)
                long[] scaled = new long[pattern.Length];
                for (int i = 0; i < pattern.Length; i++)
                {
                    scaled[i] = (long)((float)pattern[i] * _intensity);
                }
                vibrator.Call("vibrate", new object[2] { scaled, -1 });
                int total = 0;
                foreach (long v in scaled)
                {
                    total += (int)v;
                }
                return total;
            }

            int baseMs = type switch
            {
                HapticType.Light => 20,
                HapticType.Medium => 40,
                HapticType.Heavy => 80,
                _ => 40,
            };
            int durationMs = Mathf.Max(1, Mathf.RoundToInt((float)baseMs * _intensity));
            vibrator.Call("vibrate", new object[1] { (long)durationMs });
            return durationMs;
        }
    }
}
