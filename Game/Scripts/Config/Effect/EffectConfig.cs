using System;
using UnityEngine;
#if VAHTYAH_CUSTOM_INSPECTOR
using VahTyah.Inspector;
#endif

[CreateAssetMenu(fileName = "EffectConfig", menuName = "Config/EffectConfig", order = 0)]
public class EffectConfig : ScriptableObject
{
#if VAHTYAH_CUSTOM_INSPECTOR
    [BoxGroup("Data")]
#endif 
    public ParticleData[] ParticleDataArray;
}

[Serializable]
public class ParticleData
{
    public ParticleType Type;
    public GameObject ParticlePrefab;
}

public enum ParticleType
{

}