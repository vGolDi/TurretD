using UnityEngine;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Spawns a single very strong unique enemy as a bonus in the next wave.
    /// Uses bonusEnemyPrefabs slot (same as Apocalypse / WaveBoss) but assumes
    /// the prefab is configured with high HP and zero gold reward.
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_Mythic",
        menuName = "Tower Defense/Cards/Sabotages/Wave/Mythic")]
    public class MythicSabotage : SabotageEffectBase
    {
        [Tooltip("The mythic enemy prefab. Configure it with very high HP, immune element, 0 gold reward.")]
        public GameObject mythicPrefab;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            if (mythicPrefab == null)
            {
                Debug.LogWarning("[MythicSabotage] No prefab assigned!");
                return;
            }
            var wm = GetWaveManager(target);
            wm?.ApplyWaveModifiers(m => m.bonusEnemyPrefabs.Add(mythicPrefab));
            LogSabotage(target, caster, $"Mythic spawned: {mythicPrefab.name}");
        }
    }
}
