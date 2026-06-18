using System;
using System.Collections.Generic;
using UnityEngine;

namespace VahTyah
{
    [Serializable]
    public class SoundEntry
    {
        public SoundId Id;
        [Range(0f, 1f)] public float Volume = 1f;
        [Range(0.1f, 2f)] public float Pitch = 1f;
        public bool Sequential;
        public List<AudioClip> Clips = new List<AudioClip>();

        [NonSerialized] private int _clipIndex;

        public AudioClip GetClip()
        {
            if (Clips == null || Clips.Count == 0) return null;
            if (Clips.Count == 1) return Clips[0];

            if (Sequential)
            {
                var clip = Clips[_clipIndex % Clips.Count];
                _clipIndex = (_clipIndex + 1) % Clips.Count;
                return clip;
            }

            return Clips[UnityEngine.Random.Range(0, Clips.Count)];
        }
    }
}
