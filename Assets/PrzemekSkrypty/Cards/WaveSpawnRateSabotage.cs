using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    [CreateAssetMenu(fileName = "Sabotage_WaveSpawnRate",
        menuName = "Tower Defense/Cards/Sabotage Effects/Wave Spawn Rate Modifier")]
    public class WaveSpawnRateSabotage : SabotageEffectBase
    {
        [Header("Wave Modifier Settings")]
        [Range(0.1f, 1f), Tooltip("Multiplier for spawn interval (e.g. 0.5 = 2x faster spawn)")]
        public float spawnRateMultiplier = 0.5f;

        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            WaveManager targetWaveManager = GetWaveManager(targetPhotonView);
            
            if (targetWaveManager != null)
            {
                targetWaveManager.ApplyWaveModifiers(mod =>
                {
                    mod.spawnRateMultiplier *= spawnRateMultiplier;
                });
                
                LogSabotage(targetPhotonView, casterPhotonView, 
                    $"?? Enemies spawn {1f / spawnRateMultiplier:F1}x faster");
            }
            else
            {
                Debug.LogError("[WaveSpawnRateSabotage] Could not find WaveManager on target!");
            }
        }

        public override string GetEffectDescription()
        {
            return $"?? Enemies in next wave spawn {1f / spawnRateMultiplier:F1}x faster";
        }
    }
}
