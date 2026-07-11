using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Phát haptic bằng lời gọi trực tiếp (command → service, KHÔNG qua EventBus). Đăng ký qua
    /// <see cref="ModuleHaptic"/>. Cờ bật/tắt đọc thẳng từ <see cref="SettingsService"/> (SSOT), không lưu riêng.
    /// </summary>
    public sealed class HapticService
    {
        private readonly IHapticProvider _provider;
        private readonly SettingsService _settings;
        private readonly int _gapMs;
        private readonly int _cooldownMs;

        private float _lastPlay = float.MinValue;

        // _settings == null (editor / partial boot) coi như đang bật.
        private bool Active => _settings == null || _settings.Haptics;

        public HapticService(IHapticProvider provider, SettingsService settings, int gapMs, int cooldownMs)
        {
            _provider = provider;
            _settings = settings;
            _gapMs = gapMs;
            _cooldownMs = cooldownMs;
        }

        /// <summary>Rung 1 lần (đồng bộ). force = bỏ qua cooldown (KHÔNG ghi đè khi user tắt haptic).</summary>
        public void Play(HapticType type, bool force = false)
        {
            if (!CanPlay(force)) return;
            _provider.Play(type);
        }

        /// <summary>Rung tuần tự nhiều loại, có gap giữa các lần. Awaitable — gọi có chủ đích, không per-frame.</summary>
        public async UniTask PlaySequence(bool force, params HapticType[] types)
        {
            if (types == null || types.Length == 0) return;
            if (!CanPlay(force)) return;

            for (int i = 0; i < types.Length; i++)
            {
                int ms = _provider.Play(types[i]);
                if (i < types.Length - 1)
                    await UniTask.Delay(ms + _gapMs);
            }
        }

        /// <summary>Tune live (debug): đọc/ghi cấu hình one-shot. Chỉ có tác dụng khi provider hỗ trợ (Android).</summary>
        public bool TryGetOneShot(HapticType type, out HapticOneShot cfg)
        {
            if (_provider is IHapticTunable tunable) return tunable.TryGetOneShot(type, out cfg);
            cfg = default;
            return false;
        }

        public void SetOneShot(HapticType type, HapticOneShot cfg)
        {
            if (_provider is IHapticTunable tunable) tunable.SetOneShot(type, cfg);
        }

        // Active chặn trước; force chỉ bỏ qua cooldown.
        private bool CanPlay(bool force)
        {
            if (!Active) return false;

            float now = Time.realtimeSinceStartup;
            if (!force && _cooldownMs > 0 && (now - _lastPlay) * 1000f < _cooldownMs)
                return false;

            _lastPlay = now;
            return true;
        }
    }
}
