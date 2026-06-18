using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Module phát âm thanh dùng pool AudioSource, có cooldown chống spam mỗi loại sound.
    /// Sound được tham chiếu theo id (index trong danh sách Sounds).
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/Sound", fileName = "Module_Sound", order = 21)]
    internal sealed class ModuleSound : SAModule
    {
        [Tooltip("Master volume applied to every sound (0–1).")]
        [Range(0f, 1f)]
        public float MasterVolume = 1f;

        [Tooltip("Number of AudioSources in the pool. Increase for dense overlapping sounds.")]
        [Min(1f)]
        public int PoolSize = 4;

        [Tooltip("Minimum time in milliseconds between two plays of the same sound. Prevents spam from Update() calls.")]
        [Min(0f)]
        public int CooldownMs = 60;

        private AudioSource[] _pool;

        private int _poolIndex;

        private readonly Dictionary<int, float> _lastPlayTime = new Dictionary<int, float>();

        public List<SoundEntry> Sounds = new List<SoundEntry>();

        private SoundEntry[] _indexCache;

        public override Task InitializeAsync()
        {
            // Tạo pool AudioSource thường trú
            GameObject val = new GameObject("[SA] SoundPool");
            Object.DontDestroyOnLoad(val);
            _pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                _pool[i] = val.AddComponent<AudioSource>();
                _pool[i].playOnAwake = false;
            }
            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            SATypedBus.On<Ev.SoundPlay>(OnPlaySound);
        }

        private void OnPlaySound(Ev.SoundPlay e)
        {
            if (_pool == null)
            {
                return;
            }
            int typeId = e.Type;
            float volume = e.Volume;
            float rawPitch = e.Pitch;
            float pitch = (rawPitch == 0f) ? 1f : rawPitch;
            SoundEntry entry = Get(typeId);
            if (entry == null)
            {
                return;
            }
            // Cooldown theo từng loại sound
            if (CooldownMs > 0)
            {
                float now = Time.realtimeSinceStartup;
                if (_lastPlayTime.TryGetValue(typeId, out var last) && (now - last) * 1000f < (float)CooldownMs)
                {
                    return;
                }
                _lastPlayTime[typeId] = now;
            }
            AudioClip clip = entry.GetClip();
            if (clip == null)
            {
                Debug.LogWarning("[SA Sound] Entry '" + entry.Name + "' has no AudioClips assigned.");
                return;
            }
            AudioSource source = GetPooledSource();
            source.pitch = entry.Pitch * pitch;
            source.PlayOneShot(clip, entry.Volume * volume * MasterVolume);
        }

        private AudioSource GetPooledSource()
        {
            // Ưu tiên source đang rảnh, vòng tròn quanh pool
            for (int i = 0; i < _pool.Length; i++)
            {
                int idx = (_poolIndex + i) % _pool.Length;
                if (!_pool[idx].isPlaying)
                {
                    _poolIndex = (idx + 1) % _pool.Length;
                    return _pool[idx];
                }
            }
            AudioSource result = _pool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _pool.Length;
            return result;
        }

        public SoundEntry Get(int soundTypeId)
        {
            if (_indexCache == null || _indexCache.Length != Sounds.Count)
            {
                RebuildCache();
            }
            if (soundTypeId < 0 || soundTypeId >= _indexCache.Length)
            {
                Debug.LogWarning($"[SA Sound] SoundType id {soundTypeId} out of range (library has {_indexCache.Length} entries).");
                return null;
            }
            return _indexCache[soundTypeId];
        }

        private void RebuildCache()
        {
            _indexCache = new SoundEntry[Sounds.Count];
            for (int i = 0; i < Sounds.Count; i++)
            {
                _indexCache[i] = Sounds[i];
            }
        }

        private void OnValidate()
        {
            _indexCache = null;
        }
    }
}
