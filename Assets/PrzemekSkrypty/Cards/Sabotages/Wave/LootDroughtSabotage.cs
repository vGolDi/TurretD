using UnityEngine;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Reduces gold reward from kills for one wave.
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_LootDrought",
        menuName = "Tower Defense/Cards/Sabotages/Wave/Loot Drought")]
    public class LootDroughtSabotage : SabotageEffectBase
    {
        [Range(0.1f, 1f), Tooltip("Gold reward multiplier (0.85 = -15%)")]
        public float goldRewardMultiplier = 0.85f;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var wm = GetWaveManager(target);
            wm?.ApplyWaveModifiers(m => m.goldRewardMultiplier *= goldRewardMultiplier);
            LogSabotage(target, caster, $"Gold rewards x{goldRewardMultiplier:F2}");
        }
    }
}
