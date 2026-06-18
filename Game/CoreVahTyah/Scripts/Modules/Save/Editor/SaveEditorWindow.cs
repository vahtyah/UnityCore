#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VahTyah.Editor
{
    public sealed class SaveEditorWindow : EditorWindow
    {
        private Vector2 _scroll;
        private string[] _files = new string[0];
        private string _selectedFile;
        private string _selectedContent;

        [MenuItem("VahTyah/Save Browser")]
        public static void Open() => GetWindow<SaveEditorWindow>("Save Browser");

        private void OnEnable() => Refresh();

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                Refresh();

            if (GUILayout.Button("Open Folder", EditorStyles.toolbarButton, GUILayout.Width(80)))
                EditorUtility.RevealInFinder(Application.persistentDataPath);

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Delete All", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                if (EditorUtility.DisplayDialog("Delete All Saves",
                    "Are you sure? This cannot be undone.", "Delete", "Cancel"))
                {
                    DeleteAll();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            // File list
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_files.Length == 0)
            {
                EditorGUILayout.LabelField("No save files found.");
            }
            else
            {
                foreach (var file in _files)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    bool selected = file == _selectedFile;

                    if (selected)
                        GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);

                    if (GUILayout.Button(name, EditorStyles.miniButton))
                        SelectFile(file);

                    GUI.backgroundColor = Color.white;
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Content preview
            EditorGUILayout.BeginVertical();

            if (!string.IsNullOrEmpty(_selectedContent))
            {
                EditorGUILayout.LabelField(Path.GetFileName(_selectedFile), EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextArea(_selectedContent, GUILayout.ExpandHeight(true));
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Copy JSON"))
                    EditorGUIUtility.systemCopyBuffer = _selectedContent;

                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("Delete"))
                {
                    if (EditorUtility.DisplayDialog("Delete Save",
                        $"Delete {Path.GetFileName(_selectedFile)}?", "Delete", "Cancel"))
                    {
                        File.Delete(_selectedFile);
                        _selectedFile = null;
                        _selectedContent = null;
                        Refresh();
                    }
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("Select a file to preview.");
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void Refresh()
        {
            string path = Application.persistentDataPath;
            _files = Directory.Exists(path)
                ? Directory.GetFiles(path, "*.json").OrderBy(f => f).ToArray()
                : new string[0];
        }

        private void SelectFile(string path)
        {
            _selectedFile = path;
            try
            {
                string raw = File.ReadAllText(path);
                _selectedContent = FormatJson(raw);
            }
            catch
            {
                _selectedContent = "(failed to read)";
            }
        }

        private void DeleteAll()
        {
            foreach (var f in _files)
                File.Delete(f);

            _selectedFile = null;
            _selectedContent = null;
            Refresh();
        }

        private static string FormatJson(string json)
        {
            try
            {
                int indent = 0;
                var sb = new System.Text.StringBuilder();
                bool inString = false;

                foreach (char c in json)
                {
                    if (c == '"' && (sb.Length == 0 || sb[sb.Length - 1] != '\\'))
                        inString = !inString;

                    if (inString)
                    {
                        sb.Append(c);
                        continue;
                    }

                    switch (c)
                    {
                        case '{':
                        case '[':
                            sb.Append(c);
                            sb.AppendLine();
                            indent++;
                            sb.Append(new string(' ', indent * 2));
                            break;
                        case '}':
                        case ']':
                            sb.AppendLine();
                            indent--;
                            sb.Append(new string(' ', indent * 2));
                            sb.Append(c);
                            break;
                        case ',':
                            sb.Append(c);
                            sb.AppendLine();
                            sb.Append(new string(' ', indent * 2));
                            break;
                        case ':':
                            sb.Append(": ");
                            break;
                        default:
                            sb.Append(c);
                            break;
                    }
                }
                return sb.ToString();
            }
            catch
            {
                return json;
            }
        }
    }
}
#endif
