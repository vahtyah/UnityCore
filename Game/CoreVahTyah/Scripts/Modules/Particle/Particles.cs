using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Shortcut tĩnh spawn particle — gọi thẳng <see cref="ParticleService"/> (không qua EventBus).
    /// Trả GameObject đã spawn (null nếu thiếu ModuleParticle/ModulePool hoặc id không có entry).
    /// Đặt tên số nhiều để tránh đụng <c>UnityEngine.Particle</c> (type legacy) khi file dùng cả 2 namespace.
    /// </summary>
    public static class Particles
    {
        public static GameObject Play(ParticleId id, Vector3 position)
            => Services.Get<ParticleService>()?.Play(id, position);

        public static GameObject Play(ParticleId id, Vector3 position, Quaternion rotation)
            => Services.Get<ParticleService>()?.Play(id, position, rotation);

        public static GameObject Play(ParticleId id, Transform parent)
            => Services.Get<ParticleService>()?.Play(id, parent);

        public static GameObject Play(ParticleId id, Vector3 position, Quaternion rotation, Transform parent)
            => Services.Get<ParticleService>()?.Play(id, position, rotation, parent);
    }
}
