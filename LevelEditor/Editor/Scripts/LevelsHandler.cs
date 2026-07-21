using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using VahTyah.Core;
using VahTyah.LevelEditor;

public class LevelsHandler : LevelsHandlerBase
{
    protected override void DrawButtonToolbar()
    {
        base.DrawButtonToolbar();
        DrawOpenSceneGameButton();
        DrawSyncLevelsButton();
    }

    private void DrawOpenSceneGameButton()
    {
        if (GUILayout.Button("Open Scene Game"))
        {
            LevelEditorUtils.OpenScene("Assets/Game/Scenes/Game.unity", false);
        }
    }

    private void DrawSyncLevelsButton()
    {
        if (!GUILayout.Button("Reload Levels Asset")) return;

        var db = EditorUtils.GetAsset<LevelDatabaseConfig>();
        if (db == null) { Debug.LogError("[LevelsHandler] LevelDatabaseConfig not found."); return; }

        var guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Game/Data/Levels" });
        var levels = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(l => l != null)
            .OrderBy(l => l.name, NaturalComparer.Instance)
            .ToArray();

        Undo.RecordObject(db, "Reload Levels Asset");
        db.Levels = levels;
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        CustomList.ReloadList();
    }

    private class NaturalComparer : IComparer<string>
    {
        public static readonly NaturalComparer Instance = new NaturalComparer();

        public int Compare(string a, string b)
        {
            var partsA = Regex.Split(a, @"(\d+)");
            var partsB = Regex.Split(b, @"(\d+)");
            for (int i = 0; i < System.Math.Min(partsA.Length, partsB.Length); i++)
            {
                int cmp = int.TryParse(partsA[i], out int na) && int.TryParse(partsB[i], out int nb)
                    ? na.CompareTo(nb)
                    : string.Compare(partsA[i], partsB[i], System.StringComparison.OrdinalIgnoreCase);
                if (cmp != 0) return cmp;
            }
            return partsA.Length.CompareTo(partsB.Length);
        }
    }
}