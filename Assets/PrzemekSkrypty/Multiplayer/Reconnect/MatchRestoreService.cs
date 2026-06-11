using System.Collections;
using UnityEngine;
using Photon.Pun;
using ElementumDefense.Players;
using ElementumDefense.Cards;
using ElementumDefense.Turrets;
using ElementumDefense.Waves;

namespace ElementumDefense.Multiplayer.Reconnect
{
    /// <summary>
    /// Restores a reconnecting player's in-match state from a verified
    /// <see cref="PlayerMatchSnapshot"/>.
    ///
    /// Restore order is deliberate:
    ///  1. gold + HP
    ///  2. re-activate cards (rebuilds the card-side modifier stack)
    ///  3. rebuild turrets (Initialize picks up card modifiers via the event)
    ///  4. re-apply sabotages (turrets now exist for turret-targeting effects)
    ///  5. restore self-sabotage challenges
    ///  6. restore draft / sabotage-draft phase flags
    ///  7. resume the wave flow from the snapshot's wave index
    ///
    /// Run via <c>StartCoroutine(MatchRestoreService.Restore(snap))</c> from a
    /// MonoBehaviour in the game scene (GameManager_MP).
    /// </summary>
    public static class MatchRestoreService
    {
        /// <summary>
        /// True between "valid snapshot detected on reconnect" and the moment the
        /// resumed wave flow starts. The normal bootstrap (PreGame → countdown →
        /// WaveManager.StartWaves) checks this and aborts its wave-0 start so the
        /// restore's <see cref="WaveManager.StartWavesFromIndex"/> takes over.
        /// </summary>
        public static bool RestorePending { get; set; }

        public static IEnumerator Restore(PlayerMatchSnapshot snap)
        {
            if (snap == null) yield break;
            Debug.Log($"[MatchRestore] Begin restore (wave={snap.currentWaveIndex}, gold={snap.currentGold}).");

            // ----- 1. Gold + HP -----
            PlayerGold.LocalInstance?.RestoreGold(snap.currentGold);
            PlayerHealth.LocalInstance?.RestoreHealth(snap.playerHP);

            PlayerCardManager cardMgr = FindLocalCardManager();

            // ----- 2. Cards (in saved order) -----
            if (cardMgr != null)
            {
                foreach (string cardName in snap.activeCardNames)
                {
                    CardData card = ResolveCard(cardName);
                    if (card != null) cardMgr.ActivateCard(card);
                    else Debug.LogWarning($"[MatchRestore] Card '{cardName}' not found in Resources/Cards (or subfolders).");
                }
            }

            // ----- 3. Turrets -----
            PlayerBuilder builder = FindLocalBuilder();
            var registry = TurretRegistry.Instance;
            if (builder != null && registry != null)
            {
                foreach (var t in snap.turrets)
                {
                    TurretData data = registry.Resolve(t.turretDataName);
                    if (data != null) builder.PlaceTurretFromRestore(data, t.position);
                }
            }

            // Let DelayedInitializeTurret (one-frame yield) finish before sabotages
            // that may iterate existing turrets.
            yield return null;
            yield return null;

            // ----- 4. Sabotages -----
            if (cardMgr != null)
            {
                foreach (var s in snap.sabotages)
                {
                    SabotageCardData data = ResolveSabotage(s.sabotageName);
                    if (data == null)
                    {
                        Debug.LogWarning($"[MatchRestore] Sabotage '{s.sabotageName}' not found.");
                        continue;
                    }
                    PhotonView caster = ResolvePlayerView(s.casterActorNumber);
                    cardMgr.RestoreSabotage(data, caster, s.remainingDuration, s.remainingRounds);
                }
            }

            // ----- 5. Self-sabotage challenges -----
            if (SelfSabotageTracker.Instance != null)
            {
                foreach (var ch in snap.selfChallenges)
                {
                    SabotageCardData data = ResolveSabotage(ch.sabotageName);
                    if (data != null)
                        SelfSabotageTracker.Instance.RestoreChallenge(data, ch.wavesRemaining, ch.totalWaves);
                }
            }

            // ----- 6. Draft / sabotage-draft phase flags -----
            if (DraftManager.Instance != null)
                DraftManager.Instance.RestoreDraftState(snap.draft);
            if (SabotageDraftManager.Instance != null)
                SabotageDraftManager.Instance.RestoreFrom(snap.draft);

            // ----- 7. Resume wave flow -----
            WaveManager wm = FindLocalWaveManager();
            if (wm != null)
            {
                RestorePending = false; // allow the resumed flow to start
                wm.StartWavesFromIndex(snap.currentWaveIndex);
            }
            else
            {
                RestorePending = false;
                Debug.LogError("[MatchRestore] No local WaveManager — cannot resume waves!");
            }

            Debug.Log("[MatchRestore] Restore complete.");
        }

