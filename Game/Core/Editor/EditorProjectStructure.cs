#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StandardAssets.Editor
{
    /// <summary>
    /// Tiện ích Editor: tạo cấu trúc thư mục dự án chuẩn và chèn .gitkeep vào thư mục rỗng.
    /// </summary>
    public static class EditorProjectStructure
    {
        private static readonly string[] Folders = new string[12]
        {
            "Assets/Scripts", "Assets/Art", "Assets/Art/Models", "Assets/Art/Materials",
            "Assets/Art/Textures", "Assets/Art/Shaders", "Assets/Art/Animations", "Assets/Prefabs",
            "Assets/Scenes", "Assets/Settings", "Assets/ThirdParty", "Assets/Plugins"
        };

        [MenuItem("SA/Create Project Structure", false, 22)]
        public static void CreateStructure()
        {
            int created = 0;
            int existed = 0;
            foreach (string folder in Folders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    existed++;
                    continue;
                }
                EnsureFolder(folder);
                created++;
            }

            AssetDatabase.Refresh();
            AddGitkeep();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Project Setup",
                $"✓ {created} Ordner erstellt\n— {existed} existierten bereits", "OK");
        }

        private static void AddGitkeep()
        {
            int count = 0;
            foreach (string folder in Folders)
            {
                if (AssetDatabase.IsValidFolder(folder) &&
                    Directory.GetFiles(folder).Length == 0 &&
                    Directory.GetDirectories(folder).Length == 0)
                {
                    File.WriteAllText(Path.Combine(folder, ".gitkeep"), "");
                    count++;
                }
            }
            AssetDatabase.Refresh();
            Debug.Log($"[SA] .gitkeep in {count} leere Ordner eingefügt.");
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/', StringSplitOptions.None);
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif
