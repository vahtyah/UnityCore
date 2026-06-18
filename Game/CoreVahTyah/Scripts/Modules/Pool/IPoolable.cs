namespace VahTyah
{
    /// <summary>Object trong pool implement để reset/chuẩn bị state khi spawn/return.</summary>
    public interface IPoolable
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
    }
}
