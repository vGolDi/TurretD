using UnityEngine;
using Photon.Pun;
using ElementumDefense.Players;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Card that modifies player stats
    /// Example: "Tank Build" +50 HP but -10% move speed
    /// </summary>
    [CreateAssetMenu(fileName = "UtilityCard_Effect", menuName = "Tower Defense/Cards/Effects/Utility/Stat Modifier")]
    public class UtilityCardEffect : CardEffectBase
    {
        [Header("Health Modifiers")]
        public int maxHealthBonus = 0;
        public int instantHealAmount = 0;

        [Header("Speed Modifiers")]
        [Range(-50f, 100f)]
        public float moveSpeedPercent = 0f; // -50% to +100%


        public override void Activate(PhotonView ownerPhotonView)
        {
            // Health modifier
            if (maxHealthBonus != 0 || instantHealAmount != 0)
            {
                PlayerHealth playerHealth = GetPlayerHealth(ownerPhotonView);

                if (playerHealth != null)
                {
                    // TODO: Implement max health modifier system
                    // playerHealth.AddMaxHealth(maxHealthBonus);

                    if (instantHealAmount > 0)
                    {
                        playerHealth.Heal(instantHealAmount);
                    }

                    LogActivation(ownerPhotonView, $"HP: +{maxHealthBonus} max, +{instantHealAmount} heal");
                }
            }

            // Speed modifier
            if (moveSpeedPercent != 0f)
            {
                // TODO: Implement player speed modifier system
                LogActivation(ownerPhotonView, $"Move speed: {moveSpeedPercent:+0;-0}%");
            }


        }

        public override string GetEffectDescription()
        {
            string desc = "";

            if (maxHealthBonus > 0)
                desc += $"❤ +{maxHealthBonus} max HP\n";
            else if (maxHealthBonus < 0)
                desc += $"💔 {maxHealthBonus} max HP\n";

            if (instantHealAmount > 0)
                desc += $"💚 Heal {instantHealAmount} HP\n";

            if (moveSpeedPercent > 0)
                desc += $"⚡ +{moveSpeedPercent}% move speed\n";
            else if (moveSpeedPercent < 0)
                desc += $"🐌 {moveSpeedPercent}% move speed\n";

            return desc.TrimEnd('\n');
        }
    }
}