using System;

namespace VahTyah
{
    // Cả hai event đều theo pattern "set-hoặc-get": Value có giá trị → ghi cờ; luôn Reply cờ đang lưu.
    // KHÔNG event nào hiện popup — popup UMP/ATT do SDK ads bên ngoài gọi, rồi publish kết quả vào đây.
    // Xem Docs/MODULES.md → ModuleConsent (mục ⚠️) trước khi đổi bất cứ thứ gì.

    /// <summary>Cờ đồng ý UMP/GDPR (cá nhân hoá quảng cáo). Set nếu Value có; Reply cờ đang lưu.</summary>
    public struct ConsentUMPGranted : IEvent { public bool? Value; public Action<bool> Reply; }

    /// <summary>Cờ ATT (iOS App Tracking Transparency). Nền tảng ≠ iOS → luôn Reply(true).</summary>
    public struct ConsentATTGranted : IEvent { public bool? Value; public Action<bool> Reply; }
}
