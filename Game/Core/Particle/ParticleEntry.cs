using System;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>Một hiệu ứng particle được khai báo trong module (tên + prefab).</summary>
    [Serializable]
    public class ParticleEntry
    {
        [Tooltip("Used as the ParticleType enum name. Letters, digits, and underscores only.")]
        public string Name = "NewEffect";

        [Tooltip("ParticleSystem prefab. For UI rendering above canvases, add a UIParticle component to the prefab root (install ParticleEffectForUGUI via SA / Install / ParticleEffectForUGUI).")]
        public ParticleSystem Prefab;
    }
}
