using UnityEngine;
using UnityEngine.SceneManagement;

namespace StandardAssets
{
    /// <summary>
    /// Khi bấm Play ở một scene bất kỳ trong Editor (buildIndex != 0),
    /// tự nạp lại scene 0 (scene boot chứa SAManager) để framework luôn khởi động đúng.
    /// </summary>
    public static class SASceneRedirect
    {
        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            if (SceneManager.GetActiveScene().buildIndex != 0)
                SceneManager.LoadScene(0);
        }
    }
}
