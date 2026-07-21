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
            await UniTask.Yield();

            bool debug = Config != null && Config.DebugLogs;

            var introReady = EventBus.WaitFor<BootIntroCompleted>();

            await InitModules(debug);

            await introReady;

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
            }
        }

        // Chuyển tiếp vòng đời app vào EventBus (xem AppEvents.cs). Không có listener → no-op (fast-path).
        private void OnApplicationPause(bool paused)
        {
            if (paused)
                EventBus.Publish(new AppPaused()).Forget();
            else
                EventBus.Publish(new AppResumed()).Forget();
        }

        private void OnApplicationQuit() => EventBus.Publish(new AppQuitting()).Forget();
    }
}
