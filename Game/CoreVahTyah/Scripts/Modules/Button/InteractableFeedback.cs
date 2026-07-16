using System;
using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VahTyah
{
    /// <summary>
    /// Feedback khi nhấn cho UI bấm được (button / toggle / tile...): scale nhấn/thả/nảy (LitMotion)
    /// + haptic/sound khi click. THUẦN FEEDBACK — không tự gọi hành động; logic do Unity Button/Toggle
    /// trên cùng GameObject xử lý. Thông số lấy từ <see cref="InteractableStyleService"/> theo <see cref="_style"/>
    /// (chỉnh chung ở <see cref="ModuleInteractable"/>); thiếu module → code default. Scale áp lên
    /// <see cref="_visual"/> (mặc định = child đầu) để không đụng hitbox RectTransform.
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractableFeedback : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Tooltip("Transform bị scale. Bỏ trống → tự lấy child đầu tiên lúc Awake (giữ nguyên hitbox).")]
        [SerializeField] private Transform _visual;

        [Tooltip("Style dùng chung, tra từ ModuleInteractable theo id này.")]
        [SerializeField] private InteractableStyleId _style = InteractableStyleId.Default;

        // Default trong code khi chưa có ModuleInteractable (field initializer dựng sẵn curve/feedback).
        private static InteractableStyleProfile _codeDefault;
        private static InteractableStyleProfile CodeDefault => _codeDefault ??= new InteractableStyleProfile();

        private Transform _target;
        private Vector3 _originalScale = Vector3.one;
        private float _factor = 1f;              // hệ số scale hiện tại (× _originalScale)
        private MotionHandle _motion;
        private InteractableStyleProfile _active;   // style của interaction hiện tại

        private void Awake()
        {
            if (_visual == null && transform.childCount > 0)
                _visual = transform.GetChild(0);
            _target = _visual != null ? _visual : transform;
        }

        private void Start() => _originalScale = _target.localScale;

        private void OnEnable()
        {
            if (_originalScale != Vector3.zero) Apply(1f);   // reset nếu bị disable giữa animation
        }

        private void OnDisable() => Cancel();
        private void OnDestroy() => Cancel();

        private InteractableStyleProfile Resolve() => InteractableStyle.Get(_style) ?? CodeDefault;

        public void OnPointerDown(PointerEventData eventData)
        {
            _active = Resolve();
            Animate(_active.PressedScale, _active.PressDuration, _active.PressEase);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            var s = _active ?? Resolve();
            Animate(1f, s.ReleaseDuration, s.ReleaseEase);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _active = Resolve();
            Bounce(_active);
            PlayFeedback();
        }

        // --- Animation ---

        private void Animate(float toFactor, float duration, AnimationCurve ease)
        {
            Cancel();
            _motion = Tween(_factor, toFactor, duration, ease, null);
        }

        private void Bounce(InteractableStyleProfile s)
        {
            Cancel();
            // Phồng lên BounceScale rồi về 1 (tạo motion thứ 2 khi motion đầu hoàn tất tự nhiên).
            _motion = Tween(_factor, s.BounceScale, s.BounceUpDuration, s.BounceUpEase,
                () => _motion = Tween(s.BounceScale, 1f, s.BounceDownDuration, s.BounceDownEase, null));
        }

        private MotionHandle Tween(float from, float to, float duration, AnimationCurve ease, Action onComplete)
        {
            var b = LMotion.Create(from, to, Mathf.Max(0.0001f, duration));
            if (ease != null && ease.length > 0) b = b.WithEase(ease);
            if (onComplete != null) b = b.WithOnComplete(onComplete);
            return b.Bind(this, static (v, self) => self.Apply(v));
        }

        private void Apply(float factor)
        {
            _factor = factor;
            _target.localScale = _originalScale * factor;
        }

        private void Cancel()
        {
            if (_motion.IsActive()) _motion.Cancel();
        }

        // --- Feedback (haptic/sound) ---

        private void PlayFeedback()
        {
            var s = _active ?? Resolve();
            if (s.ClickHaptic != HapticType.None) Haptic.Play(s.ClickHaptic);
            if (s.ClickSound != SoundId.None) Sound.Play(s.ClickSound);
        }
    }
}
