using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    public sealed class LocalSaveProvider : ISaveProvider
    {
        private string _rootPath;

        public string Name => "Local";
        public bool IsAvailable => true;

        public UniTask InitializeAsync()
        {
            _rootPath = Application.persistentDataPath;
            return UniTask.CompletedTask;
        }

        public async UniTask SaveAsync<T>(string key, T data) where T : class
        {
            string json = JsonUtility.ToJson(data, false);
            string path = GetPath(key);

            await UniTask.SwitchToThreadPool();
            try
            {
                string tempPath = path + ".tmp";
                File.WriteAllText(tempPath, json);
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(tempPath, path);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }

        public UniTask<T> LoadAsync<T>(string key) where T : class, new()
        {
            string path = GetPath(key);
            if (!File.Exists(path))
                return UniTask.FromResult(new T());

            try
            {
                string json = File.ReadAllText(path);
                T result = JsonUtility.FromJson<T>(json);
                return UniTask.FromResult(result ?? new T());
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to load '{key}': {e.Message}");
                return UniTask.FromResult(new T());
            }
        }

        public UniTask DeleteAsync(string key)
        {
            string path = GetPath(key);
            if (File.Exists(path))
                File.Delete(path);
            return UniTask.CompletedTask;
        }

        public UniTask<bool> ExistsAsync(string key)
        {
            return UniTask.FromResult(File.Exists(GetPath(key)));
        }

        private string GetPath(string key) => Path.Combine(_rootPath, key + ".json");
    }
}
