using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Pool", fileName = "Module_Pool")]
    public sealed class ModulePool : Module
    {
        [BoxGroup("Pools")]
        [Tooltip("Pool tạo sẵn lúc boot (prefab + prewarm).")]
        public List<PoolEntry> Pools = new List<PoolEntry>();

        private PoolService _service;

        public override UniTask InitializeAsync(Transform holder)
        {
            var root = new GameObject("[PoolService]").transform;
            root.SetParent(holder);

            _service = new PoolService(root);

            foreach (var entry in Pools)
                if (entry.Prefab != null)
                    _service.Register(entry.Prefab, entry.Prewarm);

            Services.Register(_service);

            return UniTask.CompletedTask;
        }

        public override void Subscribe()
        {
            // Dọn object còn active của scene CŨ TRƯỚC khi load scene mới.
            // Dùng SceneUnloading (trước load) chứ không phải SceneLoaded (sau load) —
            // tránh despawn nhầm object mà scene mới vừa spawn lúc init.
            EventBus.On<SceneUnloading>(_ => _service.ReturnAll());
        }
    }
}
