using System;
using UnityEngine;

namespace VahTyah
{
    public struct HeartAdd : IEvent { public int Value; public bool Direct; }
    public struct HeartUse : IEvent { public int Value; public Action<bool> Reply; }
    public struct HeartGet : IEvent { public Action<int> Reply; }
    public struct HeartIsFull : IEvent { public Action<bool> Reply; }
    public struct HeartIsInfinity : IEvent { public Action<bool> Reply; }
    public struct HeartGetTimer : IEvent { public Action<string> Reply; }
    public struct HeartAddInfinity : IEvent { public float Minutes; }
    public struct HeartGetInfinityTimer : IEvent { public Action<string> Reply; }
    public struct HeartChanged : IEvent { }
    public struct HeartInfinityChanged : IEvent { }

    /// <summary>Thu tim có animation bay về HeartDisplay (mua bundle, reward...). Mỗi tim đáp → +1.
    /// Direct = cộng vượt MaxHearts. Không có ItemDisplay/prefab → cộng thẳng.</summary>
    public struct HeartCollect : IEvent { public Transform From; public int Value; public bool Direct; }
}
