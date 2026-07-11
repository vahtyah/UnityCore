namespace VahTyah
{
    public static class Sound
    {
        public static void Play(SoundId id, float volume = 1f, float pitch = 1f, bool force = false)
            => Services.Get<SoundService>()?.Play(id, volume, pitch, force);
    }
}