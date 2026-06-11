using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Instantly drains a percentage of the target's current gold. Instant duration.
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_BankRun",
        menuName = "Tower Defense/Cards/Sabotages/Opponent/Bank Run")]
    public class BankRunSabotage : SabotageEffectBase
    {
        [Range(0.1f, 1f), Tooltip("Fraction of gold to remove")]
        public float drainPercent = 0.5f;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var gold = GetPlayerGold(target);
            if (gold == null) return;
            int current = gold.GetGold();
            int drained = Mathf.RoundToInt(current * drainPercent);
            gold.AddGold(-drained);
            LogSabotage(target, caster, $"Bank Run: -{drained} gold ({drainPercent * 100f:F0}%)");
        }
    }
}
