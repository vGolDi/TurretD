using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    [CreateAssetMenu(fileName = "Sabotage_StealGold",
        menuName = "Tower Defense/Cards/Sabotage Effects/Steal Gold")]
    public class StealGoldSabotage : SabotageEffectBase
    {
        [Header("Steal Settings")]
        [Range(10f, 100f)]
        public float stealPercent = 30f;

        public int minimumSteal = 50;
        public int maximumSteal = 500;

        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            // ========== NAPRAWIONE: target = ofiara, caster = atakujący ==========
            // Target jest LOKALNYM graczem (bo ApplySabotageToMe jest wywoływane lokalnie)
            // Caster jest PRZECIWNIKIEM

            PlayerGold targetGold = GetPlayerGold(targetPhotonView);

            if (targetGold == null)
            {
                Debug.LogError("[StealGold] Target has no PlayerGold!");
                return;
            }

            int currentGold = targetGold.GetGold();
            int stealAmount = Mathf.RoundToInt(currentGold * (stealPercent / 100f));

            stealAmount = Mathf.Clamp(stealAmount, minimumSteal, maximumSteal);
            stealAmount = Mathf.Min(stealAmount, currentGold);

            if (stealAmount <= 0)
            {
                LogSabotage(targetPhotonView, casterPhotonView,
                    "Tried to steal gold but target is broke!");
                return;
            }

            // Take gold from target (local player)
            targetGold.SpendGold(stealAmount);

            // ========== NAPRAWIONE: Give gold to caster via RPC ==========
            // We can't directly modify caster's gold (they're remote)
            // Instead, caster gives themselves gold when they see the sabotage applied
            // OR we use the target's perspective: just remove gold from target
            //
            // For simplicity: just remove from target. 
            // The caster doesn't gain (or we'd need a separate RPC)
            // ============================================================

            LogSabotage(targetPhotonView, casterPhotonView,
                $"Stole {stealAmount} gold! (had {currentGold}, now {currentGold - stealAmount})");
        }

        public override string GetEffectDescription()
        {
            return $"💰 Lose {stealPercent}% of gold " +
                   $"(min {minimumSteal}, max {maximumSteal})";
        }
    }
}