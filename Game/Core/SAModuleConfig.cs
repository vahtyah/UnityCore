using System;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Asset cấu hình: danh sách module mà SAManager sẽ khởi động khi game chạy.
    /// Tạo qua menu: Create > SA > Module Config.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Module Config", fileName = "Config_Main")]
    public class SAModuleConfig : ScriptableObject
    {
        [Tooltip("Logs InitializeAsync/Subscribe lifecycle for each module.")]
        public bool DebugLogs = false;

        public SAModule[] Modules = Array.Empty<SAModule>();
    }
}
