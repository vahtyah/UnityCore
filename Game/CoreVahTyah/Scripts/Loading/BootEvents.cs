namespace VahTyah
{
    /// <summary>Tiến độ boot (0..1). View loading nghe để cập nhật thanh.</summary>
    public struct BootProgress : IEvent { public float Value; public string Message; }

    /// <summary>Boot hoàn tất (đã init module + load scene đầu). View loading nghe để ẩn.</summary>
    public struct BootCompleted : IEvent { }
}
