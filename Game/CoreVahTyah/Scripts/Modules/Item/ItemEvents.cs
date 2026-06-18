using System;
using UnityEngine;

namespace VahTyah
{
    public struct ItemAdd : IEvent { public string Key; public int Value; public bool Pending; }
    public struct ItemGet : IEvent { public string Key; public bool Pending; public Action<int> Reply; }
    public struct ItemCommitPending : IEvent { public string Key; public int Value; }
    public struct ItemChanged : IEvent { public string Key; }
    public struct ItemAnimationPlay : IEvent { public string Key; public Transform From; public int Value; }
}
