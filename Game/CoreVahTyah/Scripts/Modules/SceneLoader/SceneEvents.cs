namespace VahTyah
{
    public struct SceneLoadRequest : IEvent { public int Index; }
    public struct LoadEntryScene : IEvent { } // vào game lần đầu; SceneLoader tự quyết index
    public struct SceneUnloading : IEvent { }
    public struct SceneLoaded : IEvent { public int Index; }
}
