using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    [CreateAssetMenu(fileName = "Sabotage_WaveHP",
        menuName = "Tower Defense/Cards/Sabotage Effects/Wave HP Modifier")]
    public class WaveHPSabotage : SabotageEffectBase
    {
        [Header("Wave Modifier Settings")]
        [Range(1f, 3f), Tooltip("Multiplier for enemy HP (e.g. 1.2 = +20% HP)")]
        public float hpMultiplier = 1.2f;

        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            WaveManager targetWaveManager = GetWaveManager(targetPhotonView);
            
            if (targetWaveManager != null)
            {
                targetWaveManager.ApplyWaveModifiers(mod =>
                {
                    mod.enemyHPMultiplier *= hpMultiplier;
                });
                
                LogSabotage(targetPhotonView, casterPhotonView, 
                    $"?? Increased wave enemy HP by {(hpMultiplier - 1f) * 100f}%");
            }
            else
            {
                Debug.LogError("[WaveHPSabotage] Could not find WaveManager on target!");
            }
        }

        public override string GetEffectDescription()
        {
            return $"?? Enemies in next wave have +{(hpMultiplier - 1f) * 100f}% HP";
        }
    }
}
