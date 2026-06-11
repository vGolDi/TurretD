using UnityEngine;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Every spawned enemy in the next wave regenerates % maxHP per second.
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_Regen",
        menuName = "Tower Defense/Cards/Sabotages/Wave/Regen")]
    public class RegenSabotage : SabotageEffectBase
    {
        [Range(0.005f, 0.1f), Tooltip("HP regenerated per second (fraction of maxHP)")]
        public float percentPerSecond = 0.02f;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var wm = GetWaveManager(target);
            wm?.ApplyWaveModifiers(m => m.regenPercentPerSecond = Mathf.Max(m.regenPercentPerSecond, percentPerSecond));
            LogSabotage(target, caster, $"Enemies regen {percentPerSecond * 100f:F1}%/s");
        }
    }
}
