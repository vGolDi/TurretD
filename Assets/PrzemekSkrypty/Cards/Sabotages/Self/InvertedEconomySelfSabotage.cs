using UnityEngine;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// SELF: For one wave, regular kills give 0 gold but every wave-completion
    /// bonus is multiplied (and any wave-spawn boss kill is worth a lot).
    /// All-or-nothing economy.
    /// 
    /// Implementation: zeroes goldRewardMultiplier (kills give 0), but adds a
    /// flat bonus to the wave's existing waveCompletionBonus. WaveManager already
    /// pays waveCompletionBonus at wave end via PayWaveCompletionBonus.
    /// </summary>
    [CreateAssetMenu(fileName = "SelfSabotage_InvertedEconomy",
        menuName = "Tower Defense/Cards/Sabotages/Self/Inverted Economy")]
    public class InvertedEconomySelfSabotage : SabotageEffectBase
    {
        [Min(0), Tooltip("Lump-sum gold paid at end of the wave instead of per-kill")]
        public int waveEndBonus = 500;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var wm = GetWaveManager(target);
            wm?.ApplyWaveModifiers(m => m.goldRewardMultiplier = 0f);

            LogSabotage(target, caster,
                $"Inverted Economy: 0 gold per kill, +{waveEndBonus} at wave end");
        }

        public override void Remove(PhotonView target, PhotonView caster)
        {
            // Pay the lump-sum on cleanup. Round-based duration of 1 means
            // Remove fires after the wave completes — perfect timing.
            GetPlayerGold(target)?.AddGold(waveEndBonus);
        }
    }
}
