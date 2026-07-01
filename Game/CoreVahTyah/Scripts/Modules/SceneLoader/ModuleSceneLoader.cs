using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/SceneLoader", fileName = "Module_SceneLoader")]
    public sealed class ModuleSceneLoader : Module
    {
        [Tooltip("Scene index vào game khi boot xong.")]
        public int EntrySceneIndex = 1;

        public override void Subscribe()
        {
            EventBus.OnAsync<SceneLoadRequest>(HandleSceneLoad, -10);
            EventBus.OnAsync<LoadEntryScene>(HandleEntryScene, -10);
        }

        // Bootstrap gọi 1 lần sau khi mọi module sẵn sàng. SceneLoader tự quyết index.
        private UniTask HandleEntryScene(LoadEntryScene _)
        {
            int index = SceneRedirect.OverrideSceneIndex;   // editor: scene đang test
            if (index < 0) index = EntrySceneIndex;          // mặc định
            return EventBus.Publish(new SceneLoadRequest { Index = index });
        }

        private async UniTask HandleSceneLoad(SceneLoadRequest e)
        {
            int index = e.Index;

            await EventBus.Publish(new SceneUnloading());

            AsyncOperation op = SceneManager.LoadSceneAsync(index);
            if (op == null)
            {
                Debug.LogError($"[SceneLoader] Scene {index} not in Build Settings.");
                return;
            }

            await op;

            await EventBus.Publish(new SceneLoaded { Index = index });
        }
    }
}
