using System;
using UnityEngine;

namespace VahTyah
{
    [Serializable]
    public class ParticleEntry
    {
        public ParticleId Id;
        [Tooltip("Prefab có ParticleSystem. PooledParticle sẽ tự thêm lúc spawn nếu thiếu " +
                 "(gắn sẵn nếu muốn đặt _maxLifetime cho effect looping).")]
        public GameObject Prefab;
        [Min(0)] public int Prewarm;
    }
}
