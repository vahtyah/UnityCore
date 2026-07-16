using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Gắn (hoặc auto-add lúc spawn) lên instance particle. Tự Play khi enable, Stop+Clear khi disable,
    /// và Despawn về Pool khi particle dừng (OnParticleSystemStopped).
    ///
    /// Tự lái qua <c>OnEnable/OnDisable</c> — KHÔNG dùng IPoolable — nên hoạt động đúng kể cả khi được
    /// <c>AddComponent</c> lúc spawn (không phụ thuộc cache IPoolable mà PoolService chốt ở lần spawn đầu).
    /// PoolService bật/tắt object qua SetActive nên OnEnable/OnDisable khớp đúng vòng đời spawn/despawn.
    ///
    /// Effect looping hoặc phòng leak: đặt <see cref="_maxLifetime"/> &gt; 0 để despawn cưỡng bức
    /// (looping không bao giờ Stopped nên nếu không có sẽ không tự trả về pool). Lưu ý: instance được
    /// auto-add lúc runtime luôn có _maxLifetime = 0; muốn đặt lifetime thì gắn sẵn component trên prefab.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PooledParticle : MonoBehaviour
    {
        [Tooltip("Tự despawn sau N giây kể cả chưa Stopped (an toàn cho looping/leak). 0 = tắt.")]
        [SerializeField, Min(0f)] private float _maxLifetime;

        private ParticleSystem _ps;
        private CancellationTokenSource _cts;

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            var main = _ps.main;
            main.stopAction = ParticleSystemStopAction.Callback; // để OnParticleSystemStopped chạy
        }

        private void OnEnable()
        {
            _ps.Play(true);
            if (_maxLifetime > 0f)
            {
                _cts = new CancellationTokenSource();
                GuardLifetime(_cts.Token).Forget();
            }
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Fallback: despawn sau _maxLifetime nếu particle chưa tự dừng. Hủy khi object bị disable (đã trả pool).
        private async UniTaskVoid GuardLifetime(CancellationToken token)
        {
            bool canceled = await UniTask
                .Delay((int)(_maxLifetime * 1000f), cancellationToken: token)
                .SuppressCancellationThrow();
            if (!canceled && this != null) Pool.Despawn(gameObject);
        }

        private void OnParticleSystemStopped() => Pool.Despawn(gameObject);
    }
}
