using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Haptic", fileName = "Module_Haptic")]
    public sealed class ModuleHaptic : Module
    {
        [Header("Sequential")]
        [Tooltip("Khoảng nghỉ (ms) thêm sau mỗi haptic trong chuỗi.")]
        [SerializeField] private int _gapMs = 20;

        [Header("Cooldown")]
        [Tooltip("Thời gian tối thiểu (ms) giữa 2 lần rung. Dùng Force để bỏ qua.")]
        [SerializeField] private int _cooldownMs = 80;

        [Header("Android")]
        [Tooltip("Cường độ rung Android. 0 = tắt, 1 = thường, 2 = mạnh gấp đôi.")]
        [Range(0f, 2f)]
        [SerializeField] private float _androidIntensity = 1f;

        private const string SaveKey = "haptic";

        private IHapticProvider _provider;
        private HapticSaveData _save;
        private float _lastPlay = float.MinValue;

        private bool Active => _save == null || _save.Active;

        public override UniTask InitializeAsync(Transform holder)
        {
            _save = Services.Get<SaveService>().Load<HapticSaveData>(SaveKey);
            _provider = CreateProvider();
            return UniTask.CompletedTask;
        }

        private IHapticProvider CreateProvider()
        {
#if UNITY_EDITOR
            return new HapticProviderDefault();
#elif UNITY_IOS
            return HapticNativeRegistry.IOSProvider ?? new HapticProviderIOS();
#elif UNITY_ANDROID
            return new HapticProviderAndroid(_androidIntensity);
#else
            return new HapticProviderDefault();
#endif
        }

        public override void Subscribe()
        {
            EventBus.On<HapticPlay>(OnPlay);
            EventBus.OnAsync<HapticSequence>(OnSequence);
            EventBus.On<HapticSetActive>(OnSetActive);
            EventBus.On<HapticGet>(e => e.Reply?.Invoke(Active));
        }

        private void OnPlay(HapticPlay e)
        {
            if (!CanPlay(e.Force)) return;
            _provider.Play(e.Type);
        }

        private async UniTask OnSequence(HapticSequence e)
        {
            if (e.Types == null || e.Types.Length == 0) return;
            if (!CanPlay(e.Force)) return;

            for (int i = 0; i < e.Types.Length; i++)
            {
                int ms = _provider.Play(e.Types[i]);
                if (i < e.Types.Length - 1)
                    await UniTask.Delay(ms + _gapMs);
            }
        }

        private void OnSetActive(HapticSetActive e)
        {
            _save.Active = e.Active;
            Services.Get<SaveService>().Set(SaveKey, _save);
            EventBus.Publish(new HapticChanged { Active = e.Active }).Forget();
        }

        // Active chặn trước; Force chỉ bỏ qua cooldown (không ghi đè user tắt haptic).
        private bool CanPlay(bool force)
        {
            if (!Active) return false;

            float now = Time.realtimeSinceStartup;
            if (!force && _cooldownMs > 0 && (now - _lastPlay) * 1000f < _cooldownMs)
                return false;

            _lastPlay = now;
            return true;
        }
    }
}
