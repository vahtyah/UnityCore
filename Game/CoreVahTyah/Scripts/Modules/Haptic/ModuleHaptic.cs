using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    /// <summary>Cấu hình rung 1 loại (Android). DurationMs &lt;~25ms nhiều máy không cảm nhận được.</summary>
    [Serializable]
    public struct HapticOneShot
    {
        [Tooltip("Độ dài rung (ms). Dưới ~25ms nhiều máy Android không cảm nhận được.")]
        public int DurationMs;

        [Range(1, 255)]
        [Tooltip("Cường độ 1-255. Chỉ có tác dụng khi máy CÓ amplitude control; máy không có thì chỉ DurationMs điều khiển được.")]
        public int Amplitude;
    }

    /// <summary>
    /// Dựng provider theo nền tảng + đăng ký <see cref="HapticService"/> vào Services.
    /// Boot SAU ModuleSave và ModuleSettingsScreen (service cần <see cref="SettingsService"/> để đọc cờ bật/tắt).
    /// </summary>
    [CreateAssetMenu(menuName = "VahTyah/Modules/Haptic", fileName = "Module_Haptic")]
    [ModuleRequires(typeof(ModuleSettingsScreen))]
    public sealed class ModuleHaptic : Module
    {
        [BoxGroup("Sequential")]
        [Tooltip("Khoảng nghỉ (ms) thêm sau mỗi haptic trong chuỗi.")]
        [SerializeField] private int _gapMs = 20;

        [BoxGroup("Cooldown")]
        [Tooltip("Thời gian tối thiểu (ms) giữa 2 lần rung. Dùng Force để bỏ qua.")]
        [SerializeField] private int _cooldownMs = 80;

        [BoxGroup("Android")]
        [Tooltip("Cường độ rung Android tổng. 0 = tắt, 1 = thường, 2 = mạnh gấp đôi. Nhân với Amplitude từng loại.")]
        [Range(0f, 2f)]
        [SerializeField] private float _androidIntensity = 1f;

        [BoxGroup("Android Levels", "Android — cường độ từng loại (khi máy có amplitude control)")]
        [SerializeField] private HapticOneShot _light = new HapticOneShot { DurationMs = 35, Amplitude = 200 };
        [BoxGroup("Android Levels")]
        [SerializeField] private HapticOneShot _medium = new HapticOneShot { DurationMs = 45, Amplitude = 230 };
        [BoxGroup("Android Levels")]
        [SerializeField] private HapticOneShot _heavy = new HapticOneShot { DurationMs = 60, Amplitude = 255 };

        public override UniTask InitializeAsync(Transform holder)
        {
            Services.TryGet<SettingsService>(out var settings); // null trong editor/partial boot → coi như bật
            var provider = CreateProvider();
            Services.Register(new HapticService(provider, settings, _gapMs, _cooldownMs));
            return UniTask.CompletedTask;
        }

        private IHapticProvider CreateProvider()
        {
#if UNITY_EDITOR
            return new HapticProviderDefault();
#elif UNITY_IOS
            return HapticNativeRegistry.IOSProvider ?? new HapticProviderIOS();
#elif UNITY_ANDROID
            return new HapticProviderAndroid(_androidIntensity, _light, _medium, _heavy);
#else
            return new HapticProviderDefault();
#endif
        }
    }
}
