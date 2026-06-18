using System.Collections.Generic;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Pool theo prefab-key. Queue object rảnh (O(1)), track instance→pool để Despawn tự tìm pool,
    /// gọi IPoolable khi spawn/return, reset transform khi trả về.
    /// </summary>
    public class PoolService
    {
        private sealed class Entry
        {
            public Transform Container;
            public readonly Queue<GameObject> Available = new Queue<GameObject>();
            public readonly HashSet<GameObject> InPool = new HashSet<GameObject>(); // chống despawn trùng
        }

        private readonly Dictionary<GameObject, Entry> _byPrefab = new Dictionary<GameObject, Entry>();
        private readonly Dictionary<GameObject, Entry> _byInstance = new Dictionary<GameObject, Entry>();
        private readonly Dictionary<GameObject, IPoolable[]> _poolableCache = new Dictionary<GameObject, IPoolable[]>();
        private readonly List<GameObject> _returnBuffer = new List<GameObject>(64);
        private readonly Transform _root;

        internal PoolService(Transform root) => _root = root;

        public void Register(GameObject prefab, int prewarm = 0)
        {
            var entry = GetOrCreate(prefab);
            for (int i = 0; i < prewarm; i++)
            {
                var obj = Object.Instantiate(prefab, entry.Container);
                obj.SetActive(false);
                entry.Available.Enqueue(obj);
                entry.InPool.Add(obj);
            }
        }

        public GameObject Spawn(GameObject prefab) => Spawn(prefab, Vector3.zero, Quaternion.identity, null);
        public GameObject Spawn(GameObject prefab, Transform parent) => Spawn(prefab, Vector3.zero, Quaternion.identity, parent);

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var entry = GetOrCreate(prefab);
            var targetParent = parent != null ? parent : entry.Container;

            GameObject obj = null;
            while (entry.Available.Count > 0)
            {
                obj = entry.Available.Dequeue();
                entry.InPool.Remove(obj);   // gỡ khỏi InPool dù giữ hay bỏ (tránh dead-ref tồn đọng)
                if (obj != null) break;     // tìm được object còn sống
            }

            if (obj == null)                // hết object rảnh (hoặc toàn dead) → tạo mới
                obj = Object.Instantiate(prefab);

            obj.transform.SetParent(targetParent, false);
            obj.transform.SetPositionAndRotation(position, rotation);

            _byInstance[obj] = entry;
            obj.SetActive(true);
            Notify(obj, true);
            return obj;
        }

        public T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            var go = Spawn(prefab, position, rotation, parent);
            return go != null ? go.GetComponent<T>() : null;
        }

        public void Despawn(GameObject obj)
        {
            if (ReferenceEquals(obj, null)) // null thật (C# null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[Pool] Despawn object null.");
#endif
                return;
            }

            bool tracked = _byInstance.TryGetValue(obj, out var entry);

            if (obj == null) // Unity-dead (ref còn nhưng đã destroy ngoài pool)
            {
                if (tracked) // dọn key chết khỏi tracking
                {
                    _byInstance.Remove(obj);
                    _poolableCache.Remove(obj);
                }
                return;
            }

            if (!tracked) // object lạ còn sống → không thuộc pool nào
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[Pool] '{obj.name}' không thuộc pool nào — destroy thay vì trả.");
#endif
                DestroyObject(obj);
                return;
            }

            if (entry.InPool.Contains(obj)) return; // đã trả rồi

            _byInstance.Remove(obj);
            Notify(obj, false);

            obj.SetActive(false);
            obj.transform.SetParent(entry.Container, false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            entry.Available.Enqueue(obj);
            entry.InPool.Add(obj);
        }

        public bool IsSpawned(GameObject obj) => obj != null && _byInstance.ContainsKey(obj);

        /// <summary>Trả mọi object đang spawn về pool (gọi khi đổi scene).</summary>
        public void ReturnAll()
        {
            if (_byInstance.Count == 0) return;

            _returnBuffer.Clear();
            _returnBuffer.AddRange(_byInstance.Keys);
            for (int i = 0; i < _returnBuffer.Count; i++)
                Despawn(_returnBuffer[i]);
        }

        public void DestroyPool(GameObject prefab)
        {
            if (!_byPrefab.TryGetValue(prefab, out var entry)) return;
            if (entry.Container != null)
                DestroyObject(entry.Container.gameObject);
            _byPrefab.Remove(prefab);
        }

        public void Clear()
        {
            foreach (var entry in _byPrefab.Values)
                if (entry.Container != null)
                    DestroyObject(entry.Container.gameObject);

            _byPrefab.Clear();
            _byInstance.Clear();
            _poolableCache.Clear();
        }

        // Destroy hoạt động đúng cả trong edit mode (Destroy thường không hiệu lực ngay khi không Play).
        private static void DestroyObject(Object o)
        {
            if (o == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(o);
                return;
            }
#endif
            Object.Destroy(o);
        }

        private Entry GetOrCreate(GameObject prefab)
        {
            if (_byPrefab.TryGetValue(prefab, out var entry)) return entry;

            entry = new Entry { Container = new GameObject($"[Pool] {prefab.name}").transform };
            entry.Container.SetParent(_root);
            _byPrefab[prefab] = entry;
            return entry;
        }

        private void Notify(GameObject obj, bool spawned)
        {
            var arr = GetPoolables(obj);
            for (int i = 0; i < arr.Length; i++)
            {
                if (spawned) arr[i].OnSpawnFromPool();
                else arr[i].OnReturnToPool();
            }
        }

        private IPoolable[] GetPoolables(GameObject obj)
        {
            if (_poolableCache.TryGetValue(obj, out var arr)) return arr;
            arr = obj.GetComponents<IPoolable>();
            _poolableCache[obj] = arr;
            return arr;
        }
    }
}
