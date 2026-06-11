using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Inflates the cost of building / upgrading turrets. Stacks with other
    /// cost modifiers via the ID-based modifier stack on PlayerModifierStack.
    /// Use Time-based or 1-round duration on the SO.
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_TowerTax",
        menuName = "Tower Defense/Cards/Sabotages/Opponent/Tower Tax")]
    public class TowerTaxSabotage : SabotageEffectBase
    {
        [Header("Tower Tax")]
        [Range(1f, 3f), Tooltip("Cost multiplier (1.3 = +30%)")]
        public float costMultiplier = 1.3f;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var stack = target?.GetComponent<PlayerModifierStack>();
            if (stack == null) return;
            stack.ApplyById(MakeId(target, caster), PlayerModifierStack.SabotageStat.Cost, costMultiplier);
            LogSabotage(target, caster, $"Tower Tax: x{costMultiplier:F2} cost");
        }

        public override void Remove(PhotonView target, PhotonView caster)
        {
            var stack = target?.GetComponent<PlayerModifierStack>();
            stack?.RemoveById(MakeId(target, caster), PlayerModifierStack.SabotageStat.Cost);
        }

        private string MakeId(PhotonView t, PhotonView c) => $"TowerTax_{t.ViewID}_{(c != null ? c.ViewID : 0)}";
    }
}
