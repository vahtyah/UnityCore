using UnityEngine;
using UnityEngine.UI;

namespace VahTyah
{
    /// <summary>
    /// Tự áp cấu hình CanvasScaler chuẩn (720x1280, scale theo màn hình).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasScalerSetup : MonoBehaviour
    {
        [SerializeField] private Vector2 _referenceResolution = new Vector2(720f, 1280f);
        [Range(0f, 1f)]
        [SerializeField] private float _matchWidthOrHeight = 0.5f;

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();
        private void Reset() => Apply();

        private void Apply()
        {
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = _referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = _matchWidthOrHeight;
            scaler.referencePixelsPerUnit = 100f;
        }
    }
}
