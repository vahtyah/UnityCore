using System;
using UnityEngine;

namespace VahTyah
{
    public struct FeatureRefresh : IEvent { }

    public struct FeatureState : IEvent
    {
        public bool Active;
        public int Index;
        public float ProgressMin;
        public float ProgressMax;
        public bool Unlocked;
        public Sprite Icon;
        public Sprite IconDark;
        public string ConditionText;
    }

    public struct FeatureUnlocked : IEvent { public int Index; }
    public struct FeaturePendingUnlock : IEvent { public int Index; }
    public struct FeatureConsumePending : IEvent { public Action<bool> Reply; }
}
