using System.Collections.Generic;

namespace VahTyah
{
    /// <summary>
    /// Bảng tra style panel dùng chung, HAI loại riêng biệt: <see cref="PopupStyle"/> (scale + fade)
    /// và <see cref="FadeStyle"/> (fade-only). Đăng ký bởi <see cref="ModulePanel"/>. Truy cập qua shortcut
    /// <see cref="PanelStyle"/>. Mỗi loại fallback về Default (Id 0). Không state runtime.
    /// </summary>
    public sealed class PanelStyleService
    {
        private readonly Dictionary<int, PopupStyle> _popups = new Dictionary<int, PopupStyle>();
        private readonly Dictionary<int, FadeStyle> _fades = new Dictionary<int, FadeStyle>();
        private readonly PopupStyle _popupFallback;
        private readonly FadeStyle _fadeFallback;

        public PanelStyleService(IReadOnlyList<PopupStyle> popups, IReadOnlyList<FadeStyle> fades)
        {
            if (popups != null)
                for (int i = 0; i < popups.Count; i++)
                {
                    var p = popups[i];
                    if (p == null) continue;
                    if (_popups.ContainsKey((int)p.Id))
                        Debug.LogWarning($"[PanelStyle] Duplicate popup style '{p.Id}' — dùng bản khai báo sau.");
                    _popups[(int)p.Id] = p; // last-wins
                }

            if (fades != null)
                for (int i = 0; i < fades.Count; i++)
                {
                    var f = fades[i];
                    if (f == null) continue;
                    if (_fades.ContainsKey((int)f.Id))
                        Debug.LogWarning($"[PanelStyle] Duplicate fade style '{f.Id}' — dùng bản khai báo sau.");
                    _fades[(int)f.Id] = f; // last-wins
                }

            _popups.TryGetValue((int)PopupStyleId.Default, out _popupFallback);
            _fades.TryGetValue((int)FadeStyleId.Default, out _fadeFallback);
            if (_popupFallback == null) Debug.LogWarning("[PanelStyle] Không có PopupStyle 'Default'.");
            if (_fadeFallback == null) Debug.LogWarning("[PanelStyle] Không có FadeStyle 'Default'.");
        }

        /// <summary>PopupStyle cho <paramref name="id"/>; thiếu → Default (null nếu Default cũng thiếu).</summary>
        public PopupStyle GetPopup(PopupStyleId id)
            => _popups.TryGetValue((int)id, out var p) ? p : _popupFallback;

        /// <summary>FadeStyle cho <paramref name="id"/>; thiếu → Default (null nếu Default cũng thiếu).</summary>
        public FadeStyle GetFade(FadeStyleId id)
            => _fades.TryGetValue((int)id, out var f) ? f : _fadeFallback;
    }
}
