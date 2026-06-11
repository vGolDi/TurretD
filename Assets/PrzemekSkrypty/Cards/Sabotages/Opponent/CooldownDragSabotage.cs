using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Lowers fire rate of all turrets for 1 wave (or set duration).
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_CooldownDrag",
        menuName = "Tower Defense/Cards/Sabotages/Opponent/Cooldown Drag")]
    public class CooldownDragSabotage : SabotageEffectBase
    {
        [Header("Fire Rate Drop")]
        [Range(0.5f, 1f), Tooltip("Fire rate multiplier (0.9 = -10%)")]
        public float fireRateMultiplier = 0.9f;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var stack = target?.GetComponent<PlayerModifierStack>();
            if (stack == null) return;
            stack.ApplyById(MakeId(target, caster), PlayerModifierStack.SabotageStat.FireRate, fireRateMultiplier);
            LogSabotage(target, caster, $"Cooldown Drag: x{fireRateMultiplier:F2} FR");
        }

        public override void Remove(PhotonView target, PhotonView caster)
        {
            var stack = target?.GetComponent<PlayerModifierStack>();
            stack?.RemoveById(MakeId(target, caster), PlayerModifierStack.SabotageStat.FireRate);
        }

        private string MakeId(PhotonView t, PhotonView c) => $"CooldownDrag_{t.ViewID}_{(c != null ? c.ViewID : 0)}";
    }
}
