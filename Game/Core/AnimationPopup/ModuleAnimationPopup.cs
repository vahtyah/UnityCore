using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Module cung cấp curve popup mặc định cho các <see cref="AnimationPopup"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/AnimationPopup", fileName = "Module_AnimationPopup", order = 1)]
    internal sealed class ModuleAnimationPopup : SAModule
    {
        [SerializeField]
        private AnimationCurve defaultCurve = BuildDefaultCurve();

        public static ModuleAnimationPopup Instance { get; private set; }

        public AnimationCurve DefaultCurve => defaultCurve;

        public override async Task InitializeAsync()
        {
            Instance = this;
            await Task.CompletedTask;
        }

        public override void Subscribe()
        {
        }

        // Curve popup mặc định: phồng lên 1.1 rồi về 1.
        private static AnimationCurve BuildDefaultCurve()
        {
            return new AnimationCurve(new Keyframe[3]
            {
                new Keyframe(0f, 0f, 0f, 7f),
                new Keyframe(0.23f, 1.1f, 0f, 0f),
                new Keyframe(0.35f, 1f, 0f, 0f)
            });
        }
    }
}
