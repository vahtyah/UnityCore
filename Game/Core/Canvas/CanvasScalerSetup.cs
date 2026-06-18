using UnityEngine;
using UnityEngine.UI;

namespace StandardAssets
{
    /// <summary>
    /// Tự áp đặt cấu hình CanvasScaler chuẩn (720x1280, scale theo màn hình).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasScalerSetup : MonoBehaviour
    {
        private void OnEnable()
        {
            Apply(GetComponent<CanvasScaler>());
        }

        private void OnValidate()
        {
            Apply(GetComponent<CanvasScaler>());
        }

        public static void Apply(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 100f;
        }

        private void Reset()
        {
            Apply(GetComponent<CanvasScaler>());
        }
    }
}
