using UnityEngine;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Every spawned enemy in the next wave will revive once at 50% HP.
    /// Requires every enemy prefab to have an EnemyReviveOnDeath component
    /// (default maxRevives=0). Sabotage activates it via ApplyFromSabotage.
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_Necropolis",
        menuName = "Tower Defense/Cards/Sabotages/Wave/Necropolis")]
    public class NecropolisSabotage : SabotageEffectBase
    {
        [Tooltip("Optional revive prefab. Leave null to revive as the same enemy type.")]
        public GameObject revivePrefab;

        [Range(0.1f, 1f), Tooltip("HP percentage at revive")]
        public float reviveHpPercent = 0.5f;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var wm = GetWaveManager(target);
            wm?.ApplyWaveModifiers(m =>
            {
                m.forceReviveOnSpawn = true;
                m.forceRevivePrefab = revivePrefab;
                m.forceReviveHpPercent = reviveHpPercent;
            });
            LogSabotage(target, caster, $"Necropolis: every enemy revives once at {reviveHpPercent * 100f:F0}% HP");
        }
    }
}
