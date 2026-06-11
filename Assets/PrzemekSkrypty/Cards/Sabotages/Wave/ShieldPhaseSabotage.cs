using UnityEngine;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Every spawned enemy in the next wave gets armor stacks (clickable shield).
    /// Requires every enemy prefab to have an EnemyArmor component (default
    /// armorStacks=0). The sabotage sets armorStacks via EnemyArmor.ApplyFromSabotage.
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_ShieldPhase",
        menuName = "Tower Defense/Cards/Sabotages/Wave/Shield Phase")]
    public class ShieldPhaseSabotage : SabotageEffectBase
    {
        [Min(1), Tooltip("Armor stacks each enemy gets (clicks needed to break)")]
        public int armorStacks = 1;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var wm = GetWaveManager(target);
            wm?.ApplyWaveModifiers(m =>
            {
                m.forceArmorOnSpawn = true;
                m.forceArmorStacks = Mathf.Max(m.forceArmorStacks, armorStacks);
            });
            LogSabotage(target, caster, $"Shield Phase: every enemy gets {armorStacks} armor stacks");
        }
    }
}
