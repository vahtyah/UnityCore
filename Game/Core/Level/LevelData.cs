using System;
using System.Collections.Generic;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>Cấu hình tiến trình level (số level, level không lặp, các chuỗi sequence).</summary>
    [Serializable]
    public class LevelData : ISAModuleData
    {
        [Min(0f)]
        [Tooltip("Total number of unique level assets.")]
        public int totalLevels = 10;

        [Tooltip("Level number ranges (1-based, inclusive) that are played only once and excluded from the loop.")]
        public List<LevelRange> nonLoopLevels = new List<LevelRange>();

        [Tooltip("Groups of level indices played as one run. No win screen fires inside a sequence — only at the last index.")]
        public List<LevelSequence> sequences = new List<LevelSequence>();
    }

    /// <summary>Khoảng level [from..to] (1-based, bao gồm cả 2 đầu).</summary>
    [Serializable]
    public class LevelRange
    {
        [Min(1f)]
        [Tooltip("First level in this range (1-based, inclusive).")]
        public int from = 1;

        [Min(1f)]
        [Tooltip("Last level in this range (1-based, inclusive).")]
        public int to = 1;

        public bool Contains(int level) => level >= from && level <= to;
    }

    /// <summary>Chuỗi level chơi liền như 1 "run" — chỉ hiện win screen ở level cuối chuỗi.</summary>
    [Serializable]
    public class LevelSequence
    {
        [Min(1f)]
        [Tooltip("First level in this sequence (1-based).")]
        public int from = 1;

        [Min(1f)]
        [Tooltip("Last level in this sequence (1-based, inclusive).")]
        public int to = 1;
    }

    /// <summary>Dữ liệu lưu của người chơi: level hiện tại + số lần thử ở run hiện tại.</summary>
    [Serializable]
    internal class LevelSaveData
    {
        public int level = 1;
        public int currentRunTries = 0;
    }

    /// <summary>Nhóm UI dùng cho phần hiển thị level (đăng ký theo Category "UIGroup").</summary>
    [SAEnum("UIGroup")]
    public enum UILevel
    {
        LevelDisplay
    }
}
