#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VahTyah.Editor
{
    public static class SavePresetManager
    {
        private static string PresetsFolder
        {
            get
            {
                string path = Path.Combine(Application.dataPath, "SavePresets");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        private static string SaveFolder => Application.persistentDataPath;

        [MenuItem("VahTyah/Presets/Save Preset...")]
        public static void SavePreset()
        {
            string name = EditorInputDialog.Show("Save Preset", "Preset name:", "preset_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            if (string.IsNullOrEmpty(name))
                return;

            string presetDir = Path.Combine(PresetsFolder, name);
            if (Directory.Exists(presetDir))
            {
                if (!EditorUtility.DisplayDialog("Overwrite", $"Preset '{name}' exists. Overwrite?", "Yes", "No"))
                    return;

                Directory.Delete(presetDir, true);
            }

            Directory.CreateDirectory(presetDir);

            string[] files = Directory.GetFiles(SaveFolder, "*.json");
            foreach (var f in files)
                File.Copy(f, Path.Combine(presetDir, Path.GetFileName(f)));

            Debug.Log($"[Save] Preset '{name}' saved ({files.Length} files)");
        }

        [MenuItem("VahTyah/Presets/Load Preset...")]
        public static void LoadPreset()
        {
            if (!Directory.Exists(PresetsFolder))
            {
                EditorUtility.DisplayDialog("No Presets", "No presets found.", "OK");
                return;
            }

            string[] presets = Directory.GetDirectories(PresetsFolder)
                .Select(Path.GetFileName)
                .ToArray();

            if (presets.Length == 0)
            {
                EditorUtility.DisplayDialog("No Presets", "No presets found.", "OK");
                return;
            }

            var menu = new GenericMenu();
            foreach (var p in presets)
                menu.AddItem(new GUIContent(p), false, () => ApplyPreset(p));
            menu.ShowAsContext();
        }

        [MenuItem("VahTyah/Presets/Delete Preset...")]
        public static void DeletePreset()
        {
            if (!Directory.Exists(PresetsFolder))
                return;

            string[] presets = Directory.GetDirectories(PresetsFolder)
                .Select(Path.GetFileName)
                .ToArray();

            if (presets.Length == 0)
                return;

            var menu = new GenericMenu();
            foreach (var p in presets)
            {
                menu.AddItem(new GUIContent(p), false, () =>
                {
                    if (EditorUtility.DisplayDialog("Delete Preset", $"Delete '{p}'?", "Delete", "Cancel"))
                    {
                        Directory.Delete(Path.Combine(PresetsFolder, p), true);
                        Debug.Log($"[Save] Preset '{p}' deleted");
                    }
                });
            }
            menu.ShowAsContext();
        }

        private static void ApplyPreset(string name)
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Stop Game", "Stop Play mode before loading a preset.", "OK");
                return;
            }

            string presetDir = Path.Combine(PresetsFolder, name);
            string[] files = Directory.GetFiles(presetDir, "*.json");

            // Clear current saves
            foreach (var f in Directory.GetFiles(SaveFolder, "*.json"))
                File.Delete(f);

            // Copy preset
            foreach (var f in files)
                File.Copy(f, Path.Combine(SaveFolder, Path.GetFileName(f)), true);

            Debug.Log($"[Save] Preset '{name}' loaded ({files.Length} files)");
        }
    }

    internal static class EditorInputDialog
    {
        public static string Show(string title, string label, string defaultValue)
        {
            string result = defaultValue;
            bool confirmed = false;

            var window = ScriptableObject.CreateInstance<InputDialogWindow>();
            window.titleContent = new GUIContent(title);
            window.Label = label;
            window.Value = defaultValue;
            window.OnConfirm = v => { result = v; confirmed = true; };
            window.ShowModalUtility();

            return confirmed ? result : null;
        }
    }

    internal sealed class InputDialogWindow : EditorWindow
    {
        public string Label;
        public string Value;
        public System.Action<string> OnConfirm;

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(Label);
            Value = EditorGUILayout.TextField(Value);
            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("OK", GUILayout.Width(80)))
            {
                OnConfirm?.Invoke(Value);
                Close();
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
                Close();

            EditorGUILayout.EndHorizontal();

            minSize = maxSize = new Vector2(300, 100);
        }
    }
}
#endif
