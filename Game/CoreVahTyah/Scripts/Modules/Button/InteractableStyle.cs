namespace VahTyah
{
    /// <summary>
    /// Shortcut tĩnh tra style feedback nhấn — gọi thẳng <see cref="InteractableStyleService"/> (không qua EventBus).
    /// Trả null nếu chưa có <see cref="ModuleInteractable"/> trong ModuleConfig; component tự fallback code-default.
    /// </summary>
    public static class InteractableStyle
    {
        public static InteractableStyleProfile Get(InteractableStyleId id)
            => Services.Get<InteractableStyleService>()?.Get(id);
    }
}
