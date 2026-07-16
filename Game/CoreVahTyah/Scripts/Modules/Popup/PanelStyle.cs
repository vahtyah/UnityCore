namespace VahTyah
{
    /// <summary>
    /// Shortcut tĩnh tra style panel — gọi thẳng <see cref="PanelStyleService"/> (không qua EventBus).
    /// Trả null nếu chưa có <see cref="ModulePanel"/> trong ModuleConfig; animator tự fallback code-default.
    /// </summary>
    public static class PanelStyle
    {
        public static PopupStyle Popup(PopupStyleId id)
            => Services.Get<PanelStyleService>()?.GetPopup(id);

        public static FadeStyle Fade(FadeStyleId id)
            => Services.Get<PanelStyleService>()?.GetFade(id);
    }
}
