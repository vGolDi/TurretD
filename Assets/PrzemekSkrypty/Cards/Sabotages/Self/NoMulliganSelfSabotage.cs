using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// SELF: At the next mid-game draft, you cannot mulligan ANY card. In
    /// return, the draft offers extra choices to compensate.
    /// 
    /// Mechanics:
    ///  - <see cref="extraChoices"/> bumps the draft's choice count above the
    ///    default (3 + extra). Goes through DraftManager.SetNextDraftChoiceOverride.
    ///  - Mulligan disable goes through DraftManager.SetNextDraftMulliganDisabled —
    ///    the next mid-game draft will have its mulligan UI gated off, regardless
    ///    of how many cards the player draws.
    /// 
    /// Both flags are one-shot — consumed when the next mid-game draft starts.
    /// Apply at run-start, this stays armed until the draft fires.
    /// </summary>
    [CreateAssetMenu(fileName = "SelfSabotage_NoMulligan",
        menuName = "Tower Defense/Cards/Sabotages/Self/No Mulligan")]
    public class NoMulliganSelfSabotage : SabotageEffectBase
    {
        [Min(0), Tooltip("How many extra cards beyond the default 3 the draft offers")]
        public int extraChoices = 1;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var pcm = target?.GetComponent<PlayerCardManager>();
            if (pcm == null) return;

            int totalChoices = 3 + Mathf.Max(0, extraChoices);
            pcm.SetNextDraftChoiceCount(totalChoices);
            pcm.SetNextDraftMulliganDisabled(true);

            LogSabotage(target, caster,
                $"No Mulligan: next draft has {totalChoices} choices, mulligan disabled");
        }
        // No Remove — both flags are consumed by DraftManager when the next
        // mid-game draft starts. Sabotage SO duration should be Permanent (or
        // long enough to cover until the next draft).
    }
}
