using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace VahTyah
{
    /// <summary>
    /// Adapter nối <see cref="FeatureView.OnSetProgress"/> (UnityEvent&lt;float,float&gt;) vào một Image kiểu Filled.
    /// Nhận (from, to), đặt fillAmount = from rồi animate tới to; xong thì gọi lại
    /// <see cref="FeatureView.OnFillAnimationEnd"/> để View bắn tiếp OnFillEnd / OnFillComplete.
    /// Cần thiết vì không thể bind động một UnityEvent 2 tham số vào setter 1 tham số.
    /// </summary>
    public sealed class FeatureFillBar : MonoBehaviour
    {
        [Tooltip("View sở hữu thanh này. Khi animate xong sẽ gọi FeatureView.OnFillAnimationEnd().")]
        [SerializeField] private FeatureView _featureView;

        [Tooltip("Image kiểu Filled dùng làm thanh tiến trình (đặt Image Type = Filled).")]
        [SerializeField] private Image _fill;

        [Tooltip("Thời lượng chạy fill (giây). 0 = set thẳng, không animate.")]
        [Min(0f)]
        [SerializeField] private float _duration = 0.6f;

        [SerializeField] private Ease _ease = Ease.OutCubic;

        private MotionHandle _motion;

        /// <summary>Wire dynamic từ FeatureView.OnSetProgress (float, float).</summary>
        public void SetProgress(float from, float to)
        {
            from = Mathf.Clamp01(from);
            to = Mathf.Clamp01(to);
            CancelMotion();

            if (_fill != null) _fill.fillAmount = from;

            // Không delta hoặc không animate được -> set thẳng rồi báo xong ngay.
            if (!Application.isPlaying || _duration <= 0f || Mathf.Approximately(from, to))
            {
                if (_fill != null) _fill.fillAmount = to;
                NotifyEnd();
                return;
            }

            _motion = LMotion.Create(from, to, _duration)
                .WithEase(_ease)
                .WithOnComplete(NotifyEnd)
                .Bind(this, static (v, self) =>
                {
                    if (self._fill != null) self._fill.fillAmount = v;
                });
        }

        private void NotifyEnd()
        {
            if (_featureView != null) _featureView.OnFillAnimationEnd();
        }

        private void CancelMotion()
        {
            if (_motion.IsActive()) _motion.Cancel();
        }

        private void OnDisable() => CancelMotion();
        private void OnDestroy() => CancelMotion();
    }
}
