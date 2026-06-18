namespace VahTyah
{
    /// <summary>Lệnh: yêu cầu chuyển màn (ai cũng publish được, không cần biết ScreenRouter).</summary>
    public struct ScreenRequest : IEvent { public UIGroupId Screen; }

    /// <summary>Kết quả: đã đổi sang màn — UI/logic khác phản ứng.</summary>
    public struct ScreenChanged : IEvent { public UIGroupId Screen; }

    public static class ScreenRouter
    {
        public static UIGroupId Current { get; private set; } = UIGroupId.None;

        public static void GoTo(UIGroupId screen)
        {
            if (Current == screen) return;
            Current = screen;

            Services.Get<UIGroupService>().ShowExclusive(screen);
            EventBus.Publish(new ScreenChanged { Screen = screen });
        }
    }
}
