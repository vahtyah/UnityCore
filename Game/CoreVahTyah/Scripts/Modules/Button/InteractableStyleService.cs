using System.Collections.Generic;

namespace VahTyah
{
    /// <summary>
    /// Tra <see cref="InteractableStyleProfile"/> theo <see cref="InteractableStyleId"/>, O(1). Đăng ký vào
    /// <see cref="Services"/> bởi <see cref="ModuleInteractable"/>. Không state runtime, không phụ thuộc module
    /// khác — chỉ là bảng tra style. Truy cập qua shortcut <see cref="InteractableStyle"/>.
    /// </summary>
    public sealed class InteractableStyleService
    {
        private readonly Dictionary<int, InteractableStyleProfile> _byId = new Dictionary<int, InteractableStyleProfile>();
        private readonly InteractableStyleProfile _fallback;

        public InteractableStyleService(IReadOnlyList<InteractableStyleProfile> profiles)
        {
            if (profiles != null)
                for (int i = 0; i < profiles.Count; i++)
                {
                    var p = profiles[i];
                    if (p == null) continue;
                    if (_byId.ContainsKey((int)p.Id))
                        Debug.LogWarning($"[InteractableStyle] Duplicate style '{p.Id}' — dùng bản khai báo sau.");
                    _byId[(int)p.Id] = p; // last-wins
                }

            _byId.TryGetValue((int)InteractableStyleId.Default, out _fallback);
            if (_fallback == null)
                Debug.LogWarning("[InteractableStyle] Không có profile 'Default' — Get() trả null khi thiếu id.");
        }

        /// <summary>Profile cho <paramref name="id"/>; thiếu → Default (null nếu Default cũng thiếu).</summary>
        public InteractableStyleProfile Get(InteractableStyleId id)
            => _byId.TryGetValue((int)id, out var p) ? p : _fallback;

        public bool Has(InteractableStyleId id) => _byId.ContainsKey((int)id);
    }
}
