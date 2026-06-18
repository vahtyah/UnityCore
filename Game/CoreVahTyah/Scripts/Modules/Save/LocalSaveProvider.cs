using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    public sealed class LocalSaveProvider : ISaveProvider
    {
        private string _rootPath;

        public string Name => "Local";
        public bool IsAvailable => true;

        public Task InitializeAsync()
        {
            _rootPath = Application.persistentDataPath;
            return Task.CompletedTask;
        }

        public Task SaveAsync<T>(string key, T data) where T : class
        {
            string json = JsonUtility.ToJson(data, false);
            string path = GetPath(key);

            var tcs = new TaskCompletionSource<bool>();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    string tempPath = path + ".tmp";
                    File.WriteAllText(tempPath, json);
                    if (File.Exists(path))
                        File.Delete(path);
                    File.Move(tempPath, path);
                    tcs.SetResult(true);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            return tcs.Task;
        }

        public Task<T> LoadAsync<T>(string key) where T : class, new()
        {
            string path = GetPath(key);
            if (!File.Exists(path))
                return Task.FromResult(new T());

            try
            {
                string json = File.ReadAllText(path);
                T result = JsonUtility.FromJson<T>(json);
                return Task.FromResult(result ?? new T());
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to load '{key}': {e.Message}");
                return Task.FromResult(new T());
            }
        }

        public Task DeleteAsync(string key)
        {
            string path = GetPath(key);
            if (File.Exists(path))
                File.Delete(path);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key)
        {
            return Task.FromResult(File.Exists(GetPath(key)));
        }

        private string GetPath(string key) => Path.Combine(_rootPath, key + ".json");
    }
}
