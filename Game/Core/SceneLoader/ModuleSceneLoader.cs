using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StandardAssets
{
    /// <summary>
    /// Module nạp scene gameplay qua SATypedBus (Ev.SceneLoad) và phát các sự kiện vòng đời scene.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/SceneLoader", fileName = "Module_SceneLoader", order = 19)]
    internal sealed class ModuleSceneLoader : SAModule
    {
        public int gameSceneIndex = 1;

        public override Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            SATypedBus.OnAsync<Ev.SceneLoad>(HandleSceneLoad, -10);
            SATypedBus.Publish(new Ev.SceneLoad { Index = gameSceneIndex });
        }

        private async Task HandleSceneLoad(Ev.SceneLoad e)
        {
            int index = e.Index;
            await SATypedBus.Publish(new Ev.SceneUnloading());

            AsyncOperation op = SceneManager.LoadSceneAsync(index);
            if (op == null)
            {
                Debug.LogError($"[SA] Scene {index} not in Build Settings.");
                return;
            }

            // Chờ scene nạp xong
            while (!op.isDone)
            {
                await Task.Yield();
            }

            await SATypedBus.Publish(new Ev.SceneLoaded { Index = index });
        }
    }
}
