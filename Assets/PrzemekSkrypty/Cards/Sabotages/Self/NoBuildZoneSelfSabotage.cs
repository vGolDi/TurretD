using UnityEngine;
using Photon.Pun;
using ElementumDefense.Players;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// SELF-SABOTAGE: No Build Zone
    /// Disables building AND upgrading turrets for the next wave.
    /// Reward: massive gold multiplier if you survive with what you have.
    /// </summary>
    [CreateAssetMenu(fileName = "SelfSabo_NoBuildZone",
        menuName = "Tower Defense/Cards/Sabotages/Self/No Build Zone")]
    public class NoBuildZoneSelfSabotage : SabotageEffectBase
    {
        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            // Disable building
            BuildManager buildManager = GetBuildManager(targetPhotonView);
            if (buildManager != null)
            {
                buildManager.SetBuildingDisabled(true);
            }

            // Disable upgrades
            PlayerCardManager cardManager =
                targetPhotonView.GetComponent<PlayerCardManager>();
            if (cardManager != null)
            {
                cardManager.SetUpgradesDisabled(true);
            }

            // Also set wave modifier flag
            WaveManager wm = GetWaveManager(targetPhotonView);
            if (wm != null)
            {
                wm.ApplyWaveModifiers(mod =>
                {
                    mod.disableBuilding = true;
                });
            }

            LogSabotage(targetPhotonView, casterPhotonView,
                "SELF-SABOTAGE: No Build Zone! Cannot build or upgrade this wave!");
        }

        public override void Remove(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            // Re-enable building
            BuildManager buildManager = GetBuildManager(targetPhotonView);
            if (buildManager != null)
            {
                buildManager.SetBuildingDisabled(false);
            }

            // Re-enable upgrades
            PlayerCardManager cardManager =
                targetPhotonView.GetComponent<PlayerCardManager>();
            if (cardManager != null)
            {
                cardManager.SetUpgradesDisabled(false);
            }

            LogSabotage(targetPhotonView, casterPhotonView,
                "No Build Zone ended — building restored!");
        }

        public override string GetEffectDescription()
        {
            return "Cannot build or upgrade turrets for the next wave.\n" +
                   "Survive with what you have for massive gold bonus!";
        }
    }
}
