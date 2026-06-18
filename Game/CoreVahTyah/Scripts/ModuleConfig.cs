using System;
using UnityEngine;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Module Config", fileName = "ModuleConfig")]
    public class ModuleConfig : ScriptableObject
    {
        [Tooltip("Log khởi tạo từng module.")]
        public bool DebugLogs = false;

        public Module[] Modules = Array.Empty<Module>();
    }
}
