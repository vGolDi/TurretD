using UnityEngine;
using ElementumDefense.Cards;

namespace ElementumDefense.Multiplayer
{
    /// <summary>
    /// Local matchmaking ban after forfeit / abandon.
    /// 
    /// Ban scope:
    ///  - Casual forfeit  -> 60s
    ///  - Ranked forfeit  -> 300s (5 min)
    /// 
    /// Per-account namespace: stored under "ed_match_ban_until::{playFabId}".
    /// Call <see cref="UseAccount"/> once the user is logged in. Without an
    /// account bound, <see cref="IsBanned"/> always returns false — preventing
    /// false positives at app launch before login completes.
    /// 
    /// Persisted to PlayerPrefs so it survives app restart. Cloud sync (PlayFab
    /// UserData) is intentionally optional — local enforcement is enough for
    /// the popup behavior; server-side enforcement can be added later.
    /// </summary>
    public static class MatchmakingBan
    {
        private const string KEY_UNTIL = "ed_match_ban_until";
        private const string KEY_REASON = "ed_match_ban_reason";

        public const int CASUAL_BAN_SECONDS = 60;
        public const int RANKED_BAN_SECONDS = 300;

        private static string s_account = null;

        public static bool HasAccount => !string.IsNullOrEmpty(s_account);

        public static void UseAccount(string playFabId)
        {
            s_account = string.IsNullOrEmpty(playFabId) ? null : playFabId;
        }

        private static string K(string baseKey)
            => string.IsNullOrEmpty(s_account) ? null : $"{baseKey}::{s_account}";

        private static string K(string baseKey, string mode)
        {
            if (string.IsNullOrEmpty(s_account)) return null;
            if (string.IsNullOrEmpty(mode)) return $"{baseKey}::{s_account}";
            return $"{baseKey}_{mode.ToLower()}::{s_account}";
        }

        /// <summary>UTC unix seconds when the ban expires (0 if none / no account).</summary>
        public static long BanUntilUtc
            => HasAccount && long.TryParse(PlayerPrefs.GetString(K(KEY_UNTIL), "0"), out var t) ? t : 0L;

        public static string Reason
            => HasAccount ? PlayerPrefs.GetString(K(KEY_REASON), "") : "";

        public static bool IsBanned
        {
            get
            {
                if (!HasAccount) return false;
                long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return BanUntilUtc > now || IsBannedForMode(GameMode.Casual) || IsBannedForMode(GameMode.Ranked);
            }
        }

        public static int SecondsRemaining
        {
            get
            {
                if (!HasAccount) return 0;
                long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long maxUntil = BanUntilUtc;
                long casualUntil = GetBanUntilUtcForMode(GameMode.Casual);
                long rankedUntil = GetBanUntilUtcForMode(GameMode.Ranked);
                long finalUntil = System.Math.Max(maxUntil, System.Math.Max(casualUntil, rankedUntil));
                return Mathf.Max(0, (int)(finalUntil - now));
            }
        }

        // -------- MODE SPECIFIC METHODS --------

        public static long GetBanUntilUtcForMode(GameMode mode)
        {
            if (!HasAccount) return 0L;
            string key = K(KEY_UNTIL, mode.ToString());
            return long.TryParse(PlayerPrefs.GetString(key, "0"), out var t) ? t : 0L;
        }

        public static string GetReasonForMode(GameMode mode)
        {
            if (!HasAccount) return "";
            string key = K(KEY_REASON, mode.ToString());
            return PlayerPrefs.GetString(key, "");
        }

        public static bool IsBannedForMode(GameMode mode)
        {
            if (!HasAccount) return false;
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return GetBanUntilUtcForMode(mode) > now;
        }

        public static int SecondsRemainingForMode(GameMode mode)
        {
            if (!HasAccount) return 0;
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Mathf.Max(0, (int)(GetBanUntilUtcForMode(mode) - now));
        }

        public static string FormatRemainingForMode(GameMode mode)
        {
            int s = SecondsRemainingForMode(mode);
            return $"{s / 60}:{s % 60:00}";
        }

        public static void ApplyForMode(GameMode mode)
        {
            if (!HasAccount)
            {
                Debug.LogWarning("[MatchmakingBan] Apply ignored — no account bound.");
                return;
            }
            int seconds = mode == GameMode.Ranked ? RANKED_BAN_SECONDS : CASUAL_BAN_SECONDS;
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long newUntil = now + seconds;

            long existingUntil = GetBanUntilUtcForMode(mode);
            if (newUntil < existingUntil)
            {
                Debug.Log($"[MatchmakingBan] Skipped {mode} ({seconds}s) — existing ban is longer.");
                return;
            }

            string untilKey = K(KEY_UNTIL, mode.ToString());
            string reasonKey = K(KEY_REASON, mode.ToString());

            PlayerPrefs.SetString(untilKey, newUntil.ToString());
            PlayerPrefs.SetString(reasonKey, mode.ToString());
            PlayerPrefs.Save();
            Debug.Log($"[MatchmakingBan] Applied {seconds}s ban for {mode} on account={s_account}.");
        }

        public static void ApplySeconds(int seconds, string reason)
        {
            if (!HasAccount)
            {
                Debug.LogWarning("[MatchmakingBan] Apply ignored — no account bound.");
                return;
            }
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long newUntil = now + seconds;

            // Extend rather than shorten if there's already a longer ban.
            if (newUntil < BanUntilUtc)
            {
                Debug.Log($"[MatchmakingBan] Skipped ({seconds}s) — existing ban is longer.");
                return;
            }

            PlayerPrefs.SetString(K(KEY_UNTIL), newUntil.ToString());
            PlayerPrefs.SetString(K(KEY_REASON), reason ?? "");
            PlayerPrefs.Save();
            Debug.Log($"[MatchmakingBan] Applied {seconds}s ({reason}) for account={s_account}.");
        }

        public static void Clear()
        {
            if (!HasAccount) return;
            PlayerPrefs.DeleteKey(K(KEY_UNTIL));
            PlayerPrefs.DeleteKey(K(KEY_REASON));
            PlayerPrefs.DeleteKey(K(KEY_UNTIL, GameMode.Casual.ToString()));
            PlayerPrefs.DeleteKey(K(KEY_REASON, GameMode.Casual.ToString()));
            PlayerPrefs.DeleteKey(K(KEY_UNTIL, GameMode.Ranked.ToString()));
            PlayerPrefs.DeleteKey(K(KEY_REASON, GameMode.Ranked.ToString()));
            PlayerPrefs.Save();
        }

        /// <summary>Formats remaining time as M:SS for UI labels.</summary>
        public static string FormatRemaining()
        {
            int s = SecondsRemaining;
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
