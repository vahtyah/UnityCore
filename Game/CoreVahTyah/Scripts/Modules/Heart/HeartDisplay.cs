using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VahTyah
{
    /// <summary>
    /// Hiển thị số tim + timer (hồi tiếp / vô hạn) trên HUD. Nghe HeartChanged/HeartInfinityChanged để
    /// cập nhật tức thời; tự refresh timer mỗi giây vì đếm ngược đổi liên tục (không có event mỗi giây).
    /// Có bump scale khi số tim đổi (như ItemDisplay). Gắn lên cụm Heart, wire _countText + _timerText.
    /// </summary>
    public class HeartDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _countText;   // số tim, vd "3"
        [SerializeField] private TextMeshProUGUI _timerText;   // "2m 30s" / "Full" / "∞ 12m"
        [Tooltip("Icon trái tim — đích cho tim bay tới (HeartCollect). Thiếu → HeartCollect cộng thẳng.")]
        [SerializeField] private Image _iconImage;

        [Tooltip("Chu kỳ refresh timer (giây).")]
        [SerializeField] private float _refreshInterval = 1f;

        [Header("Change Animation")]
        [SerializeField] private bool _animateOnChange;
        [Tooltip("Transform bị bump scale. Bỏ trống → dùng chính transform này. ĐỪNG để trỏ vào node có " +
                 "LayoutGroup/ContentSizeFitter hoặc bị LayoutGroup cha đo (ChildScale) — scale sẽ làm layout rung. " +
                 "Trỏ vào một child visual thuần (vd icon).")]
        [SerializeField] private Transform _scaleTarget;
        [SerializeField] private AnimationCurve _increaseAnim = DefaultIncrease();
        [SerializeField] private AnimationCurve _decreaseAnim = DefaultDecrease();

        private static readonly List<HeartDisplay> _all = new List<HeartDisplay>();

        private float _timer;
        private int _lastCount;
        private Vector3 _baseScale = Vector3.one;
        private AnimationCurve _activeCurve;
        private MotionHandle _scaleMotion;
        private CancellationTokenSource _scaleCts;

        private void Awake()
        {
            // Bump vào _scaleTarget (visual riêng) thay vì node layout → không làm LayoutGroup/ContentSizeFitter
            // reflow khi scale. Bỏ trống thì fallback về transform.
            if (_scaleTarget == null) _scaleTarget = transform;
            _baseScale = _scaleTarget.localScale;
        }

        private void OnEnable()
        {
            _all.Add(this);
            this.On<HeartChanged>(_ => OnCountChanged());
            this.On<HeartInfinityChanged>(_ => ApplyDisplay(QueryInfinity()));
            InitAsync().Forget();
        }

        private void OnDisable()
        {
            _all.Remove(this);
            CancelScale();
            _scaleTarget.localScale = _baseScale;
        }

        /// <summary>Vị trí world của icon tim (đích bay cho HeartCollect). False nếu không có display/icon.</summary>
        public static bool TryFind(out Vector3 position)
        {
            foreach (var d in _all)
            {
                if (d._iconImage != null)
                {
                    position = d._iconImage.transform.position;
                    return true;
                }
            }
            position = Vector3.zero;
            return false;
        }

        // Đợi ModuleHeart Subscribe xong rồi mới đọc lần đầu — tránh HUD OnEnable chạy TRƯỚC khi boot xong
        // → query chưa có listener → hiển thị 0 (giống bug ItemDisplay đã fix). Không bump ở lần đầu.
        private async UniTaskVoid InitAsync()
        {
            if (!EventBus.HasListeners<HeartGet>())
                await UniTask.WaitUntil(
                    static () => EventBus.HasListeners<HeartGet>(),
                    cancellationToken: this.GetCancellationTokenOnDestroy());

            _lastCount = GetCount();
            ApplyDisplay(QueryInfinity());
        }

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;   // unscaled: timer chạy kể cả khi game pause (Time.timeScale=0)
            if (_timer >= _refreshInterval)
            {
                _timer = 0f;
                ApplyDisplay(QueryInfinity());   // tick mỗi giây: đếm ngược + bắt lúc infinity hết (count "∞" → số)
            }
        }

        // HeartChanged: số tim đổi → render lại + bump nếu bật. Đang infinity thì count hiện "∞" (số không đổi
        // trên màn) nên bỏ qua bump.
        private void OnCountChanged()
        {
            int val = GetCount();
            int prev = _lastCount;
            _lastCount = val;

            bool infinity = QueryInfinity();
            ApplyDisplay(infinity);

            if (_animateOnChange && !infinity && val != prev)
                PlayScale(val > prev ? _increaseAnim : _decreaseAnim);
        }

        // Render count + timer theo trạng thái infinity. KHÔNG bump ở đây (bump chỉ ở OnCountChanged).
        private void ApplyDisplay(bool infinity)
        {
            if (_countText != null)
                _countText.SetText(infinity ? "∞" : GetCount().ToString());

            if (_timerText != null)
            {
                string text = string.Empty;
                if (infinity)
                    EventBus.Publish(new HeartGetInfinityTimer { Reply = v => text = v }).Forget();   // giờ THÔ, không ∞
                else
                    EventBus.Publish(new HeartGetTimer { Reply = v => text = v }).Forget();            // "Full" / đếm ngược
                _timerText.SetText(text);
            }
        }

        private bool QueryInfinity()
        {
            bool v = false;
            EventBus.Publish(new HeartIsInfinity { Reply = x => v = x }).Forget();
            return v;
        }

        private int GetCount()
        {
            int hearts = 0;
            EventBus.Publish(new HeartGet { Reply = v => hearts = v }).Forget();
            return hearts;
        }

        // Bump scale khi số tim đổi. LitMotion drive t:0→duration (scaled time); mỗi frame set
        // _scaleTarget.localScale = _baseScale * curve.Evaluate(t); xong về _baseScale. Đổi lần nữa → cancel phát lại.
        private void PlayScale(AnimationCurve curve)
        {
            CancelScale();
            if (curve == null || curve.length == 0) return;

            float duration = curve.keys[curve.length - 1].time;
            if (duration <= 0f) { _scaleTarget.localScale = _baseScale; return; }

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

        private static AnimationCurve DefaultIncrease() => new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.1f, 1.1f), new Keyframe(0.3f, 1f));

        private static AnimationCurve DefaultDecrease() => new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(0.4f, 1f));
    }
}
