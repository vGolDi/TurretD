using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    [CreateAssetMenu(fileName = "Sabotage_WaveCount",
        menuName = "Tower Defense/Cards/Sabotage Effects/Wave Count Modifier")]
    public class WaveCountSabotage : SabotageEffectBase
    {
        [Header("Wave Modifier Settings")]
        [Range(1f, 3f), Tooltip("Multiplier for enemy count (e.g. 1.5 = +50% enemies)")]
        public float countMultiplier = 1.5f;

        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            WaveManager targetWaveManager = GetWaveManager(targetPhotonView);
            
            if (targetWaveManager != null)
            {
                targetWaveManager.ApplyWaveModifiers(mod =>
                {
                    mod.enemyCountMultiplier *= countMultiplier;
                });
                
                LogSabotage(targetPhotonView, casterPhotonView, 
                    $"?? Increased wave enemy count by {(countMultiplier - 1f) * 100f}%");
            }
            else
            {
                Debug.LogError("[WaveCountSabotage] Could not find WaveManager on target!");
            }
        }

        public override string GetEffectDescription()
        {
            return $"?? +{(countMultiplier - 1f) * 100f}% more enemies in next wave";
        }
    }
}
