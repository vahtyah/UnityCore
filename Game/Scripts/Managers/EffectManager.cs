using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    [SerializeField] EffectConfig effectConfig;

    public static Dictionary<ParticleType, ParticleData> particleDataMap { get; private set; }

    private void Start()
    {
        if(effectConfig == null) return;
        InitParticles();
    }

    private void InitParticles()
    {
        particleDataMap = new Dictionary<ParticleType, ParticleData>();

        foreach (var particleType in effectConfig.ParticleDataArray)
        {
            particleDataMap[particleType.Type] = particleType;
        }
    }
    
    public static GameObject PlayParticle(ParticleType type, Vector3 position)
    {
        if (particleDataMap == null || !particleDataMap.TryGetValue(type, out var particleData)) return null;
        var obj = Pool.Spawn(particleData.ParticlePrefab, position, Quaternion.identity);

        if (obj.TryGetComponent(out ParticleSystem particleSystem)) particleSystem.Play();

        return obj;
    }

    // public static async UniTask PlayParticleAsync(ParticleType type, Vector3 position)
    // {
    //     if (particleDataMap == null || !particleDataMap.TryGetValue(type, out var particleData)) return;
    //     var obj = Pool.Spawn(particleData.ParticlePrefab, position, Quaternion.identity);
    //     
    //     if (obj.TryGetComponent(out ParticleSystem particleSystem))
    //     {
    //         particleSystem.Play();
    //         await UniTask.WaitUntil(() => !particleSystem.isPlaying);
    //     }
    // }
}