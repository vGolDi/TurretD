using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    [CreateAssetMenu(fileName = "Sabotage_DisableUpgrades",
        menuName = "Tower Defense/Cards/Sabotages/Opponent/Disable Upgrades")]
    public class DisableUpgradesSabotage : SabotageEffectBase
    {
        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            // ========== NAPRAWIONE: Faktyczna implementacja ==========
            PlayerCardManager cardManager =
                targetPhotonView.GetComponent<PlayerCardManager>();

            if (cardManager != null)
            {
                cardManager.SetUpgradesDisabled(true);
                LogSabotage(targetPhotonView, casterPhotonView,
                    "🚫 Upgrades DISABLED");
            }
            // ========================================================
        }

        public override void Remove(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            PlayerCardManager cardManager =
                targetPhotonView.GetComponent<PlayerCardManager>();

            if (cardManager != null)
            {
                cardManager.SetUpgradesDisabled(false);
                LogSabotage(targetPhotonView, casterPhotonView,
                    "✅ Upgrades RESTORED");
            }
        }

        public override string GetEffectDescription()
        {
            return "🚫 Opponent cannot upgrade turrets";
        }
    }
}