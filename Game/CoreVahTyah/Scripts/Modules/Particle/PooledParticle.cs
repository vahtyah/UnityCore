using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Gắn lên prefab particle one-shot. Play khi spawn, tự Despawn về Pool khi particle dừng.
    /// Yêu cầu: hợp cho effect KHÔNG looping (looping sẽ không bao giờ Stopped → không tự despawn).
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PooledParticle : MonoBehaviour, IPoolable
    {
        private ParticleSystem _ps;

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            var main = _ps.main;
            main.stopAction = ParticleSystemStopAction.Callback; // để OnParticleSystemStopped chạy
        }

        public void OnSpawnFromPool() => _ps.Play(true);
        public void OnReturnToPool() => _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        private void OnParticleSystemStopped() => Pool.Despawn(gameObject);
    }
}
