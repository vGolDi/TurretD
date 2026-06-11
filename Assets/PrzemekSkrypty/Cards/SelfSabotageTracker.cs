using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using ElementumDefense.Players;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Tracks active self-sabotages on the local player.
    /// Listens for wave completions and awards rewards when conditions are met.
    /// 
    /// Lives on the Player object (same as PlayerCardManager).
    /// </summary>
    public class SelfSabotageTracker : MonoBehaviour
    {
        public static SelfSabotageTracker Instance { get; private set; }

        [Header("Debug")]
        [SerializeField] private bool logRewards = true;

        // Active self-sabotage challenges
        private List<ActiveChallenge> activeChallenges = new List<ActiveChallenge>();

        // Events
        public System.Action<SabotageCardData, int, int> OnRewardEarned;  // sabotage, gold, crystals
        public System.Action<SabotageCardData> OnChallengeFailed;
        public System.Action<SabotageCardData> OnChallengeStarted;

        // ==========================================
        // LIFECYCLE
        // ==========================================

        private void Awake()
        {
            var pv = GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
                Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ==========================================
        // CHALLENGE MANAGEMENT
        // ==========================================

        /// <summary>
        /// Start a self-sabotage challenge. Called by SabotageDraftManager
        /// when player picks a Self-type sabotage.
        /// </summary>
        public void StartChallenge(SabotageCardData sabotageData)
        {
            if (sabotageData == null || !sabotageData.IsSelfSabotage)
            {
                Debug.LogError("[SelfSabotageTracker] Tried to start non-self sabotage!");
                return;
            }

            var challenge = new ActiveChallenge
            {
                sabotageData = sabotageData,
                wavesRemaining = Mathf.Max(1, sabotageData.challengeWaves),
                totalWaves = Mathf.Max(1, sabotageData.challengeWaves),
                isActive = true
            };

            activeChallenges.Add(challenge);
            OnChallengeStarted?.Invoke(sabotageData);

            // Apply the self-sabotage effect to ourselves
            ApplySelfEffect(sabotageData);

            if (logRewards)
                Debug.Log($"[SelfSabotage] Challenge started: {sabotageData.sabotageName} " +
                          $"({challenge.wavesRemaining} waves)");
        }

        /// <summary>
        /// Called by WaveManager when a wave is completed (all enemies dead/escaped).
        /// </summary>
        public void OnWaveCompleted(bool survived)
        {
            if (activeChallenges.Count == 0) return;

            for (int i = activeChallenges.Count - 1; i >= 0; i--)
            {
                var challenge = activeChallenges[i];
                if (!challenge.isActive) continue;

                challenge.wavesRemaining--;

                if (!survived && challenge.sabotageData.rewardOnSurvive)
                {
                    // Failed — player lost HP / base destroyed during challenge
                    challenge.isActive = false;
                    activeChallenges.RemoveAt(i);
                    OnChallengeFailed?.Invoke(challenge.sabotageData);

                    if (logRewards)
                        Debug.Log($"[SelfSabotage] FAILED: {challenge.sabotageData.sabotageName}");
                    continue;
                }

                if (challenge.wavesRemaining <= 0)
                {
                    // Completed — give reward!
                    GiveReward(challenge);
                    activeChallenges.RemoveAt(i);
                }
                else
                {
                    if (logRewards)
                        Debug.Log($"[SelfSabotage] {challenge.sabotageData.sabotageName}: " +
                                  $"{challenge.wavesRemaining} waves left");
                }
            }
        }

        // ==========================================
        // REWARDS
        // ==========================================

        private void GiveReward(ActiveChallenge challenge)
        {
            var data = challenge.sabotageData;
            int goldReward = data.rewardGold;

            // Add gold (in-match)
            if (goldReward > 0)
            {
                PlayerGold playerGold = GetComponent<PlayerGold>();
                if (playerGold != null)
                    playerGold.AddGold(goldReward);
            }

            OnRewardEarned?.Invoke(data, goldReward, 0);

            if (logRewards)
                Debug.Log($"[SelfSabotage] REWARD! {data.sabotageName}: +{goldReward} gold");
        }

        // ==========================================
        // APPLY SELF EFFECTS
        // ==========================================

        private void ApplySelfEffect(SabotageCardData data)
        {
            if (data.sabotageEffect == null)
            {
                Debug.LogWarning($"[SelfSabotage] {data.sabotageName} has no effect assigned!");
                return;
            }

            PhotonView myView = GetComponent<PhotonView>();
            if (myView == null) return;

            // Self-sabotage applies to OURSELVES
            // target = self, caster = self
            data.sabotageEffect.Apply(myView, myView);

            // Apply gold multiplier to wave if set
            if (data.rewardGoldMultiplier > 1f)
            {
                WaveManager wm = FindMyWaveManager(myView);
                if (wm != null)
                {
                    wm.ApplyWaveModifiers(mod =>
                    {
                        mod.goldRewardMultiplier *= data.rewardGoldMultiplier;
                    });
                }
            }
        }

        private WaveManager FindMyWaveManager(PhotonView playerView)
        {
            ArenaOwner[] arenas = FindObjectsByType<ArenaOwner>(FindObjectsSortMode.None);
            foreach (var arena in arenas)
            {
                if (arena.ownerPhotonView == playerView)
                {
                    return arena.GetComponentInChildren<WaveManager>();
                }
            }
            return null;
        }

        // ==========================================
        // QUERIES
        // ==========================================

        public bool HasActiveChallenges => activeChallenges.Count > 0;

        public List<ActiveChallenge> GetActiveChallenges()
        {
            return new List<ActiveChallenge>(activeChallenges);
        }

        public void ClearAll()
        {
            activeChallenges.Clear();
        }

        /// <summary>
        /// Reconnect restore: re-register a self-sabotage challenge with the
        /// remaining wave count from the snapshot, re-applying its self effect.
        /// </summary>
        public void RestoreChallenge(SabotageCardData sabotageData, int wavesRemaining, int totalWaves)
        {
            if (sabotageData == null || !sabotageData.IsSelfSabotage)
            {
                Debug.LogWarning("[SelfSabotageTracker] RestoreChallenge skipped — invalid sabotage.");
                return;
            }

            var challenge = new ActiveChallenge
            {
                sabotageData = sabotageData,
                wavesRemaining = Mathf.Max(0, wavesRemaining),
                totalWaves = Mathf.Max(1, totalWaves),
                isActive = true
            };
            activeChallenges.Add(challenge);

            // Re-apply the self effect so modifiers match pre-disconnect state —
            // UNLESS the effect has a one-time side effect already baked into the
            // snapshot (e.g. AllIn's gold sacrifice). The challenge is still
            // registered above so the survival reward pays out.
            if (sabotageData.sabotageEffect != null && sabotageData.sabotageEffect.ReapplyOnRestore)
                ApplySelfEffect(sabotageData);
            else
                Debug.Log($"[SelfSabotage] {sabotageData.sabotageName}: skipping Apply on restore " +
                          "(one-time effect already in snapshot).");

            if (logRewards)
                Debug.Log($"[SelfSabotage] Restored challenge: {sabotageData.sabotageName} " +
                          $"({challenge.wavesRemaining}/{challenge.totalWaves} waves)");
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Print Active Challenges")]
        private void DebugPrint()
        {
            Debug.Log($"=== ACTIVE SELF-SABOTAGES ({activeChallenges.Count}) ===");
            foreach (var c in activeChallenges)
            {
                Debug.Log($"  - {c.sabotageData.sabotageName}: " +
                          $"{c.wavesRemaining}/{c.totalWaves} waves, " +
                          $"reward: {c.sabotageData.rewardGold}g");
            }
        }

        // ==========================================
        // DATA CLASS
        // ==========================================

        [System.Serializable]
        public class ActiveChallenge
        {
            public SabotageCardData sabotageData;
            public int wavesRemaining;
            public int totalWaves;
            public bool isActive;
        }
    }
}
