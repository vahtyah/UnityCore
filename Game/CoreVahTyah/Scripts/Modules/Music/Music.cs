namespace VahTyah
{
    /// <summary>Shortcut tĩnh điều khiển nhạc nền — gọi thẳng <see cref="MusicService"/> (không qua EventBus).</summary>
    public static class Music
    {
        public static void Play(MusicId id) => Services.Get<MusicService>()?.Play(id);
        public static void Stop() => Services.Get<MusicService>()?.Stop();
        public static void SetVolume(float volume) => Services.Get<MusicService>()?.SetVolume(volume);

        public static void Pause() => Services.Get<MusicService>()?.Pause();
        public static void Resume() => Services.Get<MusicService>()?.Resume();
        public static void Duck(float factor = 0.3f, float duration = 0.25f) => Services.Get<MusicService>()?.Duck(factor, duration);
        public static void Unduck(float duration = 0.25f) => Services.Get<MusicService>()?.Unduck(duration);
        // Bật/tắt BGM: đi qua SettingsService.SetSound (SSOT), không đặt ở đây.
    }
}
