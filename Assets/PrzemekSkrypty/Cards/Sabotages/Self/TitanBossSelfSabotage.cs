using UnityEngine;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// SELF-SABOTAGE: Titan Boss
    /// Spawns a mega boss with massive HP. If killed before reaching base = huge reward.
    /// Requires bossPrefab to be set (or uses existing boss with multiplied HP).
    /// </summary>
    [CreateAssetMenu(fileName = "SelfSabo_TitanBoss",
        menuName = "Tower Defense/Cards/Sabotages/Self/Titan Boss")]
    public class TitanBossSelfSabotage : SabotageEffectBase
    {
        [Header("Titan Boss Settings")]
        [Tooltip("Boss prefab to spawn (use your strongest enemy)")]
        public GameObject titanPrefab;

        [Tooltip("HP multiplier on top of boss base HP (e.g. 5 = 5x normal boss HP)")]
        public float titanHPMultiplier = 5f;

        [Tooltip("Speed multiplier (titans are slow)")]
        public float titanSpeedMultiplier = 0.5f;

        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            if (titanPrefab == null)
            {
                Debug.LogError("[TitanBoss] Titan prefab not assigned!");
                return;
            }

            WaveManager wm = GetWaveManager(targetPhotonView);
            if (wm == null)
            {
                Debug.LogError("[TitanBoss] WaveManager not found!");
                return;
            }

            wm.ApplyWaveModifiers(mod =>
            {
                mod.bonusEnemyPrefabs.Add(titanPrefab);
                // The titan has its own HP which will be multiplied
                // by enemyHPMultiplier during spawn
                mod.enemyHPMultiplier *= titanHPMultiplier;
                mod.enemySpeedMultiplier *= titanSpeedMultiplier;
            });

            LogSabotage(targetPhotonView, casterPhotonView,
                $"SELF-SABOTAGE: Titan Boss spawning! HP x{titanHPMultiplier}, Speed x{titanSpeedMultiplier}");
        }

        public override void Remove(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            // Reset HP/speed multipliers after the wave
            WaveManager wm = GetWaveManager(targetPhotonView);
            if (wm != null)
            {
                wm.ApplyWaveModifiers(mod =>
                {
                    mod.enemyHPMultiplier /= titanHPMultiplier;
                    mod.enemySpeedMultiplier /= titanSpeedMultiplier;
                });
            }
        }

        public override string GetEffectDescription()
        {
            return $"A massive Titan Boss appears with {titanHPMultiplier}x HP!\n" +
                   $"Kill it for epic rewards!";
        }
    }
}
