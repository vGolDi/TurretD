using UnityEngine;
using Photon.Pun;
using ElementumDefense.Players;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// SELF-SABOTAGE: All-In
    /// Instantly lose 50% of your gold. If you survive the next wave,
    /// get 200% of the lost gold back (net +100% profit).
    /// If you fail... gold is gone forever.
    /// </summary>
    [CreateAssetMenu(fileName = "SelfSabo_AllIn",
        menuName = "Tower Defense/Cards/Sabotages/Self/All-In")]
    public class AllInSelfSabotage : SabotageEffectBase
    {
        [Header("All-In Settings")]
        [Tooltip("Percentage of gold to sacrifice (0.5 = 50%)")]
        [Range(0.1f, 0.9f)]
        public float sacrificePercent = 0.5f;

        [Tooltip("Multiplier on sacrificed gold if you survive (2.0 = get back 200%)")]
        public float returnMultiplier = 2f;

        [Tooltip("Minimum gold to sacrifice (prevents worthless gambles)")]
        public int minimumSacrifice = 100;

        // Runtime — stored amount for reward calculation
        // (SelfSabotageTracker reads rewardGold from SabotageCardData,
        //  but we override it dynamically here)
        private int lastSacrificedAmount = 0;

        // The gold sacrifice is a one-time, already-applied mutation. On reconnect
        // the snapshot already holds the post-sacrifice gold, so do NOT re-run
        // Apply (it would drain a second time). The challenge is still re-registered
        // by SelfSabotageTracker so the survival reward still pays out.
        public override bool ReapplyOnRestore => false;

        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            PlayerGold playerGold = GetPlayerGold(targetPhotonView);
            if (playerGold == null)
            {
                Debug.LogError("[AllIn] PlayerGold not found!");
                return;
            }

            int currentGold = playerGold.GetGold();

            if (currentGold < minimumSacrifice)
            {
                Debug.LogWarning($"[AllIn] Not enough gold to gamble! " +
                                 $"Has {currentGold}, needs {minimumSacrifice}");
                // Still take the effect but sacrifice everything
                lastSacrificedAmount = currentGold;
            }
            else
            {
                lastSacrificedAmount = Mathf.RoundToInt(currentGold * sacrificePercent);
            }

            // Take the gold NOW
            playerGold.SpendGold(lastSacrificedAmount);

            // Store the reward amount dynamically
            // SelfSabotageTracker will read rewardGold from the SabotageCardData,
            // but we also log the dynamic amount
            int potentialReturn = Mathf.RoundToInt(lastSacrificedAmount * returnMultiplier);

            LogSabotage(targetPhotonView, casterPhotonView,
                $"ALL-IN! Sacrificed {lastSacrificedAmount} gold. " +
                $"Survive for {potentialReturn} gold back!");
        }

        public override string GetEffectDescription()
        {
            return $"Sacrifice {sacrificePercent * 100f}% of your gold NOW.\n" +
                   $"Survive the next wave = get {returnMultiplier}x back!\n" +
                   $"Fail = gold gone forever.";
        }
    }
}
