using System;

namespace VahTyah
{
    public struct MusicPlay : IEvent { public MusicId Id; }    // chuyển sang track (crossfade)
    public struct MusicStop : IEvent { }                       // fade out
    public struct MusicSetVolume : IEvent { public float Volume; }
    public struct MusicSetActive : IEvent { public bool Active; }
    public struct MusicChanged : IEvent { public bool Active; public float Volume; }
    public struct MusicGet : IEvent { public Action<bool, float> Reply; } // (active, volume)
}
