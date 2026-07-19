using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VahTyah.Inspector;

namespace VahTyah
{
    /// <summary>
    /// Host cho animation "bay về counter" dùng chung: tạo canvas overlay [CollectFly], giữ danh sách
    /// <see cref="CollectProfile"/>, register <see cref="CollectFlyService"/> để ModuleItem/ModuleHeart/... gọi.
    /// Cần <see cref="ModulePool"/> boot trước. Chỉnh profile 1 chỗ → mọi counter ăn theo.
    /// </summary>
    [CreateAssetMenu(menuName = "VahTyah/Modules/CollectFly", fileName = "Module_CollectFly")]
    [ModuleRequires(typeof(ModulePool))]
    public sealed class ModuleCollectFly : Module
    {
        [BoxGroup("Profiles")]
        [Tooltip("Profile animation dùng chung. Nên có 1 profile Id=Default làm fallback.")]
        public List<CollectProfile> Profiles = new List<CollectProfile> { new CollectProfile() };

        [BoxGroup("Canvas")] public int CanvasSortingOrder = 9999;

        public override UniTask InitializeAsync(Transform holder)
        {
            var canvasObj = new GameObject("[CollectFly]");
            canvasObj.transform.SetParent(holder);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            // Mirror CanvasScaler của HUD (ScaleWithScreenSize, 1080x1920, Expand) để sprite bay scale đồng nhất.
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.5f;

            // KHÔNG GraphicRaycaster: overlay thuần trang trí. Ở sortingOrder cao (9999) mà có raycaster thì
            // sprite bay (raycastTarget mặc định true) sẽ nuốt tap dọc đường bay, đè lên mọi UI bên dưới.

            var runner = canvasObj.AddComponent<CollectFlyRunner>();
            Services.Register(new CollectFlyService(runner, Profiles));

            return UniTask.CompletedTask;
        }
    }
}
