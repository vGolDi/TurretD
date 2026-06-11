using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ElementumDefense.Players;
using ElementumDefense.Cards;
using ElementumDefense.Turrets;
using ElementumDefense.Waves;

namespace ElementumDefense.Multiplayer.Reconnect
{
    /// <summary>
    /// Captures the local player's in-match state into a <see cref="PlayerMatchSnapshot"/>,
    /// encrypts + signs it (Layer 1), stores it in PlayerPrefs (per PlayFab account),
    /// and publishes a server-witnessed integrity hash to a Photon Player Custom
    /// Property (Layer 2). On reconnect the restore flow loads and verifies it.
    ///
    /// Lazily creates itself so callers can use the null-safe
    /// <c>MatchSnapshotService.Instance?.CaptureAndSave()</c> pattern from save points.
    /// </summary>
    public class MatchSnapshotService : MonoBehaviour
    {
        public const string SNAPSHOT_HASH_PROP = "snap_hash";
        private const string KEY_BLOB = "ed_match_snapshot";

        private static MatchSnapshotService s_instance;
        public static MatchSnapshotService Instance
        {
            get
            {
                if (s_instance == null)
                {
                    var go = new GameObject("[MatchSnapshotService]");
                    s_instance = go.AddComponent<MatchSnapshotService>();
                }
                return s_instance;
            }
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this) { Destroy(this); return; }
            s_instance = this;
        }

        private void OnDestroy()
        {
            if (s_instance == this) s_instance = null;
        }

        // ==========================================
        // KEY SCOPING (mirror PendingMatchState per-account namespace)
        // ==========================================

        private static string AccountId =>
            ElementumDefense.Auth.AuthManager.Instance != null
                ? ElementumDefense.Auth.AuthManager.Instance.PlayFabId
                : null;

        private static string BlobKey =>
            string.IsNullOrEmpty(AccountId) ? null : $"{KEY_BLOB}::{AccountId}";

        // ==========================================
        // CAPTURE + SAVE
        // ==========================================

        public void CaptureAndSave(string reason = "")
        {
            // Only the local player snapshots their own arena.
            if (!PhotonNetwork.InRoom) return;

            string key = BlobKey;
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[MatchSnapshot] No PlayFab account bound — save skipped.");
                return;
            }

            PlayerMatchSnapshot snap = Capture();
            if (snap == null) return;

            string json = JsonUtility.ToJson(snap);
            string blob = SnapshotCrypto.Encrypt(json);

            PlayerPrefs.SetString(key, blob);
            PlayerPrefs.Save();

            // Layer 2: server-witnessed hash.
            string hash = SnapshotCrypto.Hash(json);
            var props = new ExitGames.Client.Photon.Hashtable { { SNAPSHOT_HASH_PROP, hash } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            string tag = string.IsNullOrEmpty(reason) ? "" : $"[{reason}] ";
            Debug.Log($"[MatchSnapshot] Saved {tag}(wave={snap.currentWaveIndex}, gold={snap.currentGold}, " +
                      $"hp={snap.playerHP}, turrets={snap.turrets.Count}, cards={snap.activeCardNames.Count}, " +
                      $"sabotages={snap.sabotages.Count}, selfChallenges={snap.selfChallenges.Count}).");
        }

        private PlayerMatchSnapshot Capture()
        {
            var snap = new PlayerMatchSnapshot();

            snap.roomName = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "";

            WaveManager wm = FindLocalWaveManager();
            snap.currentWaveIndex = wm != null ? wm.GetCurrentWaveIndex() : 0;

            if (PlayerGold.LocalInstance != null)
                snap.currentGold = PlayerGold.LocalInstance.GetGold();

            if (PlayerHealth.LocalInstance != null)
                snap.playerHP = PlayerHealth.LocalInstance.CurrentHealth;

            // Turrets owned by the local player.
            Turret[] allTurrets = Object.FindObjectsByType<Turret>(FindObjectsSortMode.None);
            foreach (var t in allTurrets)
            {
                if (t == null || t.TurretData == null) continue;
                PhotonView owner = t.GetOwner();
                if (owner == null || !owner.IsMine) continue;

                snap.turrets.Add(new TurretSnapshot
                {
                    turretDataName = t.TurretData.name,
                    position = t.transform.position
                });
            }

            // Cards + sabotages via the local PlayerCardManager.
            PlayerCardManager cardMgr = FindLocalCardManager();
            if (cardMgr != null)
            {
                foreach (var c in cardMgr.ActiveCards)
                    if (c != null) snap.activeCardNames.Add(c.name);

                foreach (var s in cardMgr.GetActiveSabotages())
                {
                    if (s == null || s.sabotageData == null) continue;
                    snap.sabotages.Add(new ActiveSabotageSnapshot
                    {
                        sabotageName = s.sabotageData.name,
                        casterActorNumber = s.casterPhotonView != null && s.casterPhotonView.Owner != null
                            ? s.casterPhotonView.Owner.ActorNumber : -1,
                        remainingDuration = s.remainingDuration,
                        remainingRounds = s.remainingRounds
                    });
                }
            }

            // Self-sabotage challenges.
            if (SelfSabotageTracker.Instance != null)
            {
                foreach (var ch in SelfSabotageTracker.Instance.GetActiveChallenges())
                {
                    if (ch == null || ch.sabotageData == null) continue;
                    snap.selfChallenges.Add(new SelfChallengeSnapshot
                    {
                        sabotageName = ch.sabotageData.name,
                        wavesRemaining = ch.wavesRemaining,
                        totalWaves = ch.totalWaves
                    });
                }
            }

            // Draft + sabotage-draft phase state.
            if (DraftManager.Instance != null)
                DraftManager.Instance.CaptureDraftState(snap.draft);
            if (SabotageDraftManager.Instance != null)
                SabotageDraftManager.Instance.CaptureInto(snap.draft);

            return snap;
        }

