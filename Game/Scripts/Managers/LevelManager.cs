using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR && VAHTYAH_CUSTOM_INSPECTOR
using VahTyah.Inspector;
#endif

public class LevelManager : MonoBehaviour
{
    [SerializeField] private Spawner[] spawners;


    public void Initialize()
    {
    }

    public void LoadLevel(LevelData levelData)
    {
        ClearLevel();
        InitializeSystem(levelData);
        SpawnSpawners(levelData);
        FinalizeLevel();
    }

    private void FinalizeLevel()
    {
        WinLoseChecker.Initialize();
    }

    private void InitializeSystem(LevelData levelData)
    {
        LevelContext.Initialize(levelData);
    }

    private void ClearLevel()
    {
        ClearSpawners();
        WinLoseChecker.Dispose();
        LevelContext.Dispose();
    }

    private void SpawnSpawners(LevelData levelData)
    {
        foreach (var spawner in spawners)
        {
            spawner?.Spawn(levelData);
        }
    }

    private void ClearSpawners()
    {
        foreach (var spawner in spawners)
        {
            spawner?.Clear();
        }
    }

#if UNITY_EDITOR

    private void ReloadSpawners()
    {
        spawners = new Spawner[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            spawners[i] = transform.GetChild(i).GetComponent<Spawner>();
        }
    }

    public void ReLoadSpawner<T>(LevelData levelData) where T : Spawner
    {
        foreach (var spawner in spawners)
        {
            if (spawner is T typedSpawner)
            {
                typedSpawner.Clear();
                typedSpawner.Spawn(levelData);
                break;
            }
        }
    }

    public T GetSpawner<T>() where T : Spawner
    {
        foreach (var spawner in spawners)
        {
            if (spawner is T typedSpawner)
            {
                return typedSpawner;
            }
        }
        return null;
    }
#endif
}

public static class LevelContext
{
    public static void Initialize(LevelData levelData)
    {
    }
    
    public static void Dispose()
    {
    }
}
