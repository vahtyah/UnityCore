using UnityEngine;
using UnityEngine.UI;

namespace StandardAssets
{
    /// <summary>
    /// Làm mờ dần (fade-in) một Image khi được bật, dùng unscaled time.
    /// </summary>
    public class ImageFadeIn : MonoBehaviour
    {
        [Tooltip("Fade duration in seconds (unscaled time).")]
        [SerializeField]
        private float _duration = 0.5f;

        private Image _image;
        private Color _targetColor;
        private float _elapsed;
        private bool _fading;
        private Color _startColor;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _targetColor = _image.color;
            // Màu bắt đầu: giống màu đích nhưng alpha = 0
            _startColor = new Color(_targetColor.r, _targetColor.g, _targetColor.b, 0f);
        }

        private void OnEnable()
        {
            _elapsed = 0f;
            _fading = true;
            _image.color = _startColor;
        }

        private void OnDisable()
        {
            _fading = false;
            _elapsed = 0f;
            _image.color = _startColor;
        }

        private void LateUpdate()
        {
            if (_fading)
            {
                _elapsed += Time.unscaledDeltaTime;
                _image.color = Color.Lerp(_startColor, _targetColor, _elapsed / _duration);
                if (_elapsed >= _duration)
                {
                    _image.color = _targetColor;
                    _fading = false;
                }
            }
        }
    }
}
