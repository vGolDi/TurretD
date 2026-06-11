using UnityEngine;
using ElementumDefense.Cards;

namespace ElementumDefense.Multiplayer
{
    /// <summary>
    /// Persisted "I am still in a match" marker — saved at match start, cleared
    /// on a clean game-end. If still set when the player returns to the menu,
    /// we know the player abandoned (or got disconnected from) a live match
    /// and should be offered a reconnect prompt.
    /// 
    /// <para>
    /// Per-account namespace: every key is suffixed with the active PlayFab ID,
    /// so two players sharing a PC each see only their own pending match. Call
    /// <see cref="UseAccount"/> right after login (or just before reading) to
    /// switch the active namespace. If no account is bound, all reads/writes
    /// no-op — that prevents a fresh launch from showing a popup before login.
    /// </para>
    /// 
    /// We keep this in PlayerPrefs because:
    ///  - Reconnect window is short (90s) so cloud round-trip latency is too risky.
    ///  - Even if PlayFab is down we still want the local user to see the prompt.
    ///  - PlayerPrefs survives editor / build / app crash equally well.
    /// </summary>
    public static class PendingMatchState
    {
        // PlayerPrefs key prefix — final key is "ed_pending_room::{playFabId}".
        private const string KEY_ROOM = "ed_pending_room";
        private const string KEY_MODE = "ed_pending_mode";          // "Casual" / "Ranked" / "Custom"
        private const string KEY_STARTED = "ed_pending_started";    // Unix timestamp (utc seconds)
        private const string KEY_TTL = "ed_pending_ttl_ms";         // PlayerTtl set on the room (ms)

        private static string s_account = null;

        /// <summary>True when an account is bound. Otherwise the reads return
        /// "no pending" and writes are silently dropped.</summary>
        public static bool HasAccount => !string.IsNullOrEmpty(s_account);

        /// <summary>Switch the active per-account namespace. Pass <c>null</c> on logout.</summary>
        public static void UseAccount(string playFabId)
        {
            s_account = string.IsNullOrEmpty(playFabId) ? null : playFabId;
        }

        // -------- key helper --------

        private static string K(string baseKey)
            => string.IsNullOrEmpty(s_account) ? null : $"{baseKey}::{s_account}";

        // -------- public state --------

        public static bool HasPending
            => HasAccount && !string.IsNullOrEmpty(PlayerPrefs.GetString(K(KEY_ROOM), ""));

        public static string RoomName
            => HasAccount ? PlayerPrefs.GetString(K(KEY_ROOM), "") : "";

        public static string ModeString
            => HasAccount ? PlayerPrefs.GetString(K(KEY_MODE), "Casual") : "Casual";

        public static GameMode Mode
        {
            get
            {
                if (!HasAccount) return GameMode.Casual;
                switch (PlayerPrefs.GetString(K(KEY_MODE), "Casual"))
                {
                    case "Ranked": return GameMode.Ranked;
                    case "Custom": return GameMode.Custom;
                    default: return GameMode.Casual;
                }
            }
        }

        /// <summary>UTC unix seconds when the match was recorded.</summary>
        public static long StartedAtUtc
            => HasAccount && long.TryParse(PlayerPrefs.GetString(K(KEY_STARTED), "0"), out var t) ? t : 0L;

        /// <summary>Photon PlayerTtl that was set on the room, in milliseconds.</summary>
        public static int PlayerTtlMs
            => HasAccount ? PlayerPrefs.GetInt(K(KEY_TTL), 0) : 0;

        /// <summary>How many seconds remain inside the reconnect window. Negative if expired.</summary>
        public static int SecondsRemaining
        {
            get
            {
                if (!HasAccount) return 0;
                long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long deadline = StartedAtUtc + (PlayerTtlMs / 1000);
                return (int)(deadline - now);
            }
        }

        /// <summary>True when the reconnect window has not expired yet.</summary>
        public static bool IsWithinReconnectWindow => HasPending && SecondsRemaining > 0;

        /// <summary>Records the start of a match. Called by NetworkManager after the room is fully joined.</summary>
        public static void Set(string roomName, GameMode mode, int playerTtlMs)
        {
            if (!HasAccount)
            {
                Debug.LogWarning("[PendingMatch] Set ignored — no account bound. Did login finish?");
                return;
            }
            PlayerPrefs.SetString(K(KEY_ROOM), roomName ?? "");
            PlayerPrefs.SetString(K(KEY_MODE), mode.ToString());
            PlayerPrefs.SetString(K(KEY_STARTED), System.DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            PlayerPrefs.SetInt(K(KEY_TTL), playerTtlMs);
            PlayerPrefs.Save();
            Debug.Log($"[PendingMatch] Recorded: account={s_account}, room='{roomName}', mode={mode}, ttl={playerTtlMs}ms");
        }

        /// <summary>
        /// Re-stamps the reconnect window to start NOW. Call this at the moment
        /// the player leaves the match (e.g. Pause Menu → Main Menu), so the
        /// reconnect window is measured from the leave time rather than from
        /// match start. Without this, a long match (> TTL) would expire the
        /// window the instant the player reaches the menu.
        /// </summary>
        public static void RefreshWindow()
        {
            if (!HasAccount || !HasPending) return;
            PlayerPrefs.SetString(K(KEY_STARTED),
                System.DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            PlayerPrefs.Save();
            Debug.Log("[PendingMatch] Reconnect window refreshed (starts now).");
        }

        /// <summary>Clears the marker — called on clean game end (Win / Loss / Forfeit handled).</summary>
        public static void Clear()
        {
            if (!HasAccount) return;
            if (!HasPending) return;
            Debug.Log($"[PendingMatch] Cleared (was room='{RoomName}', account={s_account})");
            PlayerPrefs.DeleteKey(K(KEY_ROOM));
            PlayerPrefs.DeleteKey(K(KEY_MODE));
            PlayerPrefs.DeleteKey(K(KEY_STARTED));
            PlayerPrefs.DeleteKey(K(KEY_TTL));
            PlayerPrefs.Save();
        }
    }
}
