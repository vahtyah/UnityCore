using Cysharp.Threading.Tasks;

namespace VahTyah
{
    /// <summary>Shortcut tĩnh publish event nhạc nền.</summary>
    public static class Music
    {
        public static void Play(MusicId id) => EventBus.Publish(new MusicPlay { Id = id }).Forget();
        public static void Stop() => EventBus.Publish(new MusicStop()).Forget();
        public static void SetVolume(float volume) => EventBus.Publish(new MusicSetVolume { Volume = volume }).Forget();
        public static void SetActive(bool active) => EventBus.Publish(new MusicSetActive { Active = active }).Forget();
    }
}
