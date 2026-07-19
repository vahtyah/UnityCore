using System;
using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using VahTyah.Inspector;

namespace VahTyah
{
    [DisallowMultipleComponent]
    public class InteractableFeedback : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [BoxGroup("Feedback")]
        [SerializeField] private Transform _target;

        [BoxGroup("Feedback")]
        [SerializeField] private InteractableStyleId _style = InteractableStyleId.Default;

        private static InteractableStyleProfile _codeDefault;
        private static InteractableStyleProfile CodeDefault => _codeDefault ??= new InteractableStyleProfile();

        private Vector3 _originalScale = Vector3.one;
        private float _factor = 1f;
        private MotionHandle _motion;
        private InteractableStyleProfile _active;

        private void Awake()
        {
            if (_target == null) _target = transform;
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
