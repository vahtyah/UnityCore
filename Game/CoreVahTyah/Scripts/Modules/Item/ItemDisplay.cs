using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VahTyah
{
    public class ItemDisplay : MonoBehaviour
    {
        [SerializeField] private string _itemKey;
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private Image _iconImage;

        [Header("Change Animation")]
        [SerializeField] private bool _animateOnChange;
        [Tooltip("Transform bị bump scale. Bỏ trống → dùng chính transform này. ĐỪNG để trỏ vào node có " +
                 "LayoutGroup/ContentSizeFitter hoặc bị LayoutGroup cha đo (ChildScale) — scale sẽ làm layout rung. " +
                 "Trỏ vào một child visual thuần (vd wrapper chứa icon+text, hoặc chính icon).")]
        [SerializeField] private Transform _scaleTarget;
        [SerializeField] private AnimationCurve _increaseAnim = DefaultIncrease();
        [SerializeField] private AnimationCurve _decreaseAnim = DefaultDecrease();

        private static readonly List<ItemDisplay> _all = new List<ItemDisplay>();

        private int _lastValue;
        private Vector3 _baseScale = Vector3.one;
        private AnimationCurve _activeCurve;
        private MotionHandle _scaleMotion;
        private CancellationTokenSource _scaleCts;

        private void Awake()
        {
            // Bump vào _scaleTarget (visual riêng) thay vì node layout → không làm HorizontalLayoutGroup/
            // ContentSizeFitter reflow khi scale. Bỏ trống thì fallback về transform (giữ hành vi cũ).
            if (_scaleTarget == null) _scaleTarget = transform;

            // Cache scale nghỉ đúng 1 lần. Nếu capture lại trong lúc đang phình (bump trước bị
            // cancel giữa chừng) thì baseline dồn lên → to mãi không về.
            _baseScale = _scaleTarget.localScale;
        }

        private void OnEnable()
        {
            _all.Add(this);
            this.On<ItemChanged>(e => { if (e.Key == _itemKey) OnChanged(); });
            InitValueAsync().Forget();
        }

        // Giá trị đầu đọc qua query ItemGet — nhưng nếu HUD OnEnable chạy TRƯỚC khi ModuleItem.Subscribe
        // (boot chưa xong) thì query chưa có listener → trả 0 → hiển thị 0 tới ItemChanged đầu tiên.
        // Đợi tới khi ItemGet có listener rồi mới đọc để hiện đúng số đã load từ save.
        private async UniTaskVoid InitValueAsync()
        {
            if (!EventBus.HasListeners<ItemGet>())
                await UniTask.WaitUntil(
                    static () => EventBus.HasListeners<ItemGet>(),
                    cancellationToken: this.GetCancellationTokenOnDestroy());

            _lastValue = GetValue();
            Refresh();
        }

        private void OnDisable()
        {
            _all.Remove(this);
            CancelScale();
            _scaleTarget.localScale = _baseScale;
        }

        private void OnChanged()
        {
            int val = GetValue();
            int prev = _lastValue;
            _lastValue = val;
            Refresh();

            if (_animateOnChange && val != prev)
                PlayScale(val > prev ? _increaseAnim : _decreaseAnim);
        }

        private void Refresh()
        {
            if (_valueText != null)
                _valueText.SetText("{0}", GetValue());
        }

        private int GetValue()
        {
            int result = 0;
            EventBus.Publish(new ItemGet { Key = _itemKey, Reply = v => result = v }).Forget();
            return result;
        }

        // Bump scale khi số đổi. LitMotion drive t: 0 → duration (linear, scaled time y hệt Time.deltaTime
        // cũ); mỗi frame set localScale = _baseScale * curve.Evaluate(t); chạy hết thì về _baseScale.
        // Đổi số lần nữa → cancel motion đang chạy rồi phát lại từ đầu (thay cho StopCoroutine).
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

        public static bool TryFind(string key, out Vector3 position)
        {
            foreach (var d in _all)
            {
                if (d._itemKey == key && d._iconImage != null)
                {
                    position = d._iconImage.transform.position;
                    return true;
                }
            }
            position = Vector3.zero;
            return false;
        }

        public static void RefreshAll()
        {
            foreach (var d in _all) d.Refresh();
        }

        private static AnimationCurve DefaultIncrease() => new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.1f, 1.1f), new Keyframe(0.3f, 1f));

        private static AnimationCurve DefaultDecrease() => new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(0.4f, 1f));
    }
}
