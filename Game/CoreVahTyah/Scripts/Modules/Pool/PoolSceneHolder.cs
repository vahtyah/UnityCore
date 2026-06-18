using UnityEngine;

namespace VahTyah
{
    /// <summary>Đăng ký pool riêng cho 1 scene, tự huỷ pool khi scene unload.</summary>
    [DefaultExecutionOrder(-5)]
    public class PoolSceneHolder : MonoBehaviour
    {
        [SerializeField] private PoolEntry[] _pools;

        private void Awake()
        {
            if (!Services.Has<PoolService>()) return;

            var service = Services.Get<PoolService>();
            foreach (var entry in _pools)
                if (entry.Prefab != null)
                    service.Register(entry.Prefab, entry.Prewarm);
        }

        private void OnDestroy()
        {
            if (!Services.Has<PoolService>()) return;

            var service = Services.Get<PoolService>();
            foreach (var entry in _pools)
                if (entry.Prefab != null)
                    service.DestroyPool(entry.Prefab);
        }
    }
}
