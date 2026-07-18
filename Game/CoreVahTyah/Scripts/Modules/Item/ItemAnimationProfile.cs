using System;
using UnityEngine;

namespace VahTyah
{
    /// <summary>Kiểu hành vi animation khi thu item.</summary>
    public enum ItemAnimationStyle
    {
        FlyToTarget, // bay Bezier từ điểm nguồn tới icon ItemDisplay (coin/gem)
        PopInPlace,  // bung tại chỗ ItemDisplay, không bay ngang màn (booster/gift)
        None,        // không animation, cộng thẳng vào Current
    }

    /// <summary>
    /// Một profile animation dùng chung: định nghĩa 1 lần trong <see cref="ModuleItem"/>, item chọn bằng
    /// <see cref="ItemAnimationId"/>. Chỉnh 1 chỗ → mọi item cùng Id ăn theo. Nên có 1 profile Id=Default.
    /// </summary>
    [Serializable]
    public class ItemAnimationProfile
    {
        public ItemAnimationId Id;
        public ItemAnimationStyle Style = ItemAnimationStyle.FlyToTarget;
        public float SpawnRadius = 120f;
        public float StaggerDelay = 0.04f;
        public float Duration = 1f;
        public float CurveStrength = 400f;
        public AnimationCurve MoveCurve = DefaultMove();
        public AnimationCurve ScaleCurve = DefaultScale();
        public int MaxPoolSize = 20;

        [Header("Collect feedback (phát mỗi mảnh chạm đích)")]
        public SoundId CollectSound = SoundId.None;
        public HapticType CollectHaptic = HapticType.None;
        public ParticleId CollectParticle = ParticleId.None;

        public static AnimationCurve DefaultMove() => new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.4f, 0f), new Keyframe(1f, 1f, 4.8f, 4.8f));

        public static AnimationCurve DefaultScale() => new AnimationCurve(
            new Keyframe(0f, 0.4f, 12f, 12f), new Keyframe(0.1f, 1.5f),
            new Keyframe(0.5f, 1f), new Keyframe(1f, 0.5f, -12f, -12f));
    }
}
