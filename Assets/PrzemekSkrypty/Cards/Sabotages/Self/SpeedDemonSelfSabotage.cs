using UnityEngine;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// SELF: Enemies move +30% faster for one wave but kill gold is +75%.
    /// </summary>
    [CreateAssetMenu(fileName = "SelfSabotage_SpeedDemon",
        menuName = "Tower Defense/Cards/Sabotages/Self/Speed Demon")]
    public class SpeedDemonSelfSabotage : SabotageEffectBase
    {
        [Range(1f, 2f)] public float enemySpeedMultiplier = 1.3f;
        [Range(1f, 3f)] public float killGoldMultiplier = 1.75f;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var wm = GetWaveManager(target);
            wm?.ApplyWaveModifiers(m =>
            {
                m.enemySpeedMultiplier *= enemySpeedMultiplier;
                m.goldRewardMultiplier *= killGoldMultiplier;
            });
            LogSabotage(target, caster,
                $"Speed Demon: enemies x{enemySpeedMultiplier} speed, gold x{killGoldMultiplier}");
        }
    }
}
