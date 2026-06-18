using System;
using System.Collections.Generic;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Một mục âm thanh trong thư viện: tên, volume, pitch và danh sách clip.
    /// Có thể phát tuần tự hoặc ngẫu nhiên các clip.
    /// </summary>
    [Serializable]
    public class SoundEntry : ISerializationCallbackReceiver
    {
        [Tooltip("Used as the AudioType enum name. Letters, digits, and underscores only.")]
        public string Name = "NewSound";

        [Tooltip("Base volume multiplier for this sound (0–1).")]
        [Range(0f, 1f)]
        public float Volume = 1f;

        [Tooltip("Base pitch for this sound.")]
        [Range(0.1f, 2f)]
        public float Pitch = 1f;

        [SerializeField]
        [HideInInspector]
        private bool _initialized;

        [Tooltip("If true, clips play in order. If false, a random clip is picked each time.")]
        public bool Sequential;

        public List<AudioClip> Clips = new List<AudioClip>();

        [NonSerialized]
        private int _clipIndex;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            // Khởi tạo giá trị mặc định lần đầu (entry mới thêm)
            if (!_initialized)
            {
                Volume = 1f;
                Pitch = 1f;
                _initialized = true;
            }
        }

        public AudioClip GetClip()
        {
            if (Clips == null || Clips.Count == 0)
            {
                return null;
            }
            if (Clips.Count == 1)
            {
                return Clips[0];
            }
            if (Sequential)
            {
                AudioClip result = Clips[_clipIndex % Clips.Count];
                _clipIndex = (_clipIndex + 1) % Clips.Count;
                return result;
            }
            return Clips[UnityEngine.Random.Range(0, Clips.Count)];
        }
    }
}