        // ==========================================
        // CONTEXT HELPERS
        // ==========================================

        private static PlayerCardManager FindLocalCardManager()
        {
            if (PlayerGold.LocalInstance != null)
            {
                var cm = PlayerGold.LocalInstance.GetComponent<PlayerCardManager>();
                if (cm != null) return cm;
            }
            foreach (var cm in Object.FindObjectsByType<PlayerCardManager>(FindObjectsSortMode.None))
            {
                var pv = cm.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine) return cm;
            }
            return null;
        }

        private static PlayerBuilder FindLocalBuilder()
        {
            if (PlayerGold.LocalInstance != null)
            {
                var b = PlayerGold.LocalInstance.GetComponent<PlayerBuilder>();
                if (b != null) return b;
            }
            foreach (var b in Object.FindObjectsByType<PlayerBuilder>(FindObjectsSortMode.None))
            {
                var pv = b.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine) return b;
            }
            return null;
        }

        private static WaveManager FindLocalWaveManager()
        {
            foreach (var arena in Object.FindObjectsByType<ArenaOwner>(FindObjectsSortMode.None))
            {
                if (arena.ownerPhotonView != null && arena.ownerPhotonView.IsMine)
                    return arena.GetComponentInChildren<WaveManager>();
            }
            return null;
        }

        /// <summary>Find the player object owned by a given actor number (caster).</summary>
        private static PhotonView ResolvePlayerView(int actorNumber)
        {
            if (actorNumber < 0) return null;
            foreach (var pv in Object.FindObjectsByType<PhotonView>(FindObjectsSortMode.None))
            {
                if (pv.Owner != null && pv.Owner.ActorNumber == actorNumber &&
                    pv.GetComponentInChildren<PlayerHealth>() != null)
                    return pv;
            }
            return null;
        }

        // ==========================================
        // RESOURCE LOOKUPS (recursive, handles subfolders)
        // ==========================================

        private static System.Collections.Generic.Dictionary<string, CardData> s_cardLookup;
        private static System.Collections.Generic.Dictionary<string, SabotageCardData> s_sabotageLookup;

        private static CardData ResolveCard(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (s_cardLookup == null)
            {
                s_cardLookup = new System.Collections.Generic.Dictionary<string, CardData>();
                // LoadAll is recursive within the given Resources path.
                foreach (var c in Resources.LoadAll<CardData>("Cards"))
                    if (c != null) s_cardLookup[c.name] = c;
            }
            return s_cardLookup.TryGetValue(name, out var cd) ? cd : null;
        }

        private static SabotageCardData ResolveSabotage(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (s_sabotageLookup == null)
            {
                s_sabotageLookup = new System.Collections.Generic.Dictionary<string, SabotageCardData>();
                foreach (var s in Resources.LoadAll<SabotageCardData>("Cards"))
                    if (s != null) s_sabotageLookup[s.name] = s;
            }
            return s_sabotageLookup.TryGetValue(name, out var sd) ? sd : null;
        }
    }
}
