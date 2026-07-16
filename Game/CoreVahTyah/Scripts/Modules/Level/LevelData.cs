using System;
using System.Collections.Generic;
using UnityEngine;

namespace VahTyah
{
    [Serializable]
    public class LevelRange
    {
        [Min(1)] public int From = 1;
        [Min(1)] public int To = 1;

        public bool Contains(int level) => level >= From && level <= To;
    }

    [Serializable]
    public class LevelSaveData
    {
        public int Level = 1;
        public int Tries = 0;
    }
}
