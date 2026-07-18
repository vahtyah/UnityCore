using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    public sealed class SaveService
    {
        private readonly List<ISaveProvider> _providers = new List<ISaveProvider>();
        private ISaveProvider _active;
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();
        private bool _dirty;

        public ISaveProvider ActiveProvider => _active;
        public bool IsDirty => _dirty;

        public void AddProvider(ISaveProvider provider)
        {
            _providers.Add(provider);
        }

        public async UniTask InitializeAsync()
        {
            foreach (var p in _providers)
            {
                try
                {
                    await p.InitializeAsync();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Save] Provider '{p.Name}' init failed: {e.Message}");
                }
            }

            _active = _providers.Find(p => p.IsAvailable);

            if (_active == null)
                Debug.LogError("[Save] No available provider.");
            else
                Debug.Log($"[Save] Active provider: {_active.Name}");
        }

        public async UniTask<T> LoadAsync<T>(string key) where T : class, new()
        {
            if (_cache.TryGetValue(key, out object cached))
                return (T)cached;

            if (_active == null)
                return new T();

            T data = await _active.LoadAsync<T>(key);

            if (data is ISaveData saveData)
                saveData.OnAfterLoad();

            _cache[key] = data;
            return data;
        }

        public T Load<T>(string key) where T : class, new()
        {
            if (_cache.TryGetValue(key, out object cached))
                return (T)cached;

            // Blocking load — dùng cho code không thể async (MonoBehaviour sync)
            T data = _active != null
                ? _active.LoadAsync<T>(key).GetAwaiter().GetResult()
                : new T();

            if (data is ISaveData saveData)
                saveData.OnAfterLoad();

            _cache[key] = data;
            return data;
        }

        public void Set<T>(string key, T data) where T : class
        {
            _cache[key] = data;
            _dirty = true;
        }

        public async UniTask SaveAsync(string key)
        {
            if (_active == null || !_cache.TryGetValue(key, out object data))
                return;

            if (data is ISaveData saveData)
                saveData.OnBeforeSave();

            await _active.SaveAsync(key, data);
        }

        public async UniTask SaveAllAsync()
        {
            if (_active == null || !_dirty)
                return;

            foreach (var kvp in _cache)
            {
                if (kvp.Value is ISaveData saveData)
                    saveData.OnBeforeSave();

                await _active.SaveAsync(kvp.Key, kvp.Value);
            }

            _dirty = false;
        }

        /// <summary>
        /// Flush đồng bộ (blocking) mọi cache dirty. Dùng ở OnApplicationQuit/Pause — async
        /// fire-and-forget không kịp chạy trước khi Editor/OS tear-down → mất save.
        /// </summary>
        public void SaveAllImmediate()
        {
            if (_active == null || !_dirty)
                return;

            foreach (var kvp in _cache)
            {
                if (kvp.Value is ISaveData saveData)
                    saveData.OnBeforeSave();

                _active.Save(kvp.Key, kvp.Value);
            }

            _dirty = false;
        }

        public async UniTask DeleteAsync(string key)
        {
            _cache.Remove(key);

            if (_active != null)
                await _active.DeleteAsync(key);
        }

        public void MarkDirty() => _dirty = true;

        public bool TryGetCached<T>(string key, out T data) where T : class
        {
            if (_cache.TryGetValue(key, out object obj))
            {
                data = (T)obj;
                return true;
            }
            data = null;
            return false;
        }

        public bool SetActiveProvider(string name)
        {
            var provider = _providers.Find(p => p.Name == name && p.IsAvailable);
            if (provider == null)
                return false;

            _active = provider;
            Debug.Log($"[Save] Switched to provider: {name}");
            return true;
        }
    }
}
