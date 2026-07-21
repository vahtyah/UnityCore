using System;

namespace VahTyah
{
    /// <summary>
    /// Persisted state for re-engagement. DateTime is stored as ToBinary() (long) — JsonUtility does not persist DateTime.
    /// Save key: "notifications". 0 = no timestamp yet.
    /// </summary>
    [Serializable]
    public sealed class NotificationsSaveData
    {
        public long LastPlayBinary;
        public long LastScheduledBinary;
    }
}
