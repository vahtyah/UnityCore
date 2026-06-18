using System;
using System.Collections.Generic;

namespace VahTyah
{
    public struct LevelStarted : IEvent { public Dictionary<string, object> Extra; }
    public struct LevelCompleted : IEvent { public bool ShowScreen; public float ShowDelay; public Dictionary<string, object> Extra; }
    public struct LevelFailed : IEvent { public bool ShowScreen; public float ShowDelay; public Dictionary<string, object> Extra; }
    public struct LevelSet : IEvent { public int Level; }
    public struct LevelChanged : IEvent { public int Level; }
    public struct LevelGet : IEvent { public Action<int> Reply; }
    public struct LevelGetIndex : IEvent { public Action<int> Reply; }
    public struct LevelGetTries : IEvent { public Action<int> Reply; }
}
