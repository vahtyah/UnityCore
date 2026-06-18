using System;

namespace VahTyah
{
    /// <summary>Rung 1 lần (đồng bộ). Force = bỏ qua cooldown.</summary>
    public struct HapticPlay : IEvent { public HapticType Type; public bool Force; }

    /// <summary>Rung tuần tự nhiều loại, có gap giữa các lần (async).</summary>
    public struct HapticSequence : IEvent { public HapticType[] Types; public bool Force; }

    /// <summary>Bật/tắt haptic (lưu lại).</summary>
    public struct HapticSetActive : IEvent { public bool Active; }

    /// <summary>Thông báo trạng thái bật/tắt đã đổi.</summary>
    public struct HapticChanged : IEvent { public bool Active; }

    /// <summary>Query trạng thái bật/tắt hiện tại.</summary>
    public struct HapticGet : IEvent { public Action<bool> Reply; }
}
