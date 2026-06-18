using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VahTyah.EditorTools
{
    /// <summary>
    /// Bấm Play ở scene bất kỳ → game tự boot qua boot scene (index 0),
    /// rồi load LẠI đúng scene bạn đang sửa (không nhảy về scene mặc định).
    /// Dùng EditorSceneManager.playModeStartScene + SessionState (sống xuyên domain reload).
    /// </summary>
    [InitializeOnLoad]
    public static class SceneRedirectEditor
    {
        private const int BootSceneIndex = 0;

        static SceneRedirectEditor()
        {
            EditorSceneManager.activeSceneChangedInEditMode += (_, __) => Apply();
            EditorApplication.playModeStateChanged += _ => Apply();
            Apply();
        }

        private static void Apply()
        {
            // chỉ chỉnh khi đang ở edit mode (không can thiệp lúc đang play)
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Scene current = SceneManager.GetActiveScene();

            // Đang mở boot scene → play bình thường, không redirect.
            if (current.buildIndex == BootSceneIndex)
            {
                Clear();
                return;
            }

            // Scene không nằm trong Build Settings → cho test độc lập, không ép qua boot.
            if (current.buildIndex < 0)
            {
                Clear();
                return;
            }

            string bootPath = SceneUtility.GetScenePathByBuildIndex(BootSceneIndex);
            var bootAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(bootPath);
            if (bootAsset == null)
            {
                Clear();
                return;
            }

            EditorSceneManager.playModeStartScene = bootAsset;
            SessionState.SetInt(SceneRedirect.SessionKey, current.buildIndex);
        }

        private static void Clear()
        {
            EditorSceneManager.playModeStartScene = null;
            SessionState.EraseInt(SceneRedirect.SessionKey);
        }
    }
}
