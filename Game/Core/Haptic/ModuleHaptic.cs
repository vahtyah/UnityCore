using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Module phát haptic theo nền tảng, có cooldown chống spam và phát tuần tự
    /// nhiều haptic với khoảng cách giữa các lần.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/Haptic", fileName = "Module_Haptic", order = 9)]
    internal sealed class ModuleHaptic : SAModule
    {
        [Header("Sequential Playback")]
        [Tooltip("Extra gap in milliseconds added after each haptic before the next one plays.\nTotal wait = vibration duration + this gap.\nExample: Heavy on Android vibrates 80 ms. With gap = 50, next haptic starts after 130 ms.")]
        [SerializeField]
        private int _gapBetweenHaptics = 20;

        [Header("Cooldown")]
        [Tooltip("Minimum time in seconds between haptic plays.\nPrevents spamming when called every frame (e.g. in Update).\nUse SA.Haptic(true, HapticType.X) to bypass.")]
        [SerializeField]
        private int _cooldown = 80;

        [Header("Intensity")]
        [Tooltip("Haptic strength on Android. 0 = off, 1 = normal, 2 = double.\nScales all vibration durations proportionally.")]
        [Range(0f, 2f)]
        [SerializeField]
        private float _intensityAndroid = 1f;

        private IHapticProvider _provider;

        private float _lastPlayTime = float.MinValue;

        public override Task InitializeAsync()
        {
            // Chọn provider theo nền tảng: iOS (8), Android (11), còn lại Default
            RuntimePlatform platform = Application.platform;
            if ((int)platform == 8)
            {
                _provider = HapticNativeRegistry.IOSProvider ?? new HapticProviderIOS();
            }
            else if ((int)platform == 11)
            {
                _provider = new HapticProviderAndroid(_intensityAndroid);
            }
            else
            {
                _provider = new HapticProviderDefault();
            }
            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            SATypedBus.OnAsync<Ev.HapticPlay>(OnHapticPlay);
        }

        private async Task OnHapticPlay(Ev.HapticPlay e)
        {
            bool force = e.Force;
            float now = Time.realtimeSinceStartup;
            // Bỏ qua nếu còn trong cooldown (trừ khi force)
            if (!force && now - _lastPlayTime < (float)_cooldown / 1000f)
            {
                return;
            }
            _lastPlayTime = now;
            HapticType[] types = e.Types;
            if (types == null || types.Length == 0)
            {
                return;
            }
            for (int i = 0; i < types.Length; i++)
            {
                int vibrationMs = _provider.Play(types[i]);
                // Chờ giữa các haptic = thời lượng rung + gap
                if (i < types.Length - 1)
                {
                    await Task.Delay(vibrationMs + _gapBetweenHaptics);
                }
            }
        }
    }
}
