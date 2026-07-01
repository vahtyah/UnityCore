using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/FailScreen", fileName = "Module_FailScreen")]
    public sealed class ModuleFailScreen : Module
    {
        public GameObject Prefab;

        private GameObject _instance;

        public override UniTask InitializeAsync(Transform holder)
        {
            _instance = Object.Instantiate(Prefab, holder);
            _instance.SetActive(false);

            return UniTask.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<LevelFailed>(OnFailed);
            EventBus.On<SceneLoaded>(OnSceneLoaded, -100);
        }

        private async void OnFailed(LevelFailed e)
        {
            if (!e.ShowScreen) return;

            if (e.ShowDelay > 0f)
                await UniTask.Delay((int)(e.ShowDelay * 1000f));

            _instance.SetActive(true);
        }

        private void OnSceneLoaded(SceneLoaded e)
        {
            _instance.SetActive(false);
        }
    }
}
