using UnityEngine;
using UnityEngine.SceneManagement;

namespace VahTyah
{
    public static class SceneRedirect
    {
        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            int buildIndex = SceneManager.GetActiveScene().buildIndex;

            if (buildIndex < 0) return;

            if (buildIndex != 0)
                SceneManager.LoadScene(0);
        }
    }
}
