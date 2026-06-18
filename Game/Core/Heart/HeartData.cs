using System;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Cấu hình tốc độ hồi tim cho module Heart (dữ liệu có thể chỉnh trong Inspector / remote config).
    /// </summary>
    [Serializable]
    public class HeartData : ISAModuleData
    {
        // Số phút để hồi 1 tim
        [Min(0.01f)]
        public float minutesPerHeart = 1f;

        // Số tim tối đa
        [Min(1f)]
        public int maxHearts = 5;
    }

    /// <summary>
    /// Dữ liệu lưu trữ trạng thái tim của người chơi (số tim hiện có, mốc thời gian hồi, trạng thái tim vô hạn).
    /// </summary>
    [Serializable]
    internal class HeartSaveData
    {
        // (số tim đang có, số tim được tích trữ thêm khi đầy)
        public (int have, int saved) hearts = (have: 5, saved: 0);

        // Mốc Stopwatch tick lần cuối cập nhật tim
        public long lastHeartTick;

        // Mốc thời gian thực (DateTime.ToBinary) lần cuối cập nhật - dùng để chống chỉnh giờ hệ thống
        public long lastHeartDateBin;

        // (tick kết thúc tim vô hạn đang dùng, tick tích trữ thêm)
        public (long have, long saved) infinityEndTick;

        // Mốc thời gian thực kết thúc tim vô hạn (DateTime.ToBinary)
        public long infinityEndDateBin;
    }
}
