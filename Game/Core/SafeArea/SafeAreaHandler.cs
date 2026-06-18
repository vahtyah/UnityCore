using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Điều chỉnh anchor của RectTransform theo vùng an toàn (safe area) của màn hình.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private ScreenOrientation _lastOrientation;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            // Chỉ cập nhật khi safe area / kích thước / hướng màn hình thay đổi
            if (Screen.safeArea != _lastSafeArea || Screen.width != _lastScreenSize.x ||
                Screen.height != _lastScreenSize.y || Screen.orientation != _lastOrientation)
            {
                Apply();
            }
        }

        private void Apply()
        {
            Rect safeArea = Screen.safeArea;
            if (!(safeArea == _lastSafeArea) || Screen.width != _lastScreenSize.x || Screen.height != _lastScreenSize.y)
            {
                _lastSafeArea = safeArea;
                _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
                _lastOrientation = Screen.orientation;

                Vector2 anchorMin = safeArea.position;
                Vector2 anchorMax = safeArea.position + safeArea.size;
                anchorMin.x /= Screen.width;
                anchorMin.y /= Screen.height;
                anchorMax.x /= Screen.width;
                anchorMax.y /= Screen.height;

                _rectTransform.anchorMin = anchorMin;
                _rectTransform.anchorMax = anchorMax;
                _rectTransform.offsetMin = Vector2.zero;
                _rectTransform.offsetMax = Vector2.zero;
            }
        }
    }
}
