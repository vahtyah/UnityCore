using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Gắn lên GameObject để gán nó vào một "nhóm UI" (theo SAEnumRef).
    /// Lắng nghe event "UI.SetGroupVisible": nếu nhóm khớp thì bật/tắt object.
    /// Cho phép hiện/ẩn cả một nhóm UI bằng một lệnh duy nhất qua SATypedBus.
    /// </summary>
    internal sealed class SAUIGroup : MonoBehaviour
    {
        [SAEnumFilter("UIGroup")]
        public SAEnumRef Group;

        private object _tag;

        private void Awake() => EnsureSubscribed();

        internal void EnsureSubscribed()
        {
            if (_tag == null)
                _tag = SATypedBus.On<Ev.UISetGroupVisible>(OnSetGroupVisible);
        }

        private void OnDestroy() => SATypedBus.Off<Ev.UISetGroupVisible>(_tag);

        private void OnSetGroupVisible(Ev.UISetGroupVisible e)
        {
            SAEnumRef other = e.Group;
            if (Group.value == other.value && Group.typeName == other.typeName)
                gameObject.SetActive(e.Visible);
        }
    }
}
