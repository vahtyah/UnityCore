using System;
using UnityEngine;

namespace VahTyah
{
    /// <summary>Kiểu hành vi khi bay về counter.</summary>
    public enum CollectFlyStyle
    {
        FlyToTarget, // bay Bezier từ điểm nguồn tới đích (coin/gem/heart)
        PopInPlace,  // bung tại chỗ đích, không bay ngang màn (booster/gift)
        None,        // không animation, callback ngay với nguyên value
    }

    /// <summary>
    /// Profile animation dùng chung: khai báo 1 lần ở <see cref="ModuleCollectFly"/>, caller chọn bằng
    /// <see cref="CollectAnimId"/>. Chỉnh 1 chỗ → mọi counter cùng Id ăn theo. Nên có 1 profile Id=Default.
    /// </summary>
    [Serializable]
    public class CollectProfile
    {
        public CollectAnimId Id;
        public CollectFlyStyle Style = CollectFlyStyle.FlyToTarget;
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
