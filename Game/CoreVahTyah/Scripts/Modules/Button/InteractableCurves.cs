using UnityEngine;

namespace VahTyah
{
    /// <summary>Tạo các AnimationCurve easing mặc định cho feedback nhấn.</summary>
    internal static class InteractableCurves
    {
        // Ease-out: nhanh lúc đầu, chậm dần về cuối.
        internal static AnimationCurve OutQuad()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 2f),
                new Keyframe(1f, 1f, 0f, 0f));
        }

        // Ease-out có overshoot (vọt lên 1.1 ở giữa rồi về 1) → cảm giác "nảy".
        internal static AnimationCurve OutBack()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 4.70158f),
                new Keyframe(0.58f, 1.1f, 0f, 0f),
                new Keyframe(1f, 1f, 0f, 0f));
        }
    }
}
