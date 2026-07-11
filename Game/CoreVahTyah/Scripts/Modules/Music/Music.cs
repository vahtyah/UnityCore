namespace VahTyah
{
    /// <summary>Shortcut tĩnh điều khiển nhạc nền — gọi thẳng <see cref="MusicService"/> (không qua EventBus).</summary>
    public static class Music
    {
        public static void Play(MusicId id) => Services.Get<MusicService>()?.Play(id);
        public static void Stop() => Services.Get<MusicService>()?.Stop();
        public static void SetVolume(float volume) => Services.Get<MusicService>()?.SetVolume(volume);
        // Bật/tắt BGM: đi qua SettingsService.SetSound (SSOT), không đặt ở đây.
    }
}
