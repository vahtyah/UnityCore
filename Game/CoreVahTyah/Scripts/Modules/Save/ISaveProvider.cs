using System.Threading.Tasks;

namespace VahTyah
{
    public interface ISaveProvider
    {
        string Name { get; }
        bool IsAvailable { get; }

        Task InitializeAsync();
        Task SaveAsync<T>(string key, T data) where T : class;
        Task<T> LoadAsync<T>(string key) where T : class, new();
        Task DeleteAsync(string key);
        Task<bool> ExistsAsync(string key);
    }
}
