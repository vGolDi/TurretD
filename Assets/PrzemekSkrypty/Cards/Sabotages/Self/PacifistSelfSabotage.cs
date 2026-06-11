using UnityEngine;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// SELF: Disables turret upgrades for one wave. Gives bonus gold at the
    /// end of the wave as compensation.
    /// </summary>
    [CreateAssetMenu(fileName = "SelfSabotage_Pacifist",
        menuName = "Tower Defense/Cards/Sabotages/Self/Pacifist")]
    public class PacifistSelfSabotage : SabotageEffectBase
    {
        [Min(0), Tooltip("Gold awarded at wave end")]
        public int bonusGoldAtWaveEnd = 100;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var pcm = target?.GetComponent<PlayerCardManager>();
            pcm?.SetUpgradesDisabled(true);

            // Stash the bonus on WaveModifiers so wave end can pay it.
            var wm = GetWaveManager(target);
            wm?.ApplyWaveModifiers(m => { /* no specific flag needed - duration system removes it */ });

            // We rely on the sabotage card's duration to be exactly 1 wave (round-based).
            // When it expires, Remove() flips upgrades back on and pays the bonus.

            LogSabotage(target, caster, $"Pacifist: upgrades disabled, +{bonusGoldAtWaveEnd} on wave end");
        }

        public override void Remove(PhotonView target, PhotonView caster)
        {
            var pcm = target?.GetComponent<PlayerCardManager>();
            pcm?.SetUpgradesDisabled(false);
            GetPlayerGold(target)?.AddGold(bonusGoldAtWaveEnd);
        }
    }
}
