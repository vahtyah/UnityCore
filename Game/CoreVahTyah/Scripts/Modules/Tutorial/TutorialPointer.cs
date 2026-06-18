using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Tiện ích trỏ tay + lỗ highlight vào một vị trí (world hoặc screen).
    /// Gắn trong prefab tutorial, gọi PointAtWorld/PointAtScreen — bỏ lặp code convert toạ độ.
    /// </summary>
    public class TutorialPointer : MonoBehaviour
    {
        [SerializeField] private RectTransform _hole;
        [SerializeField] private RectTransform _hand;
        [SerializeField] private Vector2 _handOffset;
        [Tooltip("Camera convert world→screen. Trống = Camera.main.")]
        [SerializeField] private Camera _camera;

        [Header("Hand Pulse")]
        [SerializeField] private bool _pulse = true;
        [SerializeField] private float _pulseMinScale = 0.9f;
        [SerializeField] private float _pulseSpeed = 3f;

        private Canvas _canvas;
        private Vector3 _handBaseScale = Vector3.one;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_hand != null) _handBaseScale = _hand.localScale;
        }

        /// <summary>Trỏ vào vị trí thế giới (vd transform của block/shooter).</summary>
        public void PointAtWorld(Vector3 worldPosition)
        {
            var cam = _camera != null ? _camera : Camera.main;
            if (cam == null) return;
            PointAtScreen(cam.WorldToScreenPoint(worldPosition));
        }

        /// <summary>Trỏ vào toạ độ screen (vd vị trí một UI element).</summary>
        public void PointAtScreen(Vector2 screenPosition)
        {
            if (_canvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform, screenPosition, _canvas.worldCamera, out var local);

            if (_hole != null) _hole.anchoredPosition = local;
            if (_hand != null) _hand.anchoredPosition = local + _handOffset;
        }

        private void Update()
        {
            if (!_pulse || _hand == null) return;

            // dùng unscaledTime để vẫn nhịp khi game pause (Time.timeScale = 0)
            float t = (Mathf.Sin(Time.unscaledTime * _pulseSpeed) + 1f) * 0.5f; // 0..1
            _hand.localScale = _handBaseScale * Mathf.Lerp(_pulseMinScale, 1f, t);
        }
    }
}
