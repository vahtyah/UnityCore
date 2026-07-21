#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using VahTyah.Core;
using Object = UnityEngine.Object;

public class SceneEditorController : SceneEditorControllerBase
{
    public static LevelManager LevelManager { get; private set; }
    public static bool IsInitialized = false;
    private static LevelData OriginalLevelData { get; set; }
    public static LevelData WorkingLevelData { get; private set; }

    private void Start()
    {
        LoadCurrentEditingLevel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            LoadCurrentEditingLevel();
    }

    private void LoadCurrentEditingLevel()
    {
        var index = PlayerPrefs.GetInt("editor_level_index", 0);
        var levelData = LoadLevelData(index);
        if (levelData != null)
            LoadLevel(levelData, index);
    }

    private static LevelData LoadLevelData(int index)
    {
        var gameConfig = EditorUtils.GetAsset<GameConfig>();
        if (gameConfig == null || gameConfig.LevelDatabaseConfig == null) return null;

        var levels = gameConfig.LevelDatabaseConfig.Levels;
        if (levels == null || index < 0 || index >= levels.Length) return null;

        return levels[index];
    }

    public override void LoadLevel(Object levelObject, int index)
    {
        LevelData levelData = levelObject as LevelData;
        if (levelData == null)
        {
            OriginalLevelData = null;
            Debug.Log("Invalid level object");
            return;
        }
        
        Initialize();
        ClearLevel();
        InitData(levelData);
        
        LevelManager.LoadLevel(WorkingLevelData);
    }

    private void Initialize()
    {
        if (IsInitialized && LevelManager) return;
        // GameContext.EnsureEditorServices();
        FindReferences();
        InitReferences();
        IsInitialized = true;
    }

    private void FindReferences()
    {
        if (!LevelManager) LevelManager = FindAnyObjectByType<LevelManager>();
    }

    private void InitReferences()
    {
        // var gameSettings = EditorUtils.GetAsset<GameSettings>();
        // LevelManager.Initialize(gameSettings);
        // var gameConfig = EditorUtils.GetAsset<GameConfig>();
        // gameConfig.Initialize();
    }

    private static void InitData(LevelData levelData)
    {
        OriginalLevelData = levelData;
        WorkingLevelData = levelData.Clone();
    }

    public override void ClearLevel()
    {
        
    }

    public override void ApplyLevel()
    {
        if (OriginalLevelData == null || WorkingLevelData == null) return;

        EditorUtility.CopySerialized(WorkingLevelData, OriginalLevelData);
        EditorUtility.SetDirty(OriginalLevelData);
        AssetDatabase.SaveAssets();
        Debug.Log("Applying Level");
    }

    public override void DiscardLevel()
    {
        if (OriginalLevelData == null) return;

        LoadLevel(OriginalLevelData,PlayerPrefs.GetInt("editor_level_index", 0));
    }

    public static T GetSpawner<T>() where T : Spawner
    {
        if (!LevelManager) LevelManager = FindAnyObjectByType<LevelManager>();
        return LevelManager.GetSpawner<T>();
    }
}
#endif