using System;
using UnityEngine;

namespace VahTyah
{
    /// <summary>Khai báo pool trong Inspector: prefab + số lượng prewarm.</summary>
    [Serializable]
    public class PoolEntry
    {
        public GameObject Prefab;
        [Min(0)] public int Prewarm;
    }
}
