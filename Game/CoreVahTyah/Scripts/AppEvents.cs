namespace VahTyah
{
    // Vòng đời ứng dụng, do Bootstrap phát qua OnApplicationPause/OnApplicationQuit.
    // Dùng cho các module cần phản ứng khi người chơi rời/quay lại game (vd Notifications
    // lên lịch re-engagement khi Paused/Quitting, huỷ khi Resumed).

    /// <summary>App bị đưa ra nền (Android home, cuộc gọi, khoá màn hình...).</summary>
    public struct AppPaused : IEvent { }

    /// <summary>App quay lại foreground.</summary>
    public struct AppResumed : IEvent { }

    /// <summary>App sắp thoát hẳn (chỉ chắc chắn bắn trên một số nền tảng — Android thường chỉ có Paused).</summary>
    public struct AppQuitting : IEvent { }
}
