using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Android Vibrator, SDK-aware: ≥26 dùng VibrationEffect (createOneShot/createWaveform có amplitude),
    /// cũ hơn fallback vibrate(long)/vibrate(long[],-1). Scale theo intensity.
    /// </summary>
    public sealed class HapticProviderAndroid : IHapticProvider, IHapticTunable
    {
        private static readonly long[] T_Success = { 0, 30, 40, 60 };
        private static readonly int[] A_Success = { 0, 150, 0, 200 };
        private static readonly long[] T_Warning = { 0, 50, 40, 60 };
        private static readonly int[] A_Warning = { 0, 180, 0, 180 };
        private static readonly long[] T_Failure = { 0, 80, 40, 40, 40, 60 };
        private static readonly int[] A_Failure = { 0, 255, 0, 200, 0, 200 };

        private readonly float _intensity;
        private HapticOneShot _light, _medium, _heavy;
        private AndroidJavaObject _vibrator;
        private int _sdk;
        private bool _ready;

        public HapticProviderAndroid(float intensity, HapticOneShot light, HapticOneShot medium, HapticOneShot heavy)
        {
            _intensity = Mathf.Clamp(intensity, 0f, 2f);
            _light = light;
            _medium = medium;
            _heavy = heavy;
            Init();
        }

        public bool TryGetOneShot(HapticType type, out HapticOneShot cfg)
        {
            switch (type)
            {
                case HapticType.Light: cfg = _light; return true;
                case HapticType.Medium: cfg = _medium; return true;
                case HapticType.Heavy: cfg = _heavy; return true;
                default: cfg = default; return false;
            }
        }

        public void SetOneShot(HapticType type, HapticOneShot cfg)
        {
            switch (type)
            {
                case HapticType.Light: _light = cfg; break;
                case HapticType.Medium: _medium = cfg; break;
                case HapticType.Heavy: _heavy = cfg; break;
            }
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

                bool hasAmp = _sdk >= 26 && _ready && _vibrator.Call<bool>("hasAmplitudeControl");
                Debug.Log($"[Haptic] Android init: sdk={_sdk}, ready={_ready}, hasAmplitudeControl={hasAmp}");
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
            // Số ms/amp lấy từ ModuleHaptic (Inspector) → tune mạnh/nhẹ không cần sửa code.
            var cfg = type switch { HapticType.Light => _light, HapticType.Medium => _medium, HapticType.Heavy => _heavy, _ => _medium };
            int ms = Mathf.Max(1, cfg.DurationMs);
            int amp = Mathf.Clamp(Mathf.RoundToInt(cfg.Amplitude * _intensity), 1, 255);

            // Luôn one-shot: máy CÓ amplitude control thì amp có tác dụng; máy KHÔNG có thì amp bị bỏ qua
            // nhưng DurationMs vẫn là đòn bẩy (rung dài hơn = cảm giác mạnh hơn).
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
