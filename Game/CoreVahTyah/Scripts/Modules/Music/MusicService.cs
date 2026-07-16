using System.Collections.Generic;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Nhạc nền: command (Play/Stop/SetVolume) gọi trực tiếp, đăng ký qua <see cref="ModuleMusic"/>.
    /// Cờ bật/tắt = <see cref="SettingsService.Sound"/>, volume = <see cref="SettingsService.MusicVolume"/> (SSOT).
    /// Vì nhạc phát liên tục nên service lắng nghe <see cref="SettingsChanged"/> để mute/unmute player ngay lập tức
    /// (notification → Event) — khác SFX/haptic vốn chỉ cần gate lúc Play.
    /// </summary>
    public sealed class MusicService
    {
        private readonly MusicPlayer _player;
        private readonly SettingsService _settings;
        private readonly Dictionary<int, AudioClip> _byId = new Dictionary<int, AudioClip>();

        private bool Active => _settings == null || _settings.Sound;
        private float Volume => _settings != null ? _settings.MusicVolume : 1f;

        public MusicService(MusicPlayer player, IReadOnlyList<MusicEntry> tracks, SettingsService settings)
        {
            _player = player;
            _settings = settings;

            foreach (var t in tracks)
                if (t.Clip != null)
                    _byId[(int)t.Id] = t.Clip;

            _player.Configure(Volume, Active);
            EventBus.On<SettingsChanged>(_ => _player.SetActive(Active));
        }

        public void Play(MusicId id)
        {
            if (_byId.TryGetValue((int)id, out var clip))
                _player.Play(clip);
        }

        public void Stop() => _player.Stop();

        public void SetVolume(float volume)
        {
            _settings?.SetMusicVolume(volume);
            _player.SetVolume(Volume);
        }

        /// <summary>Tạm dừng nhạc nền, giữ vị trí phát (dùng khi game pause).</summary>
        public void Pause() => _player.Pause();

        /// <summary>Tiếp tục nhạc nền sau <see cref="Pause"/>.</summary>
        public void Resume() => _player.Resume();

        /// <summary>Hạ gain nhạc nền xuống <paramref name="factor"/> (0..1) — vd khi mở popup.</summary>
        public void Duck(float factor = 0.3f, float duration = 0.25f) => _player.Duck(factor, duration);

        /// <summary>Trả gain nhạc nền về full sau <see cref="Duck"/>.</summary>
        public void Unduck(float duration = 0.25f) => _player.Unduck(duration);
    }
}
