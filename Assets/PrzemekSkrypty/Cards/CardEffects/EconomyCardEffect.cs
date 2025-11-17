using ElementumDefense.Cards;
using Photon.Pun;
using UnityEngine;

[CreateAssetMenu(fileName = "EconomyCard_Effect", menuName = "Tower Defense/Cards/Effects/Economy Boost")]
public class EconomyCardEffect : CardEffectBase
{
    [Header("Economy Settings")]
    [Tooltip("Gold per second (Continuous effect)")]
    public int goldPerSecond = 5;

    [Tooltip("One-time gold bonus (OnDraft effect)")]
    public int instantGoldBonus = 0;

    [Tooltip("Turret cost discount % (Continuous effect)")]
    [Range(0f, 100f)]
    public float turretCostDiscount = 0f;

    public override void Activate(PhotonView ownerPhotonView)
    {
        PlayerGold playerGold = GetPlayerGold(ownerPhotonView);
        if (playerGold == null) return;

        // ⚡ INSTANT EFFECT (OnDraft):
        if (instantGoldBonus > 0)
        {
            playerGold.AddGold(instantGoldBonus);
            LogActivation(ownerPhotonView, $"+{instantGoldBonus} instant gold");
        }

        // 🔄 CONTINUOUS EFFECTS:
        // These are registered with managers and stay active
        if (goldPerSecond > 0)
        {
            // TODO: PassiveIncomeManager.Instance.AddIncome(ownerPhotonView, goldPerSecond);
            LogActivation(ownerPhotonView, $"+{goldPerSecond} gold/s (continuous)");
        }

        if (turretCostDiscount > 0)
        {
            // TODO: TurretCostManager.Instance.AddDiscount(ownerPhotonView, turretCostDiscount);
            LogActivation(ownerPhotonView, $"-{turretCostDiscount}% turret costs (continuous)");
        }
    }

    public override void Deactivate(PhotonView ownerPhotonView)
    {
        // Called when game ends - cleanup continuous effects
        if (goldPerSecond > 0)
        {
            // TODO: PassiveIncomeManager.Instance.RemoveIncome(ownerPhotonView, goldPerSecond);
        }

        if (turretCostDiscount > 0)
        {
            // TODO: TurretCostManager.Instance.RemoveDiscount(ownerPhotonView, turretCostDiscount);
        }
    }
}