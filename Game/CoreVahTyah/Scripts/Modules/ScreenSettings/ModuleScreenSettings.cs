using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Screen Settings", fileName = "Module_ScreenSettings")]
    public sealed class ModuleScreenSettings : Module
    {
        public enum FrameRate { Rate30 = 30, Rate60 = 60, Rate90 = 90, Rate120 = 120 }

        [BoxGroup("Frame Rate")]
        [Tooltip("Tự lấy theo refresh rate màn hình thiết bị.")]
        [SerializeField] private bool _autoFrameRate;
        [BoxGroup("Frame Rate")]
        [SerializeField] private FrameRate _defaultFrameRate = FrameRate.Rate60;
        [BoxGroup("Frame Rate")]
        [Tooltip("FPS khi máy ở chế độ tiết kiệm pin (iOS).")]
        [SerializeField] private FrameRate _batterySaveFrameRate = FrameRate.Rate30;

        [BoxGroup("Khác")]
        [SerializeField] private int _vSyncCount = 0;
        [BoxGroup("Khác")]
        [Tooltip("-1 = NeverSleep (màn hình không tự tắt).")]
        [SerializeField] private int _sleepTimeout = SleepTimeout.NeverSleep;

        public override UniTask InitializeAsync(Transform holder)
        {
            QualitySettings.vSyncCount = _vSyncCount;
            Screen.sleepTimeout = _sleepTimeout;
            Application.targetFrameRate = ResolveFrameRate();
            return UniTask.CompletedTask;
        }

        private int ResolveFrameRate()
        {
            if (_autoFrameRate)
            {
                var ratio = Screen.currentResolution.refreshRateRatio;
                if (ratio.numerator != 0 && ratio.denominator != 0)
                    return Mathf.RoundToInt((float)(ratio.numerator / (double)ratio.denominator));
                return (int)_defaultFrameRate;
            }

#if UNITY_IOS
            if (UnityEngine.iOS.Device.lowPowerModeEnabled)
                return (int)_batterySaveFrameRate;
#endif
            return (int)_defaultFrameRate;
        }
    }
}
