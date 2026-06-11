using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using ElementumDefense.Elements;
using ElementumDefense.Players;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// SELF: Disables building entirely for the next wave (existing turrets work).
    /// Compensation: bonus gold at wave end.
    /// </summary>
    [CreateAssetMenu(fileName = "SelfSabotage_ElementLock",
        menuName = "Tower Defense/Cards/Sabotages/Self/Element Lock")]
    public class ElementLockSelfSabotage : SabotageEffectBase
    {
        [Min(0)]
        public int bonusGoldAtWaveEnd = 150;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var build = target?.GetComponent<BuildManager>();
            build?.SetBuildingDisabled(true);
            LogSabotage(target, caster, $"Element Lock: building disabled, +{bonusGoldAtWaveEnd} on wave end");
        }

        public override void Remove(PhotonView target, PhotonView caster)
        {
            var build = target?.GetComponent<BuildManager>();
            build?.SetBuildingDisabled(false);
            GetPlayerGold(target)?.AddGold(bonusGoldAtWaveEnd);
        }
    }
}
