using UnityEngine;
using Photon.Pun;
using ElementumDefense.Players;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Card that fires a payout (gold, etc.) every N waves.
    /// 
    /// Triggered by <see cref="PlayerWaveTriggerListener"/> sibling component
    /// on the player. The listener subscribes to wave start/end events and
    /// walks every active card looking for WaveTriggerEffect instances.
    /// 
    /// Continuous effect — payout repeats for the rest of the game.
    /// </summary>
    [CreateAssetMenu(fileName = "WaveTriggerCard_Effect", menuName = "Tower Defense/Cards/Effects/Wave Trigger")]
    public class WaveTriggerEffect : CardEffectBase
    {
        [Header("Trigger")]
        [Tooltip("Fire payout every N waves. 5 = waves 5, 10, 15...")]
        [Min(1)]
        public int everyNWaves = 5;

        [Tooltip("Should it also fire on the very first wave (wave index 1)? " +
                 "Default false to avoid free start-of-game gold.")]
        public bool fireOnFirstWave = false;

        [Header("Payout")]
        [Tooltip("Gold awarded on trigger.")]
        public int goldReward = 0;

        // Reserved for future tradeoff (e.g. -10% damage during the trigger wave).

        public override void Activate(PhotonView ownerPhotonView)
        {
            LogActivation(ownerPhotonView, $"every {everyNWaves} waves -> {goldReward}g");
        }

        /// <summary>
        /// Called by PlayerWaveTriggerListener when a wave begins. Returns true
        /// when the trigger fired so the listener can show feedback.
        /// </summary>
        public bool OnWaveStarted(int waveIndex1Based, PhotonView ownerPhotonView)
        {
            if (waveIndex1Based < 1) return false;
            if (waveIndex1Based == 1 && !fireOnFirstWave) return false;
            if (waveIndex1Based % everyNWaves != 0) return false;

            // Award gold to local player only (this is per-player passive income).
            PlayerGold gold = GetPlayerGold(ownerPhotonView);
            if (gold != null && goldReward > 0)
                gold.AddGold(goldReward);

            return true;
        }

        public override string GetEffectDescription()
        {
            string desc = $"🔄 Every {everyNWaves} waves:";
            if (goldReward > 0) desc += $" +{goldReward}g";
            return desc;
        }
    }
}
