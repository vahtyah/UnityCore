using System.Collections.Generic;
using UnityEngine;

namespace VahTyah
{
    public sealed class SoundService
    {
        private readonly AudioSource[] _pool;
        private readonly float _masterVolume;
        private readonly int _cooldownMs;
        private readonly SettingsService _settings;

        private readonly Dictionary<int, SoundEntry> _byId = new Dictionary<int, SoundEntry>();
        private readonly Dictionary<int, float> _lastPlay = new Dictionary<int, float>();

        private int _poolIndex;

        private bool Active => _settings == null || _settings.Sfx;

        public SoundService(AudioSource[] pool, IReadOnlyList<SoundEntry> sounds, float masterVolume, int cooldownMs,
            SettingsService settings)
        {
            _pool = pool;
            _masterVolume = masterVolume;
            _cooldownMs = cooldownMs;
            _settings = settings;

            for (int i = 0; i < sounds.Count; i++)
                _byId[(int)sounds[i].Id] = sounds[i];
        }

        public void Play(SoundId id, float volume = 1f, float pitch = 1f, bool force = false)
        {
            if (!Active || _pool == null) return;

            int key = (int)id;
            if (!_byId.TryGetValue(key, out var entry)) return;

            if (!force && _cooldownMs > 0)
            {
                float now = Time.realtimeSinceStartup;
                if (_lastPlay.TryGetValue(key, out float last) && (now - last) * 1000f < _cooldownMs)
                    return;
                _lastPlay[key] = now;
            }

            var clip = entry.GetClip();
            if (clip == null) return;

            float vol = volume <= 0f ? 1f : volume;
            float pit = pitch <= 0f ? 1f : pitch;

            var source = GetSource();
            source.pitch = entry.Pitch * pit;
            source.PlayOneShot(clip, entry.Volume * vol * _masterVolume);
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