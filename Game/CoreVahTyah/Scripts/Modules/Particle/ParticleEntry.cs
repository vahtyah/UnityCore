using System;
using UnityEngine;

namespace VahTyah
{
    [Serializable]
    public class ParticleEntry
    {
        public ParticleId Id;
        [Tooltip("Prefab có ParticleSystem + PooledParticle.")]
        public GameObject Prefab;
        [Min(0)] public int Prewarm;
    }
}
