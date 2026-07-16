using System.Collections.Generic;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Spawn particle one-shot theo <see cref="ParticleId"/> qua <see cref="PoolService"/>. Đăng ký bởi
    /// <see cref="ModuleParticle"/>; prefab cần ParticleSystem + <see cref="PooledParticle"/> (tự despawn khi dừng).
    /// Truy cập qua shortcut <see cref="Particles"/>. Chỉ là bảng tra + spawn, không state runtime.
    /// </summary>
    public sealed class ParticleService
    {
        // key bằng int (cast từ enum) → tra O(1) không box. Reorder list không ảnh hưởng.
        private readonly Dictionary<int, ParticleEntry> _byId = new Dictionary<int, ParticleEntry>();
        private readonly PoolService _pool;

        public ParticleService(IReadOnlyList<ParticleEntry> effects, PoolService pool)
        {
            _pool = pool;
            if (effects != null)
                for (int i = 0; i < effects.Count; i++)
                {
                    var e = effects[i];
                    if (e == null || e.Prefab == null) continue;
                    if (_byId.ContainsKey((int)e.Id))
                        Debug.LogWarning($"[Particle] Duplicate id '{e.Id}' — dùng bản khai báo sau.");
                    _byId[(int)e.Id] = e; // last-wins
                }
        }

        public GameObject Play(ParticleId id, Vector3 position)
            => Play(id, position, Quaternion.identity, null);

        public GameObject Play(ParticleId id, Vector3 position, Quaternion rotation)
            => Play(id, position, rotation, null);

        /// <summary>Spawn gắn vào <paramref name="parent"/> (particle đi theo object di chuyển).</summary>
        public GameObject Play(ParticleId id, Transform parent)
            => Play(id, parent != null ? parent.position : Vector3.zero, Quaternion.identity, parent);

        public GameObject Play(ParticleId id, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (_pool == null) return null;
            if (!_byId.TryGetValue((int)id, out var entry) || entry.Prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[Particle] Không có effect cho id '{id}' (thiếu entry hoặc prefab).");
#endif
                return null;
            }
            
            var obj = _pool.Spawn(entry.Prefab, position, rotation, parent);
            // Auto-add PooledParticle nếu prefab chưa có. An toàn vì PooledParticle tự lái qua
            // OnEnable/OnDisable (không dùng IPoolable) → AddComponent lúc spawn vẫn Play/despawn đúng.
            if (obj != null && !obj.TryGetComponent<PooledParticle>(out _))
                obj.AddComponent<PooledParticle>();

            return obj;
        }
    }
}
