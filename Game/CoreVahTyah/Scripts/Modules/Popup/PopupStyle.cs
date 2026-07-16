using System;
using LitMotion;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Style cho animator kiểu scale + fade (<c>PopupAnimator</c>): pop-in theo curve + fade, đóng scale + fade.
    /// Cấu hình trong <see cref="ModulePanel"/>._popupStyles (keyed theo <see cref="Id"/>).
    /// </summary>
    [Serializable]
    public class PopupStyle
    {
        public PopupStyleId Id = PopupStyleId.Default;

        [Header("Open")]
        [Tooltip("Scale theo thời gian lúc mở (x = giây, y = scale). Độ dài curve (key cuối) = thời lượng mở.")]
        public AnimationCurve ScaleCurve = new AnimationCurve(
            new Keyframe(0f, 0.8f),
            new Keyframe(0.125f, 1.1f),
            new Keyframe(0.2f, 1f),
            new Keyframe(0.35f, 1f));
        [Min(0f)] public float FadeInDuration = 0.12f;

        [Header("Close")]
        public bool HasCloseAnimation = true;
        [Min(0f)] public float CloseDuration = 0.15f;
        [Tooltip("Scale đích khi đóng. <1 = co lại.")]
        public float CloseScale = 0.8f;
        public Ease CloseEase = Ease.InBack;
    }
}
