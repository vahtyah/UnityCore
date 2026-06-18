using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Bộ điều phối khởi động (bootstrap) của framework.
    /// Là singleton bền (DontDestroyOnLoad). Khi Awake -> nạp các module trong Config
    /// theo Priority, gọi InitializeAsync() rồi Subscribe(), cuối cùng phát "SA.AppReady".
    /// </summary>
    public class SAManager : SASingleton<SAManager>
    {
        public SAModuleConfig Config;

        protected override void OnInitialize() => BootAsync();

        private async Task BootAsync()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            SAModule[] modules = Config != null
                ? Config.Modules.Where(m => m != null).ToArray()
                : Array.Empty<SAModule>();
            bool debugLogs = Config != null && Config.DebugLogs;

            if (modules.Length == 0)
                Debug.LogWarning("[SA] No modules configured.");

            // Nhóm theo Priority, chạy nhóm có Priority nhỏ trước.
            foreach (var group in modules.GroupBy(m => m.Priority).OrderBy(g => g.Key))
            {
                foreach (SAModule m in group)
                {
                    await RunSafe(m, debugLogs);
                    RunSubscribeSafe(m, debugLogs);
                }
            }

            if (debugLogs)
                Debug.Log("[SA] Boot complete.");

            await SATypedBus.Publish(new Ev.AppReady());
        }

        // Bọc InitializeAsync trong try/catch để 1 module lỗi không làm chết cả boot.
        private static async Task RunSafe(SAModule m, bool debugLogs)
        {
            try
            {
                await m.InitializeAsync();
                if (debugLogs) Debug.Log("[SA] InitializeAsync " + m.name);
            }
            catch (Exception e)
            {
                LogModuleError(m.name, "InitializeAsync", e);
            }
        }

        private static void RunSubscribeSafe(SAModule m, bool debugLogs)
        {
            try
            {
                m.Subscribe();
                if (debugLogs) Debug.Log("[SA] Subscribe " + m.name);
            }
            catch (Exception e)
            {
                LogModuleError(m.name, "Subscribe", e);
            }
        }

        // Log lỗi module ở dạng dễ đọc: loại lỗi, lý do, gợi ý, dòng code đầu tiên của user.
        private static void LogModuleError(string moduleName, string phase, Exception e)
        {
            string inner = e.InnerException != null
                ? $"\n  Inner: [{e.InnerException.GetType().Name}] {e.InnerException.Message}"
                : string.Empty;
            string hint = e is NullReferenceException
                ? "\n  Hint:  A variable is null on the line below — check every object/field access on that line."
                : string.Empty;
            string frame = ExtractFirstUserFrame(e.StackTrace);
            string where = frame != null ? "\n  Where: " + frame : string.Empty;

            Debug.LogError(
                $"[SA] ✗ {moduleName}.{phase} failed" +
                $"\n  Type:  {e.GetType().Name}" +
                $"\n  Why:   {e.Message}{inner}{hint}{where}" +
                $"\n  Stack: {e.StackTrace}");
        }

        // Tìm dòng stack trace đầu tiên thuộc code người dùng (bỏ qua UnityEngine/System/SAManager).
        private static string ExtractFirstUserFrame(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return null;

            foreach (string line in stackTrace.Split('\n'))
            {
                string t = line.Trim();
                if (!string.IsNullOrEmpty(t)
                    && !t.Contains("UnityEngine.")
                    && !t.Contains("System.")
                    && !t.Contains("StandardAssets.SAManager")
                    && t.Contains(".cs:"))
                {
                    return t;
                }
            }
            return null;
        }

        // Chuyển tiếp vòng đời app vào SATypedBus.
        private void OnApplicationPause(bool paused)
        {
            if (paused)
                SATypedBus.Publish(new Ev.AppPaused());
            else
                SATypedBus.Publish(new Ev.AppResumed());
        }

        private void OnApplicationQuit()
            => SATypedBus.Publish(new Ev.AppQuitting());

        private void OnValidate()
        {
            if (gameObject.name != "StandardAssets")
                gameObject.name = "StandardAssets";
        }
    }
}
