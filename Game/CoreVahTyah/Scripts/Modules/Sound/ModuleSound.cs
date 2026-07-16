using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Sound", fileName = "Module_Sound")]
    [ModuleRequires(typeof(ModuleSettings))]
    public sealed class ModuleSound : Module
    {
        [BoxGroup("Audio")] [Range(0f, 1f)] public float MasterVolume = 1f;
        [BoxGroup("Audio")] [Min(1)] public int PoolSize = 4;
        [BoxGroup("Audio")] [Min(0)] public int CooldownMs = 60;

        [BoxGroup("Clips")] public List<SoundEntry> Sounds = new List<SoundEntry>();

        private AudioSource[] _pool;
        private int _poolIndex;
        // key bằng int (cast từ enum) → tra O(1) không box. Reorder list không ảnh hưởng.
        private readonly Dictionary<int, SoundEntry> _byId = new Dictionary<int, SoundEntry>();
        private readonly Dictionary<int, float> _lastPlay = new Dictionary<int, float>();

        public override UniTask InitializeAsync(Transform holder)
        {
            var go = new GameObject("[SoundPool]");
            go.transform.SetParent(holder);

            _pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                _pool[i] = go.AddComponent<AudioSource>();
                _pool[i].playOnAwake = false;
            }

            _byId.Clear();
            foreach (var s in Sounds)
                _byId[(int)s.Id] = s;
            
            Services.TryGet<SettingsService>(out var settings);
            Services.Register(new SoundService(_pool, Sounds, MasterVolume, CooldownMs, settings));

            return UniTask.CompletedTask;
        }
    }
}
