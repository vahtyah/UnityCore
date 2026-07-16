using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    /// <summary>
    /// Factory mỏng: register <see cref="PanelStyleService"/> vào <see cref="Services"/> để mọi
    /// <see cref="IPanelAnimator"/> dùng chung cấu hình animation. Hai loại style riêng:
    /// <see cref="PopupStyle"/> (PopupAnimator) và <see cref="FadeStyle"/> (FadeAnimator).
    /// Không nghe event, không giữ state. Chỉnh 1 chỗ (asset này) → mọi panel ăn theo.
    /// </summary>
    [CreateAssetMenu(menuName = "VahTyah/Modules/Panel", fileName = "Module_Panel")]
    public sealed class ModulePanel : Module
    {
        [BoxGroup("Popup Styles")]
        [Tooltip("Style cho PopupAnimator (scale + fade). Nên có 1 style Id=Default.")]
        [SerializeField]
        private List<PopupStyle> _popupStyles = new List<PopupStyle> { new PopupStyle() };

        [BoxGroup("Fade Styles")]
        [Tooltip("Style cho FadeAnimator (fade-only). Nên có 1 style Id=Default.")]
        [SerializeField]
        private List<FadeStyle> _fadeStyles = new List<FadeStyle> { new FadeStyle() };

        public override UniTask InitializeAsync(Transform holder)
        {
            Services.Register(new PanelStyleService(_popupStyles, _fadeStyles));
            return UniTask.CompletedTask;
        }
    }
}
