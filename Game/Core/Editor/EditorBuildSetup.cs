#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StandardAssets.Editor
{
    /// <summary>
    /// Tiện ích Editor: cấu hình Build Settings từ các scene trong thư mục StandardAssets/Scenes.
    /// </summary>
    public static class EditorBuildSetup
    {
        private const string LoadSceneName = "SA_Load";

        [MenuItem("SA/Setup Build Settings", false, 21)]
        public static void SetupBuildSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:Scene", new string[1] { "Assets/StandardAssets/Scenes" });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("SA Build Setup",
                    "No scenes found in Assets/Scenes/.\nCreate your scenes there first.", "OK");
                return;
            }

            List<string> scenePaths = guids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToList();
            string bootScene = scenePaths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == LoadSceneName);
            if (bootScene == null)
            {
                EditorUtility.DisplayDialog("SA Build Setup",
                    "Boot scene 'SA_Load.unity' not found in Assets/Scenes/.\nCreate it first, then run this again.", "OK");
                return;
            }

            // Boot scene đứng đầu, các scene còn lại sắp theo tên
            List<string> ordered = new List<string> { bootScene };
            ordered.AddRange(from p in scenePaths
                where p != bootScene
                orderby Path.GetFileNameWithoutExtension(p)
                select p);

            EditorBuildSettingsScene[] scenes = (EditorBuildSettings.scenes =
                ordered.Select(p => new EditorBuildSettingsScene(p, true)).ToArray());

            Debug.Log("[SA] Build Settings updated:");
            for (int i = 0; i < scenes.Length; i++)
            {
                Debug.Log($"  [{i}] {Path.GetFileNameWithoutExtension(scenes[i].path)}");
            }

            EditorUtility.DisplayDialog("SA Build Setup",
                $"✓ {scenes.Length} scene(s) added to Build Settings.\n[0] SA_Load (boot)", "OK");
        }
    }
}
#endif
