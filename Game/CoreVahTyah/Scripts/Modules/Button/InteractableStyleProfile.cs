using System;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Một "feel" nhấn dùng chung cho UI bấm được: tween nhấn–thả–nảy + feedback click (haptic/sound).
    /// Cấu hình trên <see cref="ModuleInteractable"/> (List keyed theo <see cref="Id"/>);
    /// <c>InteractableFeedback</c> chọn style bằng <see cref="InteractableStyleId"/> thay vì set từng field.
    /// </summary>
    [Serializable]
    public class InteractableStyleProfile
    {
        public InteractableStyleId Id = InteractableStyleId.Default;

        [Header("Press / Release")]
        [Min(0f)] public float PressDuration = 0.1f;
        [Min(0f)] public float ReleaseDuration = 0.15f;
        [Tooltip("Scale khi đang nhấn (giữ). <1 = thu nhỏ.")]
        public float PressedScale = 0.9f;
        public AnimationCurve PressEase = InteractableCurves.OutQuad();
        public AnimationCurve ReleaseEase = InteractableCurves.OutBack();

        [Header("Click Bounce")]
        [Tooltip("Scale đỉnh khi nảy sau click. >1 = phồng lên.")]
        public float BounceScale = 1.1f;
        [Min(0f)] public float BounceUpDuration = 0.1f;
        [Min(0f)] public float BounceDownDuration = 0.15f;
        public AnimationCurve BounceUpEase = InteractableCurves.OutQuad();
        public AnimationCurve BounceDownEase = InteractableCurves.OutBack();

        [Header("Feedback (tuỳ chọn)")]
        [Tooltip("Haptic phát khi click. None = tắt.")]
        public HapticType ClickHaptic = HapticType.Light;
        [Tooltip("Âm thanh phát khi click. None = tắt.")]
        public SoundId ClickSound = SoundId.None;
    }
}
