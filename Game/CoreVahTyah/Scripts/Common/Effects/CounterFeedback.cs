using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;
using VahTyah.Inspector;

namespace VahTyah
{
    /// <summary>
    /// Feedback dùng chung cho HUD counter (coin/heart/booster...): scale-bump khi giá trị đổi (LitMotion)
    /// + tạm nâng sortingOrder (nested Canvas overrideSorting) để nổi trên các canvas khác trong lúc animate.
    /// Display gọi <see cref="PlayChange"/> khi số đổi và <see cref="RaiseForCollect"/> khi collect bắt đầu;
    /// component KHÔNG tự nghe event — vẫn thuần feedback.
    /// </summary>
    // Nested Canvas (để overrideSorting) khiến Graphic dưới nó KHÔNG được GraphicRaycaster của canvas cha
    // raycast nữa → mất click. Nên require luôn GraphicRaycaster để nested canvas tự nhận input.
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
    public class CounterFeedback : MonoBehaviour
    {
        [BoxGroup("Change Animation")]
        [Tooltip("Bump scale khi giá trị TĂNG (mua, collect, cộng thưởng...).")]
        [SerializeField] private bool _animateOnIncrease = true;
        [BoxGroup("Change Animation")]
        [Tooltip("Bump scale khi giá trị GIẢM (tiêu...). Tắt nếu để component feedback khác lo (vd InteractableFeedback).")]
        [SerializeField] private bool _animateOnDecrease = true;
        [BoxGroup("Change Animation")]
        [Tooltip("Transform bị scale. Bỏ trống → scale chính GameObject này. Đừng trỏ vào node bị LayoutGroup/ContentSizeFitter đo.")]
        [SerializeField] private Transform _scaleTarget;
        [BoxGroup("Change Animation")]
        [SerializeField] private AnimationCurve _increaseAnim = DefaultIncrease();
        [BoxGroup("Change Animation")]
        [SerializeField] private AnimationCurve _decreaseAnim = DefaultDecrease();

        [BoxGroup("Sorting"), AutoRef]
        [SerializeField] private Canvas _canvas;
        [BoxGroup("Sorting")]
        [Tooltip("Khi animate, tạm bật overrideSorting để đẩy lên trên các canvas khác (không đụng canvas cha). Xong trả về gốc.")]
        [SerializeField] private bool _raiseSortingOnAnimate = true;
        [BoxGroup("Sorting")]
        [Tooltip("sortingOrder khi được nâng. Cao hơn sortingOrder của popup/overlay cao nhất cần vượt.")]
        [SerializeField] private int _raisedSortingOrder = 9999;

        private Vector3 _baseScale = Vector3.one;
        private AnimationCurve _activeCurve;
        private MotionHandle _scaleMotion;
        private CancellationTokenSource _scaleCts;

        private bool _baseOverrideSorting;
        private int _baseSortingOrder;

        private void Awake()
        {
            if (_scaleTarget == null) _scaleTarget = transform;
            _baseScale = _scaleTarget.localScale;

            if (_canvas == null) _canvas = GetComponent<Canvas>();
            if (_canvas != null)
            {
                _baseOverrideSorting = _canvas.overrideSorting;
                _baseSortingOrder = _canvas.sortingOrder;
            }
        }

        private void OnDisable()
        {
            CancelScale();
            _scaleTarget.localScale = _baseScale;
            RestoreSorting();   // disable giữa animation → không kẹt ở order nâng
        }

        /// <summary>Bump theo chiều tăng/giảm (tôn trọng flag). Sorting tự nâng lúc bắt đầu bump, trả khi xong.</summary>
        public void PlayChange(int prev, int current)
        {
            if (current == prev) return;
            bool increased = current > prev;
            if (increased ? _animateOnIncrease : _animateOnDecrease)
                PlayScale(increased ? _increaseAnim : _decreaseAnim);
        }

        /// <summary>Nâng sorting NGAY (collect bắt đầu, trước khi coin bay); giữ tới hết bump kế tiếp.
        /// Chỉ nâng nếu sẽ có bump tăng để đảm bảo được trả về.</summary>
        public void RaiseForCollect()
        {
            if (_raiseSortingOnAnimate && _animateOnIncrease)
                RaiseSorting();
        }

        /// <summary>Đóng raise của RaiseForCollect khi KHÔNG có bump để trả về (vd Heart full/infinity: số
        /// không đổi trên màn). Chỉ hạ khi không có bump đang chạy — có bump thì để nó tự restore lúc xong.</summary>
        public void Settle()
        {
            if (!_scaleMotion.IsActive())
                RestoreSorting();
        }

        // Bump scale khi số đổi. LitMotion drive t: 0 → duration; mỗi frame set localScale = _baseScale * curve.Evaluate(t);
        // chạy hết thì về _baseScale. Đổi lần nữa → cancel motion đang chạy rồi phát lại từ đầu.
        private void PlayScale(AnimationCurve curve)
        {
            CancelScale();
            if (curve == null || curve.length == 0) return;

            float duration = curve.keys[curve.length - 1].time;
            if (duration <= 0f) { _scaleTarget.localScale = _baseScale; return; }

            RaiseSorting();
            _activeCurve = curve;
            _scaleCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            ScaleAsync(duration, _scaleCts.Token).Forget();
        }

        private async UniTaskVoid ScaleAsync(float duration, CancellationToken ct)
        {
            _scaleMotion = LMotion.Create(0f, duration, duration)
                .Bind(this, static (t, self) => self._scaleTarget.localScale = self._baseScale * self._activeCurve.Evaluate(t));
            try
            {
                await _scaleMotion.ToUniTask(ct);
                _scaleTarget.localScale = _baseScale;   // chỉ reset khi chạy hết; bị cancel thì để motion mới tiếp quản
                RestoreSorting();                       // bị cancel thì KHÔNG trả — PlayScale mới đã nâng lại (hoặc OnDisable lo)
            }
            catch (OperationCanceledException) { }
        }

        private void CancelScale()
        {
            if (_scaleMotion.IsActive()) _scaleMotion.Cancel();
            _scaleCts?.Cancel();
            _scaleCts?.Dispose();
            _scaleCts = null;
        }

        private void RaiseSorting()
        {
            if (!_raiseSortingOnAnimate || _canvas == null) return;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = _raisedSortingOrder;
        }

        private void RestoreSorting()
        {
            if (_canvas == null) return;
            _canvas.overrideSorting = _baseOverrideSorting;
            _canvas.sortingOrder = _baseSortingOrder;
        }

        private static AnimationCurve DefaultIncrease() => new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.1f, 1.1f), new Keyframe(0.3f, 1f));

        private static AnimationCurve DefaultDecrease() => new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(0.4f, 1f));
    }
}
