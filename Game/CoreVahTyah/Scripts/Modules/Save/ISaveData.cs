namespace VahTyah
{
    public interface ISaveData
    {
        int Version { get; }
        void OnAfterLoad();
        void OnBeforeSave();
    }
}
