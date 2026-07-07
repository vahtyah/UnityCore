namespace VahTyah
{
    public struct SceneLoadRequest : IEvent { public int Index; }
    public struct SceneUnloading : IEvent { }
    public struct SceneLoaded : IEvent { public int Index; }
}
