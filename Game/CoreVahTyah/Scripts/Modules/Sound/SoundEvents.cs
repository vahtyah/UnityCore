namespace VahTyah
{
    public struct SoundPlay : IEvent
    {
        public SoundId Id;
        public float Volume; // <=0 → dùng 1 (mặc định)
        public float Pitch;  // <=0 → dùng 1 (mặc định)
    }
}
