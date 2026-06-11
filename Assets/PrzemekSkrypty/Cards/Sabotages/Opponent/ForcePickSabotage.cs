using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Marks the target so on the NEXT mid-game draft they get 2 cards instead
    /// of 3 (fewer choices = forced pick from a smaller pool).
    /// 
    /// Implementation note: this is a flag stored on PlayerCardManager. The
    /// DraftManager checks the flag at draft time and reduces midGameChoices
    /// for that draft only. Flag clears itself after consumption.
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_ForcePick",
        menuName = "Tower Defense/Cards/Sabotages/Opponent/Force Pick")]
    public class ForcePickSabotage : SabotageEffectBase
    {
        [Min(1), Tooltip("How many choices the target will see at the next mid-game draft (vs default 3)")]
        public int reducedChoices = 2;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var pcm = target?.GetComponent<PlayerCardManager>();
            if (pcm == null) return;
            pcm.SetNextDraftChoiceCount(reducedChoices);
            LogSabotage(target, caster, $"Next draft: only {reducedChoices} choices");
        }
    }
}