        // ==========================================
        // LOAD + VERIFY
        // ==========================================

        public bool TryLoad(out PlayerMatchSnapshot snap)
        {
            snap = null;
            string key = BlobKey;
            if (string.IsNullOrEmpty(key)) return false;

            string blob = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(blob)) return false;

            if (!SnapshotCrypto.TryDecrypt(blob, out string json))
            {
                Debug.LogWarning("[MatchSnapshot] Decrypt/verify failed — snapshot rejected (possible tampering).");
                return false;
            }

            try { snap = JsonUtility.FromJson<PlayerMatchSnapshot>(json); }
            catch { snap = null; }

            if (snap == null) return false;
            if (snap.version != PlayerMatchSnapshot.CURRENT_VERSION)
            {
                Debug.LogWarning($"[MatchSnapshot] Version mismatch ({snap.version}) — snapshot ignored.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Layer 2 verification: re-serialize the loaded snapshot, hash it, and
        /// compare to the server-witnessed hash in the local player's Custom
        /// Properties. Mismatch => the offline file was edited => reject.
        /// </summary>
        public bool VerifyServerHash(PlayerMatchSnapshot snap)
        {
            if (snap == null) return false;
            if (PhotonNetwork.LocalPlayer == null) return false;

            if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(SNAPSHOT_HASH_PROP, out object stored)
                || stored is not string serverHash || string.IsNullOrEmpty(serverHash))
            {
                // No server hash recorded (e.g. disconnect before first save) — cannot verify.
                Debug.LogWarning("[MatchSnapshot] No server hash to verify against.");
                return false;
            }

            string localHash = SnapshotCrypto.Hash(JsonUtility.ToJson(snap));
            bool ok = localHash == serverHash;
            if (!ok)
                Debug.LogWarning("[MatchSnapshot] Hash mismatch — snapshot tampered.");
            return ok;
        }

        public void Clear()
        {
            string key = BlobKey;
            if (!string.IsNullOrEmpty(key) && PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }

            // Also clear the server-witnessed hash so it can't carry over to the
            // next room (Photon keeps player custom properties across rooms).
            // Guard against the "client is Leaving / not ready" state — setting
            // properties then throws; the stale hash is harmless anyway because
            // the room-name guard rejects mismatched snapshots on restore.
            if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom &&
                PhotonNetwork.LocalPlayer != null &&
                PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(SNAPSHOT_HASH_PROP))
            {
                var props = new ExitGames.Client.Photon.Hashtable { { SNAPSHOT_HASH_PROP, null } };
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            }
        }

        // ==========================================
        // LOCAL CONTEXT HELPERS
        // ==========================================

        private WaveManager FindLocalWaveManager()
        {
            ArenaOwner[] arenas = Object.FindObjectsByType<ArenaOwner>(FindObjectsSortMode.None);
            foreach (var arena in arenas)
            {
                if (arena.ownerPhotonView != null && arena.ownerPhotonView.IsMine)
                    return arena.GetComponentInChildren<WaveManager>();
            }
            return null;
        }

        private PlayerCardManager FindLocalCardManager()
        {
            if (PlayerGold.LocalInstance != null)
            {
                var cm = PlayerGold.LocalInstance.GetComponent<PlayerCardManager>();
                if (cm != null) return cm;
            }
            PlayerCardManager[] all = Object.FindObjectsByType<PlayerCardManager>(FindObjectsSortMode.None);
            foreach (var cm in all)
            {
                var pv = cm.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine) return cm;
            }
            return null;
        }
    }
}
