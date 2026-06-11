using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Spawns multiple boss prefabs in the next wave. Reuses
    /// WaveModifiers.bonusEnemyPrefabs which WaveSpawnState consumes.
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_Apocalypse",
        menuName = "Tower Defense/Cards/Sabotages/Wave/Apocalypse")]
    public class ApocalypseSabotage : SabotageEffectBase
    {
        [Tooltip("Bosses to spawn at the end of the next wave")]
        public List<GameObject> bossPrefabs = new List<GameObject>();

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var wm = GetWaveManager(target);
            wm?.ApplyWaveModifiers(m => m.bonusEnemyPrefabs.AddRange(bossPrefabs));
            LogSabotage(target, caster, $"Apocalypse: {bossPrefabs.Count} bosses incoming");
        }
    }
}
