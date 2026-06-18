using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR && VAHTYAH_CUSTOM_INSPECTOR
using VahTyah.LevelEditor;
#endif

public abstract class Spawner : MonoBehaviour
{
    public List<GameObject> SpawnedObjects {get; private set;} = new();

    public abstract void Spawn(LevelData levelData);

    public virtual void Clear()
    {
#if UNITY_EDITOR && VAHTYAH_CUSTOM_INSPECTOR
        if (!Application.isPlaying || LevelEditorUtils.IsInScene("LevelEditor"))
        {
            if (transform != null)
            {
                while (transform.childCount > 0)
                {
                    DestroyImmediate(transform.GetChild(0).gameObject);
                }
            }
        }
#endif
        
        foreach (var pixel in SpawnedObjects)
        {
            if (pixel == null || !Pool.IsSpawned(pixel)) continue;
            Pool.Despawn(pixel);
        }

        SpawnedObjects.Clear();
    }
}

public interface ISpawner
{
    void Spawn(LevelData levelDetail);
    void Clear();
}