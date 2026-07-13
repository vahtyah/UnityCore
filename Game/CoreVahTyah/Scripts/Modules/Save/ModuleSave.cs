using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Save", fileName = "Module_Save")]
    [CoreModule] // bắt buộc: ModuleConfig Doctor tự offer-add nếu thiếu, ẩn nút Remove
    public sealed class ModuleSave : Module
    {
        [BoxGroup("Settings")]
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
