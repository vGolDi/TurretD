using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ElementumDefense.Players;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Tracks active sabotage effects on the local player and forwards
    /// duration / round counters. The actual modifier math lives in
    /// <see cref="PlayerModifierStack"/> — this controller just orchestrates
    /// apply / remove and DOT updates.
    /// </summary>
    [RequireComponent(typeof(PlayerModifierStack))]
    public class PlayerSabotageController : MonoBehaviour
    {
        [Header("Active Sabotages")]
        [SerializeField] private List<ActiveSabotage> activeSabotages = new List<ActiveSabotage>();

        // Passive gold accumulator (lives here because PassiveGold is driven by
        // CARDS not sabotage — but we keep it close to the per-frame loop
        // that already exists for sabotage updates).
        private float passiveGoldTimer = 0f;
        private const float PASSIVE_GOLD_INTERVAL = 1f;

        // Cached siblings
        private PhotonView photonView;
        private PlayerModifierStack modifierStack;
        private PlayerGold playerGold;

        // ==========================================
        // LIFECYCLE
        // ==========================================

        private void Awake()
        {
            photonView = GetComponent<PhotonView>();
            modifierStack = GetComponent<PlayerModifierStack>();
            playerGold = GetComponent<PlayerGold>();
        }

        private void Update()
        {
            if (photonView == null || !photonView.IsMine) return;

            // Passive gold (from cards, scaled by any sabotage multipliers).
            if (modifierStack.PassiveGoldPerSecond > 0)
            {
                passiveGoldTimer += Time.deltaTime;
                if (passiveGoldTimer >= PASSIVE_GOLD_INTERVAL)
                {
                    int goldThisTick = Mathf.RoundToInt(modifierStack.EffectivePassiveGoldPerSecond);
                    if (goldThisTick > 0)
                        playerGold?.AddGold(goldThisTick);
                    passiveGoldTimer = 0f;
                }
            }

            UpdateSabotages(Time.deltaTime);
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        /// <summary>
        /// Applies a sabotage to this player. Called by SabotageDraftManager
        /// once the reveal animation finishes.
        /// </summary>
        public void ApplySabotage(SabotageCardData sabotage, PhotonView casterPhotonView)
        {
            if (sabotage == null)
            {
                Debug.LogError("[PlayerSabotageController] Sabotage is null!");
                return;
            }

            if (sabotage.sabotageEffect == null)
            {
                Debug.LogError($"[PlayerSabotageController] Sabotage '{sabotage.sabotageName}' has NO EFFECT assigned!");
                return;
            }

            sabotage.sabotageEffect.Apply(photonView, casterPhotonView);

            if (sabotage.durationType != SabotageDurationType.Instant)
            {
                activeSabotages.Add(new ActiveSabotage
                {
                    sabotageData = sabotage,
                    casterPhotonView = casterPhotonView,
                    remainingDuration = sabotage.duration,
                    remainingRounds = sabotage.durationRounds
                });
            }

            string casterName = casterPhotonView?.Owner?.NickName ?? "Unknown";
            Debug.Log($"[PlayerSabotageController] Sabotage applied: '{sabotage.sabotageName}' " +
                      $"from {casterName} ({sabotage.durationType}, {sabotage.GetDurationText()})");
        }

        /// <summary>Called by WaveManager after each wave for round-based countdowns.</summary>
        public void OnWaveCompleted()
        {
            for (int i = activeSabotages.Count - 1; i >= 0; i--)
            {
                ActiveSabotage sabotage = activeSabotages[i];
                if (sabotage.sabotageData.durationRounds <= 0) continue;

                sabotage.remainingRounds--;
                if (sabotage.remainingRounds <= 0)
                {
                    sabotage.sabotageData.sabotageEffect?.Remove(photonView, sabotage.casterPhotonView);
                    activeSabotages.RemoveAt(i);
                    Debug.Log($"[PlayerSabotageController] Round sabotage expired: " +
                              $"{sabotage.sabotageData.sabotageName}");
                }
            }
        }

        /// <summary>
        /// Wipe ALL sabotages and reset modifier state. Used by Cleanse cards
        /// or end-of-game cleanup.
        /// </summary>
        public void ClearAllSabotages()
        {
            foreach (var sabotage in activeSabotages)
                sabotage.sabotageData?.sabotageEffect?.Remove(photonView, sabotage.casterPhotonView);

            activeSabotages.Clear();
            modifierStack.ResetSabotageModifiers();

            Debug.Log("[PlayerSabotageController] Cleared all sabotages + modifiers");
        }

        public List<ActiveSabotage> GetActiveSabotages() => new List<ActiveSabotage>(activeSabotages);

        /// <summary>
        /// Reconnect restore: re-apply a sabotage effect and register it with the
        /// REMAINING duration/rounds from the snapshot (not the full duration).
        /// Idempotent w.r.t. modifier stack because the stack is rebuilt fresh
        /// during restore. Caster falls back to the local view if the original
        /// caster is no longer resolvable.
        /// </summary>
        public void RestoreSabotage(SabotageCardData sabotage, PhotonView caster,
                                    float remainingDuration, int remainingRounds)
        {
            if (sabotage == null || sabotage.sabotageEffect == null)
            {
                Debug.LogWarning("[PlayerSabotageController] RestoreSabotage skipped — null sabotage/effect.");
                return;
            }

            PhotonView effectiveCaster = caster != null ? caster : photonView;

            // Honor the effect's restore hint: skip Apply for one-time effects
            // whose result is already in the snapshot (would otherwise double-apply).
            if (sabotage.sabotageEffect.ReapplyOnRestore)
                sabotage.sabotageEffect.Apply(photonView, effectiveCaster);
            else
                Debug.Log($"[PlayerSabotageController] '{sabotage.sabotageName}': skipping Apply on " +
                          "restore (one-time effect already in snapshot).");

            if (sabotage.durationType != SabotageDurationType.Instant)
            {
                activeSabotages.Add(new ActiveSabotage
                {
                    sabotageData = sabotage,
                    casterPhotonView = effectiveCaster,
                    remainingDuration = remainingDuration,
                    remainingRounds = remainingRounds
                });
            }

            Debug.Log($"[PlayerSabotageController] Restored sabotage '{sabotage.sabotageName}' " +
                      $"(remDur={remainingDuration:F1}, remRounds={remainingRounds})");
        }

        // ==========================================
        // INTERNAL — DURATION / DOT TICKS
        // ==========================================

        private void UpdateSabotages(float deltaTime)
        {
            for (int i = activeSabotages.Count - 1; i >= 0; i--)
            {
                ActiveSabotage sabotage = activeSabotages[i];

                // Permanent sabotages just keep ticking their OnUpdate (e.g. DOT).
                if (sabotage.sabotageData.durationType == SabotageDurationType.Permanent)
                {
                    sabotage.sabotageData.sabotageEffect?.OnUpdate(photonView, deltaTime);
                    continue;
                }

                sabotage.remainingDuration -= deltaTime;
                sabotage.sabotageData.sabotageEffect?.OnUpdate(photonView, deltaTime);

                if (sabotage.remainingDuration <= 0f)
                {
                    sabotage.sabotageData.sabotageEffect?.Remove(photonView, sabotage.casterPhotonView);
                    string name = sabotage.sabotageData.sabotageName;
                    activeSabotages.RemoveAt(i);
                    Debug.Log($"[PlayerSabotageController] Sabotage expired: {name}");
                }
            }
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Print Active Sabotages")]
        private void PrintActiveSabotages()
        {
            Debug.Log($"=== ACTIVE SABOTAGES ({activeSabotages.Count}) ===");
            if (activeSabotages.Count == 0)
            {
                Debug.Log("  (none)");
                return;
            }

            foreach (var s in activeSabotages)
            {
                string caster = s.casterPhotonView?.Owner?.NickName ?? "Unknown";
                string duration = s.sabotageData.durationType == SabotageDurationType.Permanent
                    ? "PERMANENT"
                    : s.sabotageData.durationRounds > 0
                        ? $"{s.remainingRounds} rounds left"
                        : $"{s.remainingDuration:F1}s left";

                string hasEffect = s.sabotageData.sabotageEffect != null ? "OK" : "NO EFFECT";
                Debug.Log($"  - {s.sabotageData.sabotageName} from {caster} ({duration}) [{hasEffect}]");
            }
        }

        [ContextMenu("Force Clear All Sabotages")]
        private void ForceClearSabotages() => ClearAllSabotages();
    }
}
