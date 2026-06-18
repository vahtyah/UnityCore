using System;

namespace VahTyah
{
    [Serializable]
    public class HeartSaveData
    {
        public int Hearts = 5;
        public long LastRegenTick;
        public long LastRegenDateBin;
        public long InfinityEndTick;
        public long InfinityEndDateBin;
    }
}
