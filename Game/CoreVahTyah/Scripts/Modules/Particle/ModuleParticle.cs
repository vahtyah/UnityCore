using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    /// <summary>
    /// Factory mỏng: register <see cref="ParticleService"/> để spawn particle qua shortcut <see cref="Particles"/>.
    /// Prewarm pool cho các effect lúc boot. Không nghe event. Cần <see cref="ModulePool"/> boot trước.
    /// </summary>
    [CreateAssetMenu(menuName = "VahTyah/Modules/Particle", fileName = "Module_Particle")]
    [ModuleRequires(typeof(ModulePool))]
    public sealed class ModuleParticle : Module
    {
        [BoxGroup("Effects")] public List<ParticleEntry> Effects = new List<ParticleEntry>();

        public override UniTask InitializeAsync(Transform holder)
        {
            if (!Services.TryGet<PoolService>(out var pool))
                Debug.LogWarning("[Particle] Cần ModulePool boot trước ModuleParticle để prewarm/spawn.");

            if (pool != null)
                foreach (var e in Effects)
                    if (e != null && e.Prefab != null && e.Prewarm > 0)
                        pool.Register(e.Prefab, e.Prewarm);

            Services.Register(new ParticleService(Effects, pool));
            return UniTask.CompletedTask;
        }
    }
}
