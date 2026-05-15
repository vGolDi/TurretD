using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    [CreateAssetMenu(fileName = "Sabotage_WaveBoss",
        menuName = "Tower Defense/Cards/Sabotage Effects/Wave Boss")]
    public class WaveBossSabotage : SabotageEffectBase
    {
        [Header("Wave Modifier Settings")]
        [Tooltip("Enemy prefab to spawn at the end of the wave")]
        public GameObject bossPrefab;

        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            if (bossPrefab == null)
            {
                Debug.LogError("[WaveBossSabotage] Boss prefab is not assigned!");
                return;
            }

            WaveManager targetWaveManager = GetWaveManager(targetPhotonView);
            
            if (targetWaveManager != null)
            {
                targetWaveManager.ApplyWaveModifiers(mod =>
                {
                    mod.bonusEnemyPrefabs.Add(bossPrefab);
                });
                
                LogSabotage(targetPhotonView, casterPhotonView, 
                    $"?? Added a boss to the next wave");
            }
            else
            {
                Debug.LogError("[WaveBossSabotage] Could not find WaveManager on target!");
            }
        }

        public override string GetEffectDescription()
        {
            return $"?? Adds a powerful enemy to the end of the next wave";
        }
    }
}
