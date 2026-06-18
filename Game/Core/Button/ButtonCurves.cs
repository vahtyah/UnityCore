using UnityEngine;

namespace StandardAssets
{
    /// <summary>Tạo các AnimationCurve easing dùng chung cho hiệu ứng nút bấm.</summary>
    internal static class ButtonCurves
    {
        internal static AnimationCurve OutQuad()
        {
            return new AnimationCurve(new Keyframe[2]
            {
                new Keyframe(0f, 0f, 0f, 2f),
                new Keyframe(1f, 1f, 0f, 0f)
            });
        }

        internal static AnimationCurve OutBack()
        {
            return new AnimationCurve(new Keyframe[3]
            {
                new Keyframe(0f, 0f, 0f, 4.70158f),
                new Keyframe(0.58f, 1.1f, 0f, 0f),
                new Keyframe(1f, 1f, 0f, 0f)
            });
        }
    }
}
