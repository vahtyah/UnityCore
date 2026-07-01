using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Save", fileName = "Module_Save")]
    public sealed class ModuleSave : Module
    {
        [Tooltip("Tự động save mỗi N giây (0 = tắt).")]
        [SerializeField] private float _autoSaveInterval = 30f;

        public override UniTask InitializeAsync(Transform holder)
        {
            var service = new SaveService();
            service.AddProvider(new LocalSaveProvider());

            Services.Register(service);

            var go = new GameObject("[SaveRunner]");
            go.transform.SetParent(holder);
            go.hideFlags = HideFlags.HideInHierarchy;

            var runner = go.AddComponent<SaveRunner>();
            runner.Bind(service, _autoSaveInterval);

            return service.InitializeAsync();
        }
    }
}
