using UnityEngine;

namespace VahTyah
{
    /// <summary>Shortcut tĩnh tới PoolService (cache 1 lần, không lookup Services mỗi lần).</summary>
    public static class Pool
    {
        private static PoolService S => Services.Get<PoolService>();

        public static void Register(GameObject prefab, int prewarm = 0) => S.Register(prefab, prewarm);

        public static GameObject Spawn(GameObject prefab) => S.Spawn(prefab);
        public static GameObject Spawn(GameObject prefab, Transform parent) => S.Spawn(prefab, parent);
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            => S.Spawn(prefab, position, rotation, parent);
        public static T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
            => S.Spawn<T>(prefab, position, rotation, parent);

        public static void Despawn(GameObject obj) => S.Despawn(obj);
        public static bool IsSpawned(GameObject obj) => S.IsSpawned(obj);
        public static void ReturnAll() => S.ReturnAll();
    }

    public static class PoolExtensions
    {
        public static void Despawn(this GameObject go) => Pool.Despawn(go);
        public static bool IsSpawnedFromPool(this GameObject go) => Pool.IsSpawned(go);
        public static bool IsInPool(this GameObject go) => !Pool.IsSpawned(go);
    }
}
