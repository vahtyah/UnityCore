using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Sound", fileName = "Module_Sound")]
    public sealed class ModuleSound : Module
    {
        [Range(0f, 1f)] public float MasterVolume = 1f;
        [Min(1)] public int PoolSize = 4;
        [Min(0)] public int CooldownMs = 60;

        public List<SoundEntry> Sounds = new List<SoundEntry>();

        private AudioSource[] _pool;
        private int _poolIndex;
        // key bằng int (cast từ enum) → tra O(1) không box. Reorder list không ảnh hưởng.
        private readonly Dictionary<int, SoundEntry> _byId = new Dictionary<int, SoundEntry>();
        private readonly Dictionary<int, float> _lastPlay = new Dictionary<int, float>();

        public override Task InitializeAsync(Transform holder)
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

            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<SoundPlay>(OnPlay);
        }

        private void OnPlay(SoundPlay e)
        {
            if (_pool == null) return;

            int id = (int)e.Id;
            if (!_byId.TryGetValue(id, out var entry)) return;

            float vol = e.Volume <= 0f ? 1f : e.Volume;   // mặc định 1 nếu không set
            float pitch = e.Pitch <= 0f ? 1f : e.Pitch;

            if (CooldownMs > 0)
            {
                float now = Time.realtimeSinceStartup;
                if (_lastPlay.TryGetValue(id, out float last) && (now - last) * 1000f < CooldownMs)
                    return;
                _lastPlay[id] = now;
            }

            var clip = entry.GetClip();
            if (clip == null) return;

            var source = GetSource();
            source.pitch = entry.Pitch * pitch;
            source.PlayOneShot(clip, entry.Volume * vol * MasterVolume);
        }

        private AudioSource GetSource()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                int idx = (_poolIndex + i) % _pool.Length;
                if (!_pool[idx].isPlaying)
                {
                    _poolIndex = (idx + 1) % _pool.Length;
                    return _pool[idx];
                }
            }

            var src = _pool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _pool.Length;
            return src;
        }
    }
}
