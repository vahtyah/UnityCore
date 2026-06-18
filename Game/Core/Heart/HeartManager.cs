using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// MonoBehaviour chạy nền: định kỳ kiểm tra và tính toán hồi tim khi chưa đầy và không ở chế độ tim vô hạn.
    /// </summary>
    public class HeartManager : MonoBehaviour
    {
        private ModuleHeart heartModule;

        // Khoảng thời gian (giây) giữa 2 lần kiểm tra hồi tim
        private const float RegenCheckInterval = 1f;

        private float regenCheckTimer = 0f;

        internal void Initialize(ModuleHeart module)
        {
            heartModule = module;
        }

        private void Update()
        {
            if (!heartModule.IsFull() && !heartModule.IsInfinityHeart())
            {
                // Dùng unscaledDeltaTime để không bị ảnh hưởng bởi Time.timeScale
                regenCheckTimer += Time.unscaledDeltaTime;
                if (!(regenCheckTimer < RegenCheckInterval))
                {
                    regenCheckTimer = 0f;
                    heartModule.CalculateRegeneration();
                }
            }
        }
    }
}
