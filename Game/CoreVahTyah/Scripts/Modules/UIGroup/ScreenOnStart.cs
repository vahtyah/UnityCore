using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Đặt trong scene để tự chuyển sang màn chỉ định khi scene bắt đầu.
    /// Start() (không phải Awake) để mọi UIGroup trong scene đã kịp đăng ký.
    /// </summary>
    public class ScreenOnStart : MonoBehaviour
    {
        [SerializeField] private UIGroupId _screen = UIGroupId.Gameplay;

        private void Start() => EventBus.Publish(new ScreenRequest { Screen = _screen });
    }
}
