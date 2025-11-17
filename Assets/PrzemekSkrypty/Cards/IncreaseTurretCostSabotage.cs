using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Increases turret build costs permanently
    /// Tag: Economy, Duration: Permanent
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_IncreaseCosts", menuName = "Tower Defense/Cards/Sabotage/Increase Costs")]
    public class IncreaseTurretCostSabotage : SabotageEffectBase
    {
        [Header("Cost Modifier")]
        [Tooltip("Cost increase % (50 = +50% costs)")]
        [Range(10f, 100f)]
        public float costIncreasePercent = 50f;

        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            // TODO: TurretCostManager.Instance.AddCostModifier(targetPhotonView, 1f + costIncreasePercent/100f);
            LogSabotage(targetPhotonView, casterPhotonView, $"Turret costs +{costIncreasePercent}% (PERMANENT)");
        }

        public override void Remove(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            // Permanent - no removal
        }

        public override string GetEffectDescription()
        {
            return $"💸 Opponent's turrets cost +{costIncreasePercent}% (permanent)";
        }
    }
}