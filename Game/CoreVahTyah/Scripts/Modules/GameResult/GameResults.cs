using Cysharp.Threading.Tasks;

namespace VahTyah
{
    /// Facade gọn cho ModuleGameResult — tránh new event + .Forget() rải rác ở gameplay.
    public static class GameResults
    {
        /// Chấm điều kiện Win (vd sau mỗi move).
        public static void CheckWin() => EventBus.Publish(new GameResultCheckWin()).Forget();

        /// Chấm điều kiện Lose.
        public static void CheckLose() => EventBus.Publish(new GameResultCheckLose()).Forget();

        /// Chấm cả hai. Lose trước — thua chặn luôn thắng cùng nhịp (tuỳ game đổi thứ tự nếu cần).
        public static void Check()
        {
            CheckLose();
            CheckWin();
        }

        /// Override handler runtime (vd LevelEditor).
        public static void SetHandler(IGameResultHandler handler)
            => EventBus.Publish(new GameResultSetHandler { Handler = handler }).Forget();

        /// Xoá override, quay về handler mặc định của module.
        public static void ClearHandler() => SetHandler(null);
    }
}
