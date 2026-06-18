using System.Collections.Generic;
using UnityEngine;
#if VAHTYAH_CUSTOM_INSPECTOR
using VahTyah.Inspector;
#endif
using Object = UnityEngine.Object;

public interface IPoolable
{
    void OnSpawnFromPool();
    void OnReturnToPool();
}

public class Pool
{
    private static PoolManager manager { get; set; }
    private readonly Queue<GameObject> availableObjects = new Queue<GameObject>();
    private readonly HashSet<GameObject> availableSeenObjects = new HashSet<GameObject>();
    private Transform parentTransform;

    private Pool(Transform parent)
    {
        this.parentTransform = parent;
    }

    public static Pool Register(GameObject prefab, Transform parentTransform = null, int initialSize = 0)
    {
        EnsureManagerExists();

        parentTransform ??= manager.transform;
        var pool = new Pool(parentTransform);

        for (var i = 0; i < initialSize; i++)
        {
            var obj = Object.Instantiate(prefab, parentTransform);
            obj.SetActive(false);
            pool.availableObjects.Enqueue(obj);
            pool.availableSeenObjects.Add(obj);
        }

        return manager.RegisterPool(prefab, pool);
    }

    public static GameObject Spawn(GameObject prefab)
    {
        return Spawn(prefab, Vector3.zero, Quaternion.identity);
    }

    public static GameObject Spawn(GameObject prefab, Transform parent)
    {
        return Spawn(prefab, Vector3.zero, Quaternion.identity, parent);
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation,
        Transform parent = null)
    {
        EnsureManagerExists();

        var pool = manager.GetPool(prefab) ?? Register(prefab);
        var targetParent = parent ?? pool.parentTransform;

        GameObject obj = null;

        while (pool.availableObjects.Count > 0)
        {
            obj = pool.availableObjects.Dequeue();
            pool.availableSeenObjects.Remove(obj);

            if (obj == null) continue;

            obj.transform.SetParent(targetParent, false);
            obj.transform.SetPositionAndRotation(position, rotation);
            break;
        }

        if (obj == null)
        {
            obj = Object.Instantiate(prefab, position, rotation, targetParent);
        }

        manager.TrackObject(obj, pool);
        obj.SetActive(true);

        manager.NotifySpawn(obj);

        return obj;
    }

    private void Return(GameObject obj)
    {
        EnsureManagerExists();

        if (availableSeenObjects.Contains(obj))
            return;

        manager.NotifyReturn(obj);

        obj.SetActive(false);

        obj.transform.SetParent(parentTransform, false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        availableObjects.Enqueue(obj);
        availableSeenObjects.Add(obj);
    }

    public static void Despawn(GameObject obj)
    {
        if (obj == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("Attempted to despawn null object.");
#endif
            return;
        }

        EnsureManagerExists();

        var pool = manager.UntrackObject(obj);
        if (pool != null)
        {
            pool.Return(obj);
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning($"Object {obj.name} doesn't belong to any pool.  Destroying instead.");
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(obj);
                return;
            }
#endif
            Object.Destroy(obj);
        }
    }
    
    public static bool IsSpawned(GameObject obj)
    {
        if (obj == null) return false;
        EnsureManagerExists();
        return manager.IsTracked(obj);
    }

    public void Clear()
    {
        while (availableObjects.Count > 0)
        {
            var obj = availableObjects.Dequeue();
            availableSeenObjects.Remove(obj);
            
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
                return;
            }
#endif
            
            if (obj != null)
            {
                Object.Destroy(obj);
            }
        }
    }

    private static void EnsureManagerExists()
    {
        if (manager == null)
            manager = PoolManager.Instance ?? Object.FindFirstObjectByType<PoolManager>() ?? new GameObject("PoolManager").AddComponent<PoolManager>();
    }
}

public class PoolManager : Singleton<PoolManager>
{
    private readonly Dictionary<GameObject, Pool> prefabToPool = new();
    private readonly Dictionary<GameObject, Pool> instanceToPool = new();
    private readonly Dictionary<GameObject, IPoolable[]> poolableCache = new();

    public Pool RegisterPool(GameObject prefab, Pool pool)
    {
        prefabToPool.TryAdd(prefab, pool);
        return prefabToPool[prefab];
    }

    public Pool GetPool(GameObject prefab) => prefabToPool.GetValueOrDefault(prefab);

    public void TrackObject(GameObject obj, Pool pool) => instanceToPool[obj] = pool;

    public Pool UntrackObject(GameObject obj) =>
        !instanceToPool.Remove(obj, out var pool) ? null : pool;

    public bool IsTracked(GameObject obj) => instanceToPool.ContainsKey(obj);

    public void NotifySpawn(GameObject obj)
    {
        if (obj == null) return;
        var poolables = GetPoolableComponents(obj);
        foreach (var poolable in poolables)
            poolable.OnSpawnFromPool();
    }

    public void NotifyReturn(GameObject obj)
    {
        if (obj == null) return;
        var poolables = GetPoolableComponents(obj);
        foreach (var poolable in poolables)
            poolable.OnReturnToPool();
    }

    private IPoolable[] GetPoolableComponents(GameObject obj)
    {
        if (poolableCache.TryGetValue(obj, out var poolables)) return poolables;
        
        poolables = obj.GetComponents<IPoolable>();
        poolableCache[obj] = poolables;
        return poolables;
    }

#if VAHTYAH_CUSTOM_INSPECTOR
    [Button]
#endif
    public void ClearAllPools()
    {
        foreach (var pool in prefabToPool.Values)
        {
            pool.Clear();
        }

        poolableCache.Clear();
        prefabToPool.Clear();
        instanceToPool.Clear();
        
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }
#endif
    }

    protected override void OnDestroy()
    {
        ClearAllPools();
        base.OnDestroy();
    }
}

public static class PoolExtensions
{
    public static bool IsSpawnedFromPool(this GameObject obj)
    {
        return Pool.IsSpawned(obj);
    }
    
    public static bool IsInPool(this GameObject obj)
    {
        return !Pool.IsSpawned(obj);
    }
}