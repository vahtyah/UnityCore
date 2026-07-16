using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/FailScreen", fileName = "Module_FailScreen")]
    public sealed class ModuleFailScreen : Module
    {
        [BoxGroup("Screen")]
        [Required(isError: true)]
        public GameObject Prefab;

        private IPanelAnimator _panelView;
        private GameObject _instance;

        public override UniTask InitializeAsync(Transform holder)
        {
            _instance = Object.Instantiate(Prefab, holder);
            _panelView = _instance.GetComponent<IPanelAnimator>();
            _instance.SetActive(false);

            return UniTask.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<LevelFailed>(OnFailed);
            EventBus.On<LevelLoadRequest>(OnLevelLoadRequest);
        }

        private async void OnFailed(LevelFailed e)
        {
            if (!e.ShowScreen) return;

            if (e.ShowDelay > 0f)
                await UniTask.Delay((int)(e.ShowDelay * 1000f));

            _panelView.Show();
        }

        private void OnLevelLoadRequest(LevelLoadRequest e)
        {
            _panelView.Hide();
        }
    }
}
