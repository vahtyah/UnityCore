using System;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Style cho animator fade-only (<c>FadeAnimator</c>): fade-in/out CanvasGroup, không scale.
    /// Cấu hình trong <see cref="ModulePanel"/>._fadeStyles (keyed theo <see cref="Id"/>).
    /// </summary>
    [Serializable]
    public class FadeStyle
    {
        public FadeStyleId Id = FadeStyleId.Default;

        [Header("Fade In")]
        [Tooltip("false → hiện ngay (alpha = 1), không fade.")]
        public bool FadeIn = true;
        [Min(0f)] public float FadeInDuration = 0.2f;

        [Header("Fade Out")]
        [Tooltip("false → tắt tức thì, không fade.")]
        public bool FadeOut = true;
        [Min(0f)] public float FadeOutDuration = 0.15f;
    }
}
