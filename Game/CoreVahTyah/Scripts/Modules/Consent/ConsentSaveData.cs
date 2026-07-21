using System;

namespace VahTyah
{
    /// <summary>Cờ đồng ý đã persist. JsonUtility → chỉ field public. Key save: "consent".</summary>
    [Serializable]
    public sealed class ConsentSaveData
    {
        public bool UMPGranted;
        public bool ATTGranted;
    }
}
