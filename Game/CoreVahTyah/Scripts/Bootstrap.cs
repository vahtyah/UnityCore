using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    public class Bootstrap : Singleton<Bootstrap>
    {
        public ModuleConfig Config;

        protected override void OnInitialize() => BootAsync().Forget();

        private async UniTask BootAsync()
        {
            bool debug = Config != null && Config.DebugLogs;

            await InitModules(debug);

            EventBus.Publish(new BootProgress { Value = 0.85f, Message = "Đang tải màn chơi..." }).Forget();
            await EventBus.Publish(new LoadEntryScene());

            EventBus.Publish(new BootProgress { Value = 1f, Message = "Hoàn tất" }).Forget();
            EventBus.Publish(new BootCompleted()).Forget();

            if (debug) Debug.Log("[VahTyah] Boot complete.");
        }

        private async UniTask InitModules(bool debug)
        {
            Module[] modules = Config != null
                ? Config.Modules.Where(m => m != null).ToArray()
                : Array.Empty<Module>();

            if (modules.Length == 0)
            {
                Debug.LogWarning("[VahTyah] No modules configured.");
                return;
            }

            EventBus.Publish(new BootProgress { Value = 0f, Message = "Đang khởi tạo..." }).Forget();

            int total = modules.Length;

            for (int i = 0; i < modules.Length; i++)
            {
                var m = modules[i];
                try
                {
                    await m.InitializeAsync(transform);
                    m.Subscribe();
                    if (debug) Debug.Log($"[VahTyah] Booted: {m.name}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[VahTyah] {m.name} boot failed: {e.Message}\n{e.StackTrace}");
                }

                EventBus.Publish(new BootProgress { Value = (i + 1) / (float)total * 0.85f }).Forget();
            }
        }

    }
}
