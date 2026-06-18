using System;
using UnityEngine;

namespace VahTyah
{
    [Serializable]
    public class FeatureDefinition
    {
        public string Name;
        [Min(1)] public int LevelMin;
        [Min(1)] public int LevelMax;
        public Sprite Icon;
        public Sprite IconDark;
        public string ConditionText;
        public string UnlockTitle;
        public string UnlockDescription;
    }
}
