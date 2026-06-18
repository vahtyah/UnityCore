using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Module cung cấp cấu hình tween mặc định cho các <see cref="ButtonClick"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/Button", fileName = "Module_Button", order = 4)]
    internal sealed class ModuleButton : SAModule
    {
        [Header("Tween Settings")]
        public float PressDuration = 0.1f;
        public float ReleaseDuration = 0.15f;
        public float PressedScale = 0.9f;
        public AnimationCurve PressEase = ButtonCurves.OutQuad();
        public AnimationCurve ReleaseEase = ButtonCurves.OutBack();

        [Header("Click Bounce")]
        public float BounceScale = 1.1f;
        public float BounceUpDuration = 0.1f;
        public float BounceDownDuration = 0.15f;
        public AnimationCurve BounceUpEase = ButtonCurves.OutQuad();
        public AnimationCurve BounceDownEase = ButtonCurves.OutBack();

        public static ModuleButton Instance { get; private set; }

        public override Task InitializeAsync()
        {
            Instance = this;
            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
        }
    }
}
