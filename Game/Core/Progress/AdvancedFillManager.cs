using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StandardAssets
{
    /// <summary>
    /// Hiển thị thanh tiến trình (fill) có hoạt ảnh mượt. Hỗ trợ 2 chế độ:
    /// điều chỉnh RectTransform (offsetMin/Max) hoặc Image.fillAmount, kèm text %/x-of-y.
    /// Tự đăng ký với <see cref="Progress"/> theo khoá enum để nhận cập nhật.
    /// </summary>
    [AddComponentMenu("SA/Progress/Advanced Fill Manager")]
    public class AdvancedFillManager : MonoBehaviour
    {
        [Header("Key")]
        [SAEnumFilter("Progress")]
        [SerializeField]
        private SAEnumRef _key;

        [Header("Fill")]
        [SerializeField]
        private bool _useRectMode = true;

        [ShowIf(new object[] { "_useRectMode" })]
        [SerializeField]
        private RectTransform _fillRect;

        [ShowIf(new object[] { "_useRectMode" })]
        [SerializeField]
        private RectTransform _fillParent;

        [ShowIf(new object[] { "_useRectMode" })]
        [SerializeField]
        private bool _fillReverse;

        [ShowIf(new object[] { "_useRectMode" })]
        [SerializeField]
        private bool _fillVertical;

        [ShowIf(new object[] { "_useRectMode" })]
        [SerializeField]
        private float _offset;

        [HideIf(new object[] { "_useRectMode" })]
        [SerializeField]
        private Image _fillImage;

        [Header("Text")]
        [SerializeField]
        private TextMeshProUGUI _progressText;

        [SerializeField]
        private ProgressTextFormat _textFormat = ProgressTextFormat.Percent;

        [Header("Animation")]
        [SerializeField]
        private float _animationTime = 0.3f;

        [SerializeField]
        private AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public UnityEvent<float> OnProgressChanged;

        public UnityEvent OnProgressComplete;

        private float _visualRatio;

        private float _animFrom;

        private float _animTo;

        private float _elapsed;

        private float _duration;

        private bool _isAnimating;

        private bool _completeFired;

        private int _countTo;

        private int _countTotal;

        public float Ratio => _visualRatio;

        public int Total => _countTotal;

        private string ResolveKey()
        {
            return _key.GetEnum()?.ToString();
        }

        private void OnEnable()
        {
            Progress.Register(ResolveKey(), this);
            Progress.Fill(ResolveKey(), 0, _countTotal, direct: true);
        }

        private void OnDisable()
        {
            Progress.Unregister(ResolveKey(), this);
        }

        internal void SetCounts(int toCount, int total)
        {
            _countTo = Mathf.Clamp(toCount, 0, total);
            _countTotal = total;
        }

        public void SetProgress(float fromRatio, float toRatio)
        {
            UpdateProgress(fromRatio, direct: true);
            UpdateProgress(toRatio, direct: false);
        }

        public void UpdateProgress(float toRatio)
        {
            UpdateProgress(toRatio, direct: false);
        }

        public void UpdateProgress(float toRatio, bool direct = false)
        {
            toRatio = Mathf.Clamp01(toRatio);
            // Đặt thẳng giá trị (không hoạt ảnh) khi direct, không play, hoặc tắt animation.
            if (direct || !Application.isPlaying || _animationTime <= 0f)
            {
                _visualRatio = toRatio;
                _animFrom = toRatio;
                _animTo = toRatio;
                _elapsed = 0f;
                _isAnimating = false;
                _completeFired = Mathf.Approximately(toRatio, 1f);
                ApplyFill(toRatio);
                UpdateText(toRatio, _countTo, _countTotal);
                OnProgressChanged?.Invoke(toRatio);
                if (_completeFired)
                {
                    OnProgressComplete?.Invoke();
                }
                return;
            }

            _duration = _animationTime;
            if (_isAnimating)
            {
                // Nếu đang animate cùng hướng -> nối tiếp mượt; ngược hướng -> bắt đầu lại.
                if ((Mathf.Approximately(_animTo, _animFrom) || (toRatio - _visualRatio) * (_animTo - _animFrom) >= 0f) && !Mathf.Approximately(toRatio, _visualRatio))
                {
                    float curveAt = _animationCurve.Evaluate(0.15f);
                    float inv = 1f - curveAt;
                    _animFrom = Mathf.Approximately(inv, 0f) ? _visualRatio : ((_visualRatio - toRatio * curveAt) / inv);
                    _elapsed = 0.15f * _duration;
                }
                else
                {
                    _animFrom = _visualRatio;
                    _elapsed = 0f;
                }
            }
            else
            {
                _animFrom = _visualRatio;
                _elapsed = 0f;
            }
            _animTo = toRatio;
            _isAnimating = true;
            _completeFired = false;
        }

        private void Update()
        {
            if (!_isAnimating)
            {
                return;
            }
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            float eased = _animationCurve.Evaluate(t);
            _visualRatio = Mathf.Lerp(_animFrom, _animTo, eased);
            int count = (_countTotal > 0) ? Mathf.RoundToInt(_visualRatio * _countTotal) : 0;
            ApplyFill(_visualRatio);
            UpdateText(_visualRatio, count, _countTotal);
            if (t < 1f)
            {
                return;
            }
            _visualRatio = _animTo;
            _isAnimating = false;
            int finalCount = (_countTotal > 0) ? _countTo : 0;
            ApplyFill(_visualRatio);
            UpdateText(_visualRatio, finalCount, _countTotal);
            OnProgressChanged?.Invoke(_animTo);
            if (!_completeFired && Mathf.Approximately(_animTo, 1f))
            {
                _completeFired = true;
                OnProgressComplete?.Invoke();
            }
        }

        private void ApplyFill(float ratio)
        {
            if (_useRectMode)
            {
                if (_fillRect == null || _fillParent == null)
                {
                    return;
                }
                Rect rect = _fillParent.rect;
                float availW = rect.width - _offset;
                rect = _fillParent.rect;
                float availH = rect.height - _offset;
                Vector2 offsetMin = _fillRect.offsetMin;
                Vector2 offsetMax = _fillRect.offsetMax;
                if (_fillVertical)
                {
                    float h = availH * ratio;
                    if (_fillReverse)
                    {
                        offsetMin.y = availH - h;
                    }
                    else
                    {
                        offsetMax.y = h - availH;
                    }
                }
                else
                {
                    float w = availW * ratio;
                    if (_fillReverse)
                    {
                        offsetMin.x = availW - w;
                    }
                    else
                    {
                        offsetMax.x = w - availW;
                    }
                }
                _fillRect.offsetMin = offsetMin;
                _fillRect.offsetMax = offsetMax;
            }
            else if (_fillImage != null)
            {
                _fillImage.fillAmount = ratio;
            }
        }

        private void UpdateText(float ratio, int count, int total)
        {
            if (_progressText == null)
            {
                return;
            }
            switch (_textFormat)
            {
                case ProgressTextFormat.Percent:
                    _progressText.SetText("{0}%", (float)Mathf.RoundToInt(ratio * 100f));
                    break;
                case ProgressTextFormat.CurrentOfTarget:
                    _progressText.SetText("{0}/{1}", (float)count, (float)total);
                    break;
                case ProgressTextFormat.CurrentOnly:
                    _progressText.SetText("{0}", (float)count);
                    break;
            }
        }
    }

    /// <summary>Định dạng text của thanh tiến trình.</summary>
    public enum ProgressTextFormat
    {
        Percent,
        CurrentOfTarget,
        CurrentOnly
    }
}
