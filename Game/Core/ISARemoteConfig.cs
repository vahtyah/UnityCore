namespace StandardAssets
{
    /// <summary>
    /// Module nào cần đọc Remote Config thì implement interface này:
    /// - RemoteConfigKey: key trên server.
    /// - GetEditorJson(): JSON mặc định khi chạy trong Editor (chưa có server).
    /// </summary>
    public interface ISARemoteConfig
    {
        string RemoteConfigKey { get; }
        string GetEditorJson();
    }
}
