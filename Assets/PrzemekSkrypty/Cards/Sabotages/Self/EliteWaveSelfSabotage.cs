using UnityEngine;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// SELF-SABOTAGE: Elite Wave
    /// Enemies in next wave have +100% HP and +50% count.
    /// Reward: bonus gold + gold multiplier for surviving.
    /// </summary>
    [CreateAssetMenu(fileName = "SelfSabo_EliteWave",
        menuName = "Tower Defense/Cards/Sabotages/Self/Elite Wave")]
    public class EliteWaveSelfSabotage : SabotageEffectBase
    {
        [Header("Elite Wave Settings")]
        [Tooltip("HP multiplier for enemies (e.g. 2.0 = 2x HP)")]
        public float hpMultiplier = 2f;

        [Tooltip("Enemy count multiplier (e.g. 1.5 = +50% enemies)")]
        public float countMultiplier = 1.5f;

        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            WaveManager wm = GetWaveManager(targetPhotonView);
            if (wm == null)
            {
                Debug.LogError("[EliteWave] WaveManager not found!");
                return;
            }

            wm.ApplyWaveModifiers(mod =>
            {
                mod.enemyHPMultiplier *= hpMultiplier;
                mod.enemyCountMultiplier *= countMultiplier;
            });

            LogSabotage(targetPhotonView, casterPhotonView,
                $"SELF-SABOTAGE: Elite Wave! HP x{hpMultiplier}, Count x{countMultiplier}");
        }

        public override string GetEffectDescription()
        {
            return $"Enemies have {(hpMultiplier - 1f) * 100f}% more HP and " +
                   $"{(countMultiplier - 1f) * 100f}% more count.\n" +
                   $"Survive for bonus gold!";
        }
    }
}
