using Cysharp.Threading.Tasks;

namespace VahTyah
{
    public interface ISaveProvider
    {
        string Name { get; }
        bool IsAvailable { get; }

        UniTask InitializeAsync();
        UniTask SaveAsync<T>(string key, T data) where T : class;

        /// <summary>Ghi đồng bộ (blocking) — dùng cho flush lúc quit/pause khi không thể chờ async.</summary>
        void Save<T>(string key, T data) where T : class;

        UniTask<T> LoadAsync<T>(string key) where T : class, new();
        UniTask DeleteAsync(string key);
        UniTask<bool> ExistsAsync(string key);
    }
}
