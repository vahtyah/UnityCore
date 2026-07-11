namespace VahTyah
{
    public struct OpenSettingsRequest : IEvent { }

    public struct SettingsChanged : IEvent
    {
        public bool Sound;
        public bool Sfx;
        public bool Haptics;
    }
}