using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Module hiển thị màn hình thắng (Win Screen) khi level hoàn thành.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/WinScreen", fileName = "Module_WinScreen", order = 23)]
    internal sealed class ModuleWinScreen : SAModule
    {
        public GameObject prefab;

        private GameObject _winScreen;

        public override Task InitializeAsync()
        {
            _winScreen = Object.Instantiate(prefab);
            Object.DontDestroyOnLoad(_winScreen);
            SAUIGroupManager.Attach(_winScreen, UIWinScreen.WinScreen);
            SA.SetUIGroupVisible(UIWinScreen.WinScreen, visible: false);
            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            SATypedBus.On<Ev.LevelCompleted>(OnGameplayEnded, -100);
            SATypedBus.On<Ev.SceneLoaded>(OnSceneLoaded, -100);
        }

        private async void OnGameplayEnded(Ev.LevelCompleted e)
        {
            if (e.ShowScreen)
            {
                float delay = e.ShowDelay;
                await Task.Delay((int)(delay * 1000f));
                SA.SetUIGroupVisible(UIWinScreen.WinScreen, visible: true);
            }
        }

        private void OnSceneLoaded(Ev.SceneLoaded e)
        {
            SA.SetUIGroupVisible(UIWinScreen.WinScreen, visible: false);
        }
    }

    /// <summary>Nhóm UI cho màn hình thắng.</summary>
    [SAEnum("UIGroup")]
    public enum UIWinScreen
    {
        WinScreen
    }
}
