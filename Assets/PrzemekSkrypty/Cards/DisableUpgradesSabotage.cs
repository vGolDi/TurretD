using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Disables turret upgrades for X seconds
    /// Tag: Turrets, Duration: Temporary
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_DisableUpgrades", menuName = "Tower Defense/Cards/Sabotage/Disable Upgrades")]
    public class DisableUpgradesSabotage : SabotageEffectBase
    {
        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            // TODO: TurretUpgradeManager.Instance.DisableUpgrades(targetPhotonView);
            LogSabotage(targetPhotonView, casterPhotonView, "Upgrades DISABLED");
        }

        public override void Remove(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            // TODO: TurretUpgradeManager.Instance.EnableUpgrades(targetPhotonView);
            LogSabotage(targetPhotonView, casterPhotonView, "Upgrades RESTORED");
        }

        public override string GetEffectDescription()
        {
            return "🚫 Opponent cannot upgrade turrets";
        }
    }
}