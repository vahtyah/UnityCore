using UnityEngine;
using UnityEngine.UI;

namespace VahTyah
{
    /// <summary>
    /// Fade-in một Image khi được bật, dùng unscaled time.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ImageFadeIn : MonoBehaviour
    {
        [Tooltip("Thời gian fade (giây, unscaled).")]
        [SerializeField] private float _duration = 0.5f;

        private Image _image;
        private Color _targetColor;
        private Color _startColor;
        private float _elapsed;
        private bool _fading;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _targetColor = _image.color;
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
            _image.color = _startColor;
        }

        private void LateUpdate()
        {
            if (!_fading) return;

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
