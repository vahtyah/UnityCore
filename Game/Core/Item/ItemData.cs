using System;
using System.Collections.Generic;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Bản ghi override cho một item (khớp theo name). Phần asset Unity (icon, prefab)
    /// vẫn nằm trong ScriptableObject, ở đây chỉ chứa dữ liệu serialize được (cho remote config).
    /// </summary>
    [Serializable]
    public class ItemDefinitionEntry
    {
        public string name;
        public string title;

        [TextArea(1, 3)]
        public string description;

        public string particleEffect;
        public string collectSound;
        public HapticType haptic;
        public int startAmount;
    }

    /// <summary>
    /// Dữ liệu cấu hình module Item dùng cho remote config (SADataProvider).
    /// Chứa thông số animation và danh sách override từng item.
    /// </summary>
    [Serializable]
    public class ItemData : ISAModuleData
    {
        [Tooltip("Random radius around the start point when spawning.")]
        public float spawnRadius = 120f;

        [Tooltip("Time delay between individual items (seconds).")]
        public float staggerDelay = 0.04f;

        [Tooltip("Flight duration per item (seconds).")]
        public float duration = 1f;

        [Tooltip("Strength of the curve. Larger values = stronger curve.")]
        public float curveStrength = 400f;

        [Tooltip("Maximum number of active prefabs at the same time.")]
        public int maxPoolSize = 20;

        [Tooltip("Sorting Order of the animations canvas.")]
        public int canvasSortingOrder = 20;

        [Tooltip("Default start position for animations (screen coordinates). (0,0) = screen center.")]
        public Vector2 defaultStartPosition;

        [Tooltip("Per-item overrides. Matched by name. Unity assets (icon, prefab) stay in SO.")]
        public List<ItemDefinitionEntry> items = new List<ItemDefinitionEntry>();
    }
}
