using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Particle", fileName = "Module_Particle")]
    public sealed class ModuleParticle : Module
    {
        public List<ParticleEntry> Effects = new List<ParticleEntry>();

        // key bằng int (cast từ enum) để tra O(1) không box. Reorder list không ảnh hưởng.
        private readonly Dictionary<int, ParticleEntry> _byId = new Dictionary<int, ParticleEntry>();

        public override UniTask InitializeAsync(Transform holder)
        {
            _byId.Clear();
            bool hasPool = Services.Has<PoolService>();

            foreach (var e in Effects)
            {
                if (e.Prefab == null) continue;
                _byId[(int)e.Id] = e;

                if (hasPool && e.Prewarm > 0)
                    Pool.Register(e.Prefab, e.Prewarm);
            }

            if (!hasPool)
                Debug.LogWarning("[Particle] Cần ModulePool boot trước ModuleParticle để prewarm.");

            return UniTask.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<ParticlePlay>(OnPlay);
        }

        private void OnPlay(ParticlePlay e)
        {
            if (!_byId.TryGetValue((int)e.Id, out var entry) || entry.Prefab == null)
                return;

            Pool.Spawn(entry.Prefab, e.Position, Quaternion.identity);
        }
    }
}
