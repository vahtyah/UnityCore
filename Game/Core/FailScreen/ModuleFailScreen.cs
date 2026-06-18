using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Module hiển thị màn hình thua (Fail Screen) khi level thất bại.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/FailScreen", fileName = "Module_FailScreen", order = 6)]
    internal sealed class ModuleFailScreen : SAModule
    {
        public GameObject prefab;

        private GameObject _failScreen;

        public override Task InitializeAsync()
        {
            _failScreen = Object.Instantiate(prefab);
            Object.DontDestroyOnLoad(_failScreen);
            SAUIGroupManager.Attach(_failScreen, UIFailScreen.FailScreen);
            SA.SetUIGroupVisible(UIFailScreen.FailScreen, visible: false);
            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            SATypedBus.On<Ev.LevelFailed>(OnGameplayFailed);
            SATypedBus.On<Ev.SceneLoaded>(OnSceneLoaded, -100);
        }

        private async void OnGameplayFailed(Ev.LevelFailed e)
        {
            if (e.ShowScreen)
            {
                float delay = e.ShowDelay;
                await Task.Delay((int)(delay * 1000f));
                SA.SetUIGroupVisible(UIFailScreen.FailScreen, visible: true);
            }
        }

        private void OnSceneLoaded(Ev.SceneLoaded e)
        {
            SA.SetUIGroupVisible(UIFailScreen.FailScreen, visible: false);
        }
    }

    /// <summary>Nhóm UI cho màn hình thua.</summary>
    [SAEnum("UIGroup")]
    public enum UIFailScreen
    {
        FailScreen
    }
}
