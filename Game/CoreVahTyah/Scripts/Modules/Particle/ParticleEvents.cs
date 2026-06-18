using UnityEngine;

namespace VahTyah
{
    public struct ParticlePlay : IEvent { public ParticleId Id; public Vector3 Position; }
    public struct ParticlePlayUI : IEvent { public ParticleId Id; public Vector3 Position; }
}
