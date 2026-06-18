using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Android Vibrator, SDK-aware: ≥26 dùng VibrationEffect (createOneShot/createWaveform có amplitude),
    /// cũ hơn fallback vibrate(long)/vibrate(long[],-1). Scale theo intensity.
    /// </summary>
    public sealed class HapticProviderAndroid : IHapticProvider
    {
        private static readonly long[] T_Success = { 0, 30, 40, 60 };
        private static readonly int[] A_Success = { 0, 150, 0, 200 };
        private static readonly long[] T_Warning = { 0, 50, 40, 60 };
        private static readonly int[] A_Warning = { 0, 180, 0, 180 };
        private static readonly long[] T_Failure = { 0, 80, 40, 40, 40, 60 };
        private static readonly int[] A_Failure = { 0, 255, 0, 200, 0, 200 };

        private readonly float _intensity;
        private AndroidJavaObject _vibrator;
        private int _sdk;
        private bool _ready;

        public HapticProviderAndroid(float intensity)
        {
            _intensity = Mathf.Clamp(intensity, 0f, 2f);
            Init();
        }

        private void Init()
        {
#if UNITY_ANDROID
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

                using var ver = new AndroidJavaClass("android.os.Build$VERSION");
                _sdk = ver.GetStatic<int>("SDK_INT");

                _ready = _vibrator != null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Haptic] Android init failed: {e.Message}");
            }
#endif
        }

        public int Play(HapticType type)
        {
            if (!_ready || _intensity <= 0f) return 0;

#if UNITY_ANDROID
            try
            {
                switch (type)
                {
                    case HapticType.Success: return Waveform(T_Success, A_Success);
                    case HapticType.Warning: return Waveform(T_Warning, A_Warning);
                    case HapticType.Failure: return Waveform(T_Failure, A_Failure);
                    default: return OneShot(type);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Haptic] play failed: {e.Message}");
            }
#endif
            return 0;
        }

#if UNITY_ANDROID
        private int OneShot(HapticType type)
        {
            int ms = type switch { HapticType.Light => 15, HapticType.Medium => 30, HapticType.Heavy => 60, _ => 30 };
            int amp = type switch { HapticType.Light => 80, HapticType.Medium => 150, HapticType.Heavy => 255, _ => 150 };
            amp = Mathf.Clamp(Mathf.RoundToInt(amp * _intensity), 1, 255);

            if (_sdk >= 26)
            {
                using var fx = new AndroidJavaClass("android.os.VibrationEffect");
                var eff = fx.CallStatic<AndroidJavaObject>("createOneShot", (long)ms, amp);
                _vibrator.Call("vibrate", eff);
            }
            else
            {
                _vibrator.Call("vibrate", (long)ms);
            }
            return ms;
        }

        private int Waveform(long[] timings, int[] amps)
        {
            int total = 0;
            for (int i = 0; i < timings.Length; i++) total += (int)timings[i];

            if (_sdk >= 26)
            {
                int[] scaled = new int[amps.Length];
                for (int i = 0; i < amps.Length; i++)
                    scaled[i] = amps[i] == 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(amps[i] * _intensity), 1, 255);

                using var fx = new AndroidJavaClass("android.os.VibrationEffect");
                var eff = fx.CallStatic<AndroidJavaObject>("createWaveform", timings, scaled, -1);
                _vibrator.Call("vibrate", eff);
            }
            else
            {
                _vibrator.Call("vibrate", timings, -1);
            }
            return total;
        }
#endif
    }
}
