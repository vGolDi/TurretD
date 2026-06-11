using UnityEngine;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    [CreateAssetMenu(fileName = "Sabotage_WaveSpeed",
        menuName = "Tower Defense/Cards/Sabotages/Wave/Wave Speed Modifier")]
    public class WaveSpeedSabotage : SabotageEffectBase
    {
        [Header("Wave Modifier Settings")]
        [Range(1f, 3f), Tooltip("Multiplier for enemy speed (e.g. 1.3 = +30% Speed)")]
        public float speedMultiplier = 1.3f;

        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            WaveManager targetWaveManager = GetWaveManager(targetPhotonView);
            
            if (targetWaveManager != null)
            {
                targetWaveManager.ApplyWaveModifiers(mod =>
                {
                    mod.enemySpeedMultiplier *= speedMultiplier;
                });
                
                LogSabotage(targetPhotonView, casterPhotonView, 
                    $"?? Increased wave enemy speed by {(speedMultiplier - 1f) * 100f}%");
            }
            else
            {
                Debug.LogError("[WaveSpeedSabotage] Could not find WaveManager on target!");
            }
        }

        public override string GetEffectDescription()
        {
            return $"?? Enemies in next wave are +{(speedMultiplier - 1f) * 100f}% faster";
        }
    }
}
