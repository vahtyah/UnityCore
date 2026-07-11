    using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/WinScreen", fileName = "Module_WinScreen")]
    public sealed class ModuleWinScreen : Module
    {
        public GameObject Prefab;

        private GameObject _instance;
        private IPanelView _winView;

        public override UniTask InitializeAsync(Transform holder)
        {
            _instance = Object.Instantiate(Prefab, holder);
            _winView = _instance.GetComponent<IPanelView>();
            _instance.SetActive(false);

            return UniTask.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<LevelCompleted>(OnCompleted, -100);
            EventBus.On<LevelLoadRequest>(OnLevelLoadRequest);
        }

        private async void OnCompleted(LevelCompleted e)
        {
            if (!e.ShowScreen) return;

            if (e.ShowDelay > 0f)
                await UniTask.Delay((int)(e.ShowDelay * 1000f));

            _winView.Show();
        }

        private void OnLevelLoadRequest(LevelLoadRequest e)
        {
            _instance.SetActive(false);
        }
    }
}
