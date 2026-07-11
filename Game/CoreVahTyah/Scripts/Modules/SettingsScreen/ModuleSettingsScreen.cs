using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Sở hữu Settings popup: instantiate prefab dưới holder (giữ inactive) và mở khi có
    /// <see cref="OpenSettingsRequest"/>. Boot SAU ModuleSave/ModuleMusic/ModuleSound/ModuleHaptic
    /// vì view query trạng thái các module đó qua *Get event khi mở.
    /// </summary>
    [CreateAssetMenu(menuName = "VahTyah/Modules/SettingsScreen", fileName = "Module_SettingsScreen")]
    public sealed class ModuleSettingsScreen : Module
    {
        public GameObject Prefab;

        private SettingsView _instance;

        public override async UniTask InitializeAsync(Transform holder)
        {
            var service = new SettingsService(Services.Get<SaveService>());
            await service.InitAsync();
            Services.Register(service);
            
            _instance = Object.Instantiate(Prefab, holder).GetComponent<SettingsView>();
            _instance.Bind(service);
            _instance.gameObject.SetActive(false);
        }

        public override void Subscribe()
        {
            EventBus.On<OpenSettingsRequest>(_ => _instance.Open());
        }
    }
}
