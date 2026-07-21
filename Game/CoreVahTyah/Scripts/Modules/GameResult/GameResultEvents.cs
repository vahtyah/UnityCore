namespace VahTyah
{
    /// Gameplay publish khi muốn module chấm điều kiện Win (vd sau mỗi move).
    public struct GameResultCheckWin : IEvent { }

    /// Gameplay publish khi muốn module chấm điều kiện Lose.
    public struct GameResultCheckLose : IEvent { }

    /// Override handler lúc runtime (vd LevelEditor set handler riêng, không chạy game flow).
    /// Handler = null → xoá override, quay về handler mặc định của module.
    public struct GameResultSetHandler : IEvent { public IGameResultHandler Handler; }
}
