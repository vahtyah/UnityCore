using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Gắn lên GameObject để gán nó vào một hoặc nhiều "nhóm UI" (UIGroupId).
    /// Object hiện nếu BẤT KỲ group nào của nó đang được Show (OR semantics).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIGroup : MonoBehaviour
    {
        [SerializeField] private UIGroupId[] _groups;

        public UIGroupId[] Groups => _groups;

        private UIGroupService _service;

        private void Awake()
        {
            if (Services.TryGet(out _service))
                _service.Add(this);
            else
                Debug.LogError("[UIGroup] UIGroupService chưa được đăng ký. Đảm bảo ModuleUIGroup boot trước khi scene UI load.");
        }

        private void OnDestroy()
        {
            _service?.Remove(this);
            _service = null;
        }

        /// <summary>Đổi danh sách group lúc runtime (tự re-register).</summary>
        public void SetGroups(params UIGroupId[] groups)
        {
            _service?.Remove(this);
            _groups = groups;
            _service?.Add(this);
        }

        internal void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
