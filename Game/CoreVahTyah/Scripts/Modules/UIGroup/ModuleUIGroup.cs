using System.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/UIGroup", fileName = "Module_UIGroup")]
    public sealed class ModuleUIGroup : Module
    {
        public override Task InitializeAsync(Transform transform)
        {
            Services.Register(new UIGroupService());
            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<ScreenRequest>(e => ScreenRouter.GoTo(e.Screen));
        }
    }
}
