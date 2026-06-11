using UnityEngine;
using Photon.Pun;
using ElementumDefense.Players;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// SELF: Drops your base HP to 1 but multiplies all turret damage. One leak
    /// = loss. Massive risk for massive damage.
    /// 
    /// Implementation note: PlayerHealth.SetMaxHP / SetCurrentHP not yet exposed
    /// — uses TakeDamage(currentHP - 1) as a workaround to drop HP. Restored
    /// on Remove via Heal(currentMaxHP).
    /// </summary>
    [CreateAssetMenu(fileName = "SelfSabotage_GlassCannon",
        menuName = "Tower Defense/Cards/Sabotages/Self/Glass Cannon")]
    public class GlassCannonSelfSabotage : SabotageEffectBase
    {
        [Range(1.5f, 5f), Tooltip("Damage multiplier on all turrets")]
        public float damageMultiplier = 2f;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            // 1. Damage boost via ID-based modifier
            var stack = target?.GetComponent<PlayerModifierStack>();
            stack?.ApplyById(MakeId(target, caster), PlayerModifierStack.SabotageStat.Damage, damageMultiplier);

            // 2. Drop HP to 1
            var ph = GetPlayerHealth(target);
            if (ph != null)
            {
                int current = ph.CurrentHealth;
                if (current > 1) ph.TakeDamage(current - 1);
            }

            LogSabotage(target, caster, $"Glass Cannon: HP=1, dmg x{damageMultiplier}");
        }

        public override void Remove(PhotonView target, PhotonView caster)
        {
            var stack = target?.GetComponent<PlayerModifierStack>();
            stack?.RemoveById(MakeId(target, caster), PlayerModifierStack.SabotageStat.Damage);
            // HP is NOT restored — staying alive at 1 HP IS the cost.
        }

        private string MakeId(PhotonView t, PhotonView c) => $"GlassCannon_{t.ViewID}_{(c != null ? c.ViewID : 0)}";
    }
}
