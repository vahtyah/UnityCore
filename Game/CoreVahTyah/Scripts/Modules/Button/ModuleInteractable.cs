using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    /// <summary>
    /// Factory mỏng: register <see cref="InteractableStyleService"/> vào <see cref="Services"/> để mọi
    /// <c>InteractableFeedback</c> (button/toggle/...) dùng chung cấu hình tween/feedback theo
    /// <see cref="InteractableStyleId"/>. Không nghe event, không giữ state. Chỉnh 1 chỗ → mọi UI ăn theo.
    /// </summary>
    [CreateAssetMenu(menuName = "VahTyah/Modules/Interactable", fileName = "Module_Interactable")]
    public sealed class ModuleInteractable : Module
    {
        [BoxGroup("Styles")]
        [Tooltip("Mỗi phần tử = 1 style. Nên có 1 style Id=Default làm fallback.")]
        [SerializeField]
        private List<InteractableStyleProfile> _styles = new List<InteractableStyleProfile> { new InteractableStyleProfile() };

        public override UniTask InitializeAsync(Transform holder)
        {
            Services.Register(new InteractableStyleService(_styles));
            return UniTask.CompletedTask;
        }
    }
}
