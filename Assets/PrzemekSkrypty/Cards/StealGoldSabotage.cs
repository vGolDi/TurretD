using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Steals gold from opponent (instant)
    /// Tag: Economy, Duration: Instant
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_StealGold", menuName = "Tower Defense/Cards/Sabotage/Steal Gold")]
    public class StealGoldSabotage : SabotageEffectBase
    {
        [Header("Steal Settings")]
        [Tooltip("% of opponent's gold to steal (0-100)")]
        [Range(10f, 100f)]
        public float stealPercent = 30f;

        [Tooltip("Minimum gold to steal")]
        public int minimumSteal = 50;

        [Tooltip("Maximum gold to steal")]
        public int maximumSteal = 500;

        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            PlayerGold targetGold = GetPlayerGold(targetPhotonView);
            PlayerGold casterGold = GetPlayerGold(casterPhotonView);

            if (targetGold == null || casterGold == null) return;

            // Calculate steal amount
            int currentGold = targetGold.GetGold();
            int stealAmount = Mathf.RoundToInt(currentGold * (stealPercent / 100f));

            // Clamp
            stealAmount = Mathf.Clamp(stealAmount, minimumSteal, maximumSteal);
            stealAmount = Mathf.Min(stealAmount, currentGold); // Can't steal more than they have

            // Transfer gold
            if (targetGold.SpendGold(stealAmount))
            {
                casterGold.AddGold(stealAmount);
                LogSabotage(targetPhotonView, casterPhotonView, $"Stole {stealAmount} gold!");
            }
        }

        public override void Remove(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            // Instant - no removal
        }

        public override string GetEffectDescription()
        {
            return $"💰 Steal {stealPercent}% of opponent's gold (min {minimumSteal}, max {maximumSteal})";
        }
    }
}