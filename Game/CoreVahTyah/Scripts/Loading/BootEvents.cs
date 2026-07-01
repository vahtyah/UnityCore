namespace VahTyah
{
    /// <summary>Loading bar đã chạy xong đoạn intro (0→intro target) theo thời gian.
    /// Bootstrap chờ event này (sau khi InitModules xong) rồi mới cho load scene.</summary>
    public struct BootIntroReady : IEvent { }

    /// <summary>Boot hoàn tất (đã init module + load scene đầu). View loading nghe để ẩn.</summary>
    public struct BootCompleted : IEvent { }
}
